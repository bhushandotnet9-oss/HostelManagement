namespace HostelManagement.Server.Models;

public class Hostel : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid OwnerOrgId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public List<Room> Rooms { get; set; } = [];
}
