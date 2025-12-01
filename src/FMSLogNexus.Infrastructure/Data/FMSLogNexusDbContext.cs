using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Infrastructure.Data;

/// <summary>
/// Main database context for FMS Log Nexus.
/// </summary>
public class FMSLogNexusDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the database context.
    /// </summary>
    /// <param name="options">Database context options.</param>
    public FMSLogNexusDbContext(DbContextOptions<FMSLogNexusDbContext> options)
        : base(options)
    {
    }

    #region DbSets - Log Schema

    /// <summary>
    /// Log entries.
    /// </summary>
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();

    #endregion

    #region DbSets - Job Schema

    /// <summary>
    /// Jobs.
    /// </summary>
    public DbSet<Job> Jobs => Set<Job>();

    /// <summary>
    /// Job executions.
    /// </summary>
    public DbSet<JobExecution> JobExecutions => Set<JobExecution>();

    /// <summary>
    /// Servers/machines.
    /// </summary>
    public DbSet<Server> Servers => Set<Server>();

    #endregion

    #region DbSets - Alert Schema

    /// <summary>
    /// Alert definitions.
    /// </summary>
    public DbSet<Alert> Alerts => Set<Alert>();

    /// <summary>
    /// Alert instances.
    /// </summary>
    public DbSet<AlertInstance> AlertInstances => Set<AlertInstance>();

    #endregion

    #region DbSets - Auth Schema

    /// <summary>
    /// Users.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// User API keys.
    /// </summary>
    public DbSet<UserApiKey> UserApiKeys => Set<UserApiKey>();

    /// <summary>
    /// Refresh tokens.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    #endregion

    #region DbSets - System Schema

    /// <summary>
    /// System configuration.
    /// </summary>
    public DbSet<SystemConfiguration> SystemConfiguration => Set<SystemConfiguration>();

    /// <summary>
    /// Audit log entries.
    /// </summary>
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    /// <summary>
    /// Dashboard cache entries.
    /// </summary>
    public DbSet<DashboardCacheEntry> DashboardCache => Set<DashboardCacheEntry>();

    /// <summary>
    /// Schema versions.
    /// </summary>
    public DbSet<SchemaVersion> SchemaVersions => Set<SchemaVersion>();

    #endregion

    #region Configuration

    /// <summary>
    /// Configures the model using Fluent API.
    /// </summary>
    /// <param name="modelBuilder">Model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FMSLogNexusDbContext).Assembly);

        // Configure default schema
        modelBuilder.HasDefaultSchema("dbo");

        // Configure enum conversions
        ConfigureEnumConversions(modelBuilder);

        // Apply global query filters
        ApplyGlobalQueryFilters(modelBuilder);
    }

    /// <summary>
    /// Configures enum to string conversions.
    /// </summary>
    private static void ConfigureEnumConversions(ModelBuilder modelBuilder)
    {
        // LogLevel is stored as int (matches SQL Server)
        modelBuilder.Entity<LogEntry>()
            .Property(e => e.Level)
            .HasConversion<int>();

        // JobStatus stored as int
        modelBuilder.Entity<JobExecution>()
            .Property(e => e.Status)
            .HasConversion<int>();

        // JobType stored as int
        modelBuilder.Entity<Job>()
            .Property(e => e.JobType)
            .HasConversion<int>();

        // ServerStatus stored as int
        modelBuilder.Entity<Server>()
            .Property(e => e.Status)
            .HasConversion<int>();

        // AlertSeverity stored as int
        modelBuilder.Entity<Alert>()
            .Property(e => e.Severity)
            .HasConversion<int>();

        modelBuilder.Entity<AlertInstance>()
            .Property(e => e.Severity)
            .HasConversion<int>();

        // AlertType stored as int
        modelBuilder.Entity<Alert>()
            .Property(e => e.AlertType)
            .HasConversion<int>();

        // AlertInstanceStatus stored as int
        modelBuilder.Entity<AlertInstance>()
            .Property(e => e.Status)
            .HasConversion<int>();

        // UserRole stored as int
        modelBuilder.Entity<User>()
            .Property(e => e.Role)
            .HasConversion<int>();

        // Job LastStatus stored as int (nullable)
        modelBuilder.Entity<Job>()
            .Property(e => e.LastStatus)
            .HasConversion<int?>();
    }

    /// <summary>
    /// Applies global query filters (e.g., soft delete).
    /// </summary>
    private static void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // Note: Soft delete filters would be applied here if needed
        // For now, we handle this in repositories for more control
    }

    #endregion

    #region SaveChanges Overrides

    /// <summary>
    /// Saves changes with automatic audit field population.
    /// </summary>
    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    /// <summary>
    /// Saves changes with automatic audit field population.
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// Saves changes asynchronously with automatic audit field population.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Saves changes asynchronously with automatic audit field population.
    /// </summary>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Updates audit fields (CreatedAt, UpdatedAt) on tracked entities.
    /// </summary>
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            // Handle creation auditing
            if (entry.Entity is ICreationAuditable creationAuditable && entry.State == EntityState.Added)
            {
                creationAuditable.CreatedAt = now;
            }

            // Handle modification auditing
            if (entry.Entity is IModificationAuditable modificationAuditable)
            {
                modificationAuditable.UpdatedAt = now;

                // Don't overwrite CreatedAt on updates
                if (entry.State == EntityState.Modified)
                {
                    var createdAtProp = entry.Property(nameof(ICreationAuditable.CreatedAt));
                    var createdByProp = entry.Property(nameof(ICreationAuditable.CreatedBy));
                    
                    if (createdAtProp != null)
                        createdAtProp.IsModified = false;
                    if (createdByProp != null)
                        createdByProp.IsModified = false;
                }
            }
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Saves changes with audit information.
    /// </summary>
    /// <param name="userId">User performing the operation.</param>
    /// <param name="username">Username performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of entities saved.</returns>
    public async Task<int> SaveChangesWithAuditAsync(
        Guid? userId,
        string? username,
        CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is ICreationAuditable creationAuditable && entry.State == EntityState.Added)
            {
                creationAuditable.CreatedBy = username;
            }

            if (entry.Entity is IModificationAuditable modificationAuditable && entry.State == EntityState.Modified)
            {
                modificationAuditable.UpdatedBy = username;
            }
        }

        return await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Detaches all tracked entities.
    /// </summary>
    public void DetachAllEntities()
    {
        var entries = ChangeTracker.Entries().ToList();
        foreach (var entry in entries)
        {
            entry.State = EntityState.Detached;
        }
    }

    #endregion
}
