namespace HostelManagement.Server.Models;

public class Owner
{
    public Guid Id { get; set; }
    public Guid OwnerOrgId { get; set; }
    public OwnerOrg? OwnerOrg { get; set; }

    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
