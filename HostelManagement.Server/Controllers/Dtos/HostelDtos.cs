namespace HostelManagement.Server.Controllers.Dtos;

public record CreateHostelRequest(string Name, string Location);

public record UpdateHostelRequest(string Name, string Location);

public record HostelResponse(Guid Id, string Name, string Location);
