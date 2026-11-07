namespace HostelManagement.Server.Data;

public class HttpCurrentOwnerContext : ICurrentOwnerContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentOwnerContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? OwnerOrgId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst("OwnerOrgId")?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }
}
