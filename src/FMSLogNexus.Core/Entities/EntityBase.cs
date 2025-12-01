namespace FMSLogNexus.Core.Entities;

/// <summary>
/// Marker interface for all domain entities.
/// </summary>
public interface IEntity
{
}

/// <summary>
/// Interface for entities with a GUID primary key.
/// </summary>
public interface IEntity<TKey> : IEntity
{
    TKey Id { get; set; }
}

/// <summary>
/// Interface for entities that track creation metadata.
/// </summary>
public interface ICreationAuditable
{
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
}

/// <summary>
/// Interface for entities that track modification metadata.
/// </summary>
public interface IModificationAuditable : ICreationAuditable
{
    DateTime UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}

/// <summary>
/// Interface for entities that support soft delete.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}

/// <summary>
/// Interface for entities that can be activated/deactivated.
/// </summary>
public interface IActivatable
{
    bool IsActive { get; set; }
}

/// <summary>
/// Base class for entities with a GUID primary key.
/// </summary>
public abstract class EntityBase : IEntity<Guid>
{
    public Guid Id { get; set; }

    protected EntityBase()
    {
        Id = Guid.NewGuid();
    }

    protected EntityBase(Guid id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not EntityBase other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (Id == Guid.Empty || other.Id == Guid.Empty)
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return (GetType().ToString() + Id).GetHashCode();
    }

    public static bool operator ==(EntityBase? left, EntityBase? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(EntityBase? left, EntityBase? right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Base class for auditable entities with creation and modification tracking.
/// </summary>
public abstract class AuditableEntity : EntityBase, IModificationAuditable
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Base class for entities with a string primary key.
/// </summary>
public abstract class StringKeyEntity : IEntity<string>
{
    public string Id { get; set; } = string.Empty;

    public override bool Equals(object? obj)
    {
        if (obj is not StringKeyEntity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (string.IsNullOrEmpty(Id) || string.IsNullOrEmpty(other.Id))
            return false;

        return string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return (GetType().ToString() + Id.ToUpperInvariant()).GetHashCode();
    }
}
