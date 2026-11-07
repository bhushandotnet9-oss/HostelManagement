using HostelManagement.Server.Data;

namespace HostelManagement.Server.Tests;

/// <summary>
/// Test-only ICurrentOwnerContext whose OwnerOrgId tests can set directly, for
/// tests that manipulate the database without an HTTP request. When left unset
/// (null), falls back to the real claim-based context, so HTTP-driven tests
/// that authenticate via the real login/setup endpoints exercise production
/// claim resolution rather than a stubbed value.
/// </summary>
public class MutableOwnerContext : ICurrentOwnerContext
{
    private readonly ICurrentOwnerContext _fallback;
    private Guid? _override;

    public MutableOwnerContext(ICurrentOwnerContext fallback)
    {
        _fallback = fallback;
    }

    public Guid? OwnerOrgId
    {
        get => _override ?? _fallback.OwnerOrgId;
        set => _override = value;
    }
}
