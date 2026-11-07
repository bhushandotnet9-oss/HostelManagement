namespace HostelManagement.Server.Data;

/// <summary>
/// Resolves the authenticated owner's OwnerOrgId for the current request so the
/// data layer can scope every query/write to it. Returning null means "no
/// authenticated owner" - the global query filter then matches no rows,
/// failing closed rather than open.
/// </summary>
public interface ICurrentOwnerContext
{
    Guid? OwnerOrgId { get; }
}
