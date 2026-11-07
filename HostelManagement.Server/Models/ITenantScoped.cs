namespace HostelManagement.Server.Models;

public interface ITenantScoped
{
    Guid OwnerOrgId { get; set; }
}
