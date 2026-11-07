using HostelManagement.Server.Models;

namespace HostelManagement.Server.Controllers.Dtos;

public record CreateCotRequest(string Label);

public record CotResponse(Guid Id, Guid RoomId, string Label, CotStatus Status);

public record HostelOccupancyResponse(int Total, int Occupied, int Vacant);
