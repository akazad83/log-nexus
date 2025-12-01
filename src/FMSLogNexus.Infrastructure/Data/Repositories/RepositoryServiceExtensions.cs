using Microsoft.Extensions.DependencyInjection;
using FMSLogNexus.Core.Interfaces.Repositories;

namespace FMSLogNexus.Infrastructure.Data.Repositories;

/// <summary>
/// Extension methods for registering repositories with dependency injection.
/// </summary>
public static class RepositoryServiceExtensions
{
    /// <summary>
    /// Adds all repository implementations to the service collection.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register individual repositories (optional - can use UoW instead)
        services.AddScoped<ILogRepository, LogRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IJobExecutionRepository, JobExecutionRepository>();
        services.AddScoped<IServerRepository, ServerRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IAlertInstanceRepository, AlertInstanceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ISystemConfigurationRepository, SystemConfigurationRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        return services;
    }

    /// <summary>
    /// Adds only the Unit of Work to the service collection.
    /// Use this when you prefer to access repositories through the UoW pattern only.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    /// <summary>
    /// Adds the complete data access layer including DbContext and repositories.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="enableSensitiveDataLogging">Enable sensitive data logging for development.</param>
    /// <param name="enableDetailedErrors">Enable detailed error messages.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddDataAccessLayer(
        this IServiceCollection services,
        string connectionString,
        bool enableSensitiveDataLogging = false,
        bool enableDetailedErrors = false)
    {
        // Add DbContext
        services.AddFMSLogNexusDbContext(connectionString, enableSensitiveDataLogging, enableDetailedErrors);

        // Add repositories
        services.AddRepositories();

        return services;
    }

    /// <summary>
    /// Adds the complete data access layer with connection pooling for high-performance scenarios.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="poolSize">Maximum pool size.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddDataAccessLayerWithPooling(
        this IServiceCollection services,
        string connectionString,
        int poolSize = 1024)
    {
        // Add pooled DbContext
        services.AddFMSLogNexusDbContextPool(connectionString, poolSize);

        // Add repositories
        services.AddRepositories();

        return services;
    }
}
