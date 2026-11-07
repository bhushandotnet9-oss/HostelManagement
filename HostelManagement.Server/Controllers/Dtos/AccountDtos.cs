namespace HostelManagement.Server.Controllers.Dtos;

public record SetupRequest(string OrgName, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, string Email, Guid OwnerOrgId);
