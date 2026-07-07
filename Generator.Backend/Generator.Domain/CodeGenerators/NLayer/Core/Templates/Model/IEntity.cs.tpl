namespace {{ core_project_name }}.Model;

/// <summary>
/// Base interface for all entities.
/// </summary>
public interface IEntity
{
}

/// <summary>
/// Interface for entities voiding to handle interceptors.
/// </summary>

public interface IProjectEntity
{
}

/// <summary>
/// Interface for entities that support soft deletion.
/// </summary>
public interface ISoftDeletableEntity
{
    string? DeletedBy { get; set; }
    bool IsDeleted { get; set; }
    DateTime? DeletedDateUtc { get; set; }
}

/// <summary>
/// Interface for entities that support auditing.
/// </summary>
public interface IAuditableEntity
{
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
    DateTime? CreateDateUtc { get; set; }
    DateTime? UpdateDateUtc { get; set; }
}

/// <summary>
/// Interface for entities that support archiving.
/// </summary>
public interface IArchivableEntity
{
}