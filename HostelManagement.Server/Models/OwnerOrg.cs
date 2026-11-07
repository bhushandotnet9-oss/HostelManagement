namespace HostelManagement.Server.Models;

public class OwnerOrg
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Owner? Owner { get; set; }
}
