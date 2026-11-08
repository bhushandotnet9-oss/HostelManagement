namespace HostelManagement.Server.Controllers.Dtos;

public record CreateRoomRequest(string Name);

public record UpdateRoomRequest(string Name);

public record RoomResponse(Guid Id, Guid HostelId, string Name);
