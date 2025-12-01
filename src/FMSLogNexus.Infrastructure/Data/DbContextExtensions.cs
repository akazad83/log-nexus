using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FMSLogNexus.Infrastructure.Data;

/// <summary>
/// Extension methods for registering database services.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Adds FMS Log Nexus database context to the service collection.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="enableSensitiveDataLogging">Enable sensitive data in logs (for development).</param>
    /// <param name="enableDetailedErrors">Enable detailed error messages.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddFMSLogNexusDbContext(
        this IServiceCollection services,
        string connectionString,
        bool enableSensitiveDataLogging = false,
        bool enableDetailedErrors = false)
    {
        services.AddDbContext<FMSLogNexusDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(FMSLogNexusDbContext).Assembly.FullName);
                sqlOptions.CommandTimeout(300);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                
                // Use split queries for complex joins
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            if (enableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }

            if (enableDetailedErrors)
            {
                options.EnableDetailedErrors();
            }
        });

        return services;
    }

    /// <summary>
    /// Adds FMS Log Nexus database context with a factory for pooling.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="poolSize">Connection pool size.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddFMSLogNexusDbContextPool(
        this IServiceCollection services,
        string connectionString,
        int poolSize = 1024)
    {
        services.AddDbContextPool<FMSLogNexusDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(FMSLogNexusDbContext).Assembly.FullName);
                sqlOptions.CommandTimeout(300);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        }, poolSize);

        return services;
    }

    /// <summary>
    /// Applies any pending migrations to the database.
    /// </summary>
    /// <param name="serviceProvider">Service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task MigrateDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FMSLogNexusDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>
    /// Ensures the database exists and applies migrations.
    /// </summary>
    /// <param name="serviceProvider">Service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task EnsureDatabaseCreatedAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FMSLogNexusDbContext>();
        
        // Ensure database exists
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }

    /// <summary>
    /// Tests database connectivity.
    /// </summary>
    /// <param name="serviceProvider">Service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection successful.</returns>
    public static async Task<bool> TestDatabaseConnectionAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FMSLogNexusDbContext>();
            return await context.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}
