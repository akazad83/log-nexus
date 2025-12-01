using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FMSLogNexus.Infrastructure.Data;

/// <summary>
/// Design-time factory for creating FMSLogNexusDbContext during migrations.
/// </summary>
public class FMSLogNexusDbContextFactory : IDesignTimeDbContextFactory<FMSLogNexusDbContext>
{
    /// <summary>
    /// Creates a new instance of the database context for design-time operations.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Configured database context.</returns>
    public FMSLogNexusDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Get connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=FMSLogNexus;Trusted_Connection=True;TrustServerCertificate=True;";

        // Create options builder
        var optionsBuilder = new DbContextOptionsBuilder<FMSLogNexusDbContext>();
        
        optionsBuilder.UseSqlServer(connectionString, options =>
        {
            options.MigrationsAssembly(typeof(FMSLogNexusDbContext).Assembly.FullName);
            options.CommandTimeout(300);
            options.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });

        return new FMSLogNexusDbContext(optionsBuilder.Options);
    }
}
