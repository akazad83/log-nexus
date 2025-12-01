namespace FMSLogNexus.Core.Entities;

/// <summary>
/// Represents a system configuration key-value pair.
/// Maps to [system].[Configuration] table.
/// </summary>
public class SystemConfiguration : IEntity<string>
{
    /// <summary>
    /// Configuration key (primary key).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Alias for Id to maintain naming consistency.
    /// </summary>
    public string Key
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>
    /// Configuration value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Data type (string, int, bool, json, datetime).
    /// </summary>
    public string DataType { get; set; } = "string";

    /// <summary>
    /// Category for grouping configurations.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Description of the configuration.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the value is encrypted.
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// UTC timestamp when last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Username who last updated.
    /// </summary>
    public string? UpdatedBy { get; set; }

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets the value as an integer.
    /// </summary>
    public int GetIntValue(int defaultValue = 0)
    {
        if (string.IsNullOrEmpty(Value))
            return defaultValue;

        return int.TryParse(Value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets the value as a boolean.
    /// </summary>
    public bool GetBoolValue(bool defaultValue = false)
    {
        if (string.IsNullOrEmpty(Value))
            return defaultValue;

        return bool.TryParse(Value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets the value as a DateTime.
    /// </summary>
    public DateTime? GetDateTimeValue()
    {
        if (string.IsNullOrEmpty(Value))
            return null;

        return DateTime.TryParse(Value, out var result) ? result : null;
    }

    /// <summary>
    /// Gets the value as a TimeSpan.
    /// </summary>
    public TimeSpan? GetTimeSpanValue()
    {
        if (string.IsNullOrEmpty(Value))
            return null;

        return TimeSpan.TryParse(Value, out var result) ? result : null;
    }

    /// <summary>
    /// Gets the value as a typed object from JSON.
    /// </summary>
    public T? GetJsonValue<T>() where T : class
    {
        if (string.IsNullOrEmpty(Value))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(Value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets the value from an object.
    /// </summary>
    public void SetValue(object value, string? updatedBy = null)
    {
        if (value is string strValue)
        {
            Value = strValue;
            DataType = "string";
        }
        else if (value is int or long)
        {
            Value = value.ToString();
            DataType = "int";
        }
        else if (value is bool boolValue)
        {
            Value = boolValue.ToString().ToLowerInvariant();
            DataType = "bool";
        }
        else if (value is DateTime dateValue)
        {
            Value = dateValue.ToString("O");
            DataType = "datetime";
        }
        else
        {
            Value = System.Text.Json.JsonSerializer.Serialize(value);
            DataType = "json";
        }

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}

/// <summary>
/// Represents an audit log entry.
/// Maps to [system].[AuditLog] table.
/// </summary>
public class AuditLogEntry : IEntity<long>
{
    /// <summary>
    /// Auto-incrementing ID.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// UTC timestamp of the action.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who performed the action (null for system actions).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Username who performed the action.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Action performed (Create, Update, Delete, Login, etc.).
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity affected.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity affected.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Previous values as JSON (for updates).
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// New values as JSON (for creates and updates).
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// IP address of the client.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client.
    /// </summary>
    public string? UserAgent { get; set; }

    // -------------------------------------------------------------------------
    // Navigation Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// The user who performed the action.
    /// </summary>
    public virtual User? User { get; set; }

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets old values as a dictionary.
    /// </summary>
    public Dictionary<string, object>? GetOldValuesAsDictionary()
    {
        if (string.IsNullOrEmpty(OldValues))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(OldValues);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets new values as a dictionary.
    /// </summary>
    public Dictionary<string, object>? GetNewValuesAsDictionary()
    {
        if (string.IsNullOrEmpty(NewValues))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(NewValues);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates an audit entry for a create action.
    /// </summary>
    public static AuditLogEntry ForCreate(
        string entityType,
        string entityId,
        object newValues,
        Guid? userId = null,
        string? username = null,
        string? ipAddress = null)
    {
        return new AuditLogEntry
        {
            Action = "Create",
            EntityType = entityType,
            EntityId = entityId,
            NewValues = System.Text.Json.JsonSerializer.Serialize(newValues),
            UserId = userId,
            Username = username,
            IpAddress = ipAddress
        };
    }

    /// <summary>
    /// Creates an audit entry for an update action.
    /// </summary>
    public static AuditLogEntry ForUpdate(
        string entityType,
        string entityId,
        object? oldValues,
        object newValues,
        Guid? userId = null,
        string? username = null,
        string? ipAddress = null)
    {
        return new AuditLogEntry
        {
            Action = "Update",
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues != null ? System.Text.Json.JsonSerializer.Serialize(oldValues) : null,
            NewValues = System.Text.Json.JsonSerializer.Serialize(newValues),
            UserId = userId,
            Username = username,
            IpAddress = ipAddress
        };
    }

    /// <summary>
    /// Creates an audit entry for a delete action.
    /// </summary>
    public static AuditLogEntry ForDelete(
        string entityType,
        string entityId,
        object? oldValues = null,
        Guid? userId = null,
        string? username = null,
        string? ipAddress = null)
    {
        return new AuditLogEntry
        {
            Action = "Delete",
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues != null ? System.Text.Json.JsonSerializer.Serialize(oldValues) : null,
            UserId = userId,
            Username = username,
            IpAddress = ipAddress
        };
    }
}

/// <summary>
/// Represents a cached dashboard value.
/// Maps to [system].[DashboardCache] table.
/// </summary>
public class DashboardCacheEntry : IEntity<string>
{
    /// <summary>
    /// Cache key (primary key).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Alias for Id to maintain naming consistency.
    /// </summary>
    public string Key
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>
    /// Cached value as JSON.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// UTC timestamp when the value was computed.
    /// </summary>
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when the cache expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    // -------------------------------------------------------------------------
    // Computed Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Indicates if the cache entry is expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Age of the cache entry in seconds.
    /// </summary>
    public int AgeInSeconds => (int)(DateTime.UtcNow - ComputedAt).TotalSeconds;

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets the cached value as a typed object.
    /// </summary>
    public T? GetValue<T>() where T : class
    {
        if (string.IsNullOrEmpty(Value))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(Value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets the cached value.
    /// </summary>
    public void SetValue<T>(T value, int ttlSeconds)
    {
        Value = System.Text.Json.JsonSerializer.Serialize(value);
        ComputedAt = DateTime.UtcNow;
        ExpiresAt = ComputedAt.AddSeconds(ttlSeconds);
    }
}

/// <summary>
/// Represents a schema version entry for migration tracking.
/// Maps to [system].[SchemaVersion] table.
/// </summary>
public class SchemaVersion : IEntity<int>
{
    /// <summary>
    /// Version number.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Alias for Id to maintain naming consistency.
    /// </summary>
    public int Version
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>
    /// Description of the migration.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// UTC timestamp when the migration was applied.
    /// </summary>
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Username who applied the migration.
    /// </summary>
    public string? AppliedBy { get; set; }

    /// <summary>
    /// Name of the script file.
    /// </summary>
    public string? Script { get; set; }
}
