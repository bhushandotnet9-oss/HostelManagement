namespace HostelManagement.Server.Models;

public class Cot : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid OwnerOrgId { get; set; }

    public Guid RoomId { get; set; }
    public Room? Room { get; set; }

    public string Label { get; set; } = string.Empty;
    public CotStatus Status { get; set; } = CotStatus.Vacant;

    public Resident? CurrentResident { get; set; }
}
