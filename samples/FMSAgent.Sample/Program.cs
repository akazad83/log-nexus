using FMSLogNexus.Client;
using Serilog;
using FmsLogLevel = FMSLogNexus.Client.LogLevel;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting FMS Agent Sample...");

    var builder = Host.CreateApplicationBuilder(args);

    // Add Serilog
    builder.Services.AddSerilog();

    // Configure FMS Log Nexus Agent
    builder.Services.AddFMSLogNexusAgent(options =>
    {
        options.BaseUrl = builder.Configuration["FMSLogNexus:BaseUrl"] ?? "http://localhost:5000";
        options.ServerName = builder.Configuration["FMSLogNexus:ServerName"] ?? Environment.MachineName;
        options.ApiKey = builder.Configuration["FMSLogNexus:ApiKey"];
        options.AgentVersion = "1.0.0";
        options.HeartbeatIntervalSeconds = 30;
        options.LogFlushIntervalSeconds = 5;
        options.MaxBatchSize = 100;
        options.EnableAutoHeartbeat = true;
        options.EnableLogBuffering = true;
    });

    // Add the sample worker
    builder.Services.AddHostedService<SampleJobWorker>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Sample worker that demonstrates job execution and logging.
/// </summary>
public class SampleJobWorker : BackgroundService
{
    private readonly FMSLogNexusClient _client;
    private readonly ILogger<SampleJobWorker> _logger;
    private readonly Random _random = new();

    private const string JobId = "SAMPLE-JOB-001";

    public SampleJobWorker(FMSLogNexusClient client, ILogger<SampleJobWorker> logger)
    {
        _client = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sample job worker started");

        // Register the job
        await RegisterJobAsync(stoppingToken);

        // Wait for a few seconds before starting
        await Task.Delay(5000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job execution failed");
            }

            // Wait before next execution (simulate scheduled job)
            var delay = TimeSpan.FromSeconds(30 + _random.Next(30));
            _logger.LogInformation("Next job execution in {Delay}", delay);
            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task RegisterJobAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _client.Jobs.RegisterAsync(new FMSLogNexus.Client.Models.RegisterJobRequest
            {
                JobId = JobId,
                DisplayName = "Sample Data Processing Job",
                Description = "A sample job that demonstrates FMS Log Nexus integration",
                Priority = JobPriority.Normal,
                TimeoutMinutes = 5,
                Tags = new List<string> { "sample", "demo" }
            }, cancellationToken);

            _logger.LogInformation("Job registered: {JobId}", result?.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to register job (may already exist)");
        }
    }

    private async Task RunJobAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting job execution...");

        // Start execution
        var execution = await _client.Executions.StartAsync(
            JobId,
            TriggerType.Scheduled,
            "SampleWorker",
            cancellationToken: cancellationToken);

        if (execution == null)
        {
            _logger.LogError("Failed to start execution");
            return;
        }

        _logger.LogInformation("Execution started: {ExecutionId}", execution.Id);

        try
        {
            // Log job start
            _client.Logs.Enqueue(FmsLogLevel.Information, "Job execution started", JobId, execution.Id);

            // Simulate job work
            var steps = _random.Next(3, 7);
            for (int i = 1; i <= steps; i++)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Log progress
                _client.Logs.Enqueue(FmsLogLevel.Information, $"Processing step {i}/{steps}", JobId, execution.Id);
                _logger.LogDebug("Step {Step}/{Total}", i, steps);

                // Simulate work
                await Task.Delay(_random.Next(1000, 3000), cancellationToken);

                // Random warning
                if (_random.NextDouble() < 0.2)
                {
                    _client.Logs.Enqueue(FmsLogLevel.Warning, $"Step {i} took longer than expected", JobId, execution.Id);
                }
            }

            // Random failure simulation
            if (_random.NextDouble() < 0.1)
            {
                throw new InvalidOperationException("Simulated random failure");
            }

            // Complete with success
            _client.Logs.Enqueue(FmsLogLevel.Information, "Job completed successfully", JobId, execution.Id);
            
            await _client.Executions.CompleteSuccessAsync(
                execution.Id,
                $"Processed {steps} steps successfully",
                cancellationToken);

            _logger.LogInformation("Execution completed successfully");
        }
        catch (OperationCanceledException)
        {
            _client.Logs.Enqueue(FmsLogLevel.Warning, "Job execution cancelled", JobId, execution.Id);
            await _client.Executions.CancelAsync(execution.Id, "Application shutting down", cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            _client.Logs.Enqueue(FmsLogLevel.Error, $"Job failed: {ex.Message}", JobId, execution.Id,
                new Dictionary<string, object> { ["Exception"] = ex.ToString() });

            await _client.Executions.CompleteFailureAsync(
                execution.Id,
                ex.Message,
                cancellationToken);

            _logger.LogError(ex, "Execution failed");
        }
    }
}
