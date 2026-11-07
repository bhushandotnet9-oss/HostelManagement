namespace HostelManagement.Server.Models;

public class Room : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid OwnerOrgId { get; set; }

    public Guid HostelId { get; set; }
    public Hostel? Hostel { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<Cot> Cots { get; set; } = [];
}
