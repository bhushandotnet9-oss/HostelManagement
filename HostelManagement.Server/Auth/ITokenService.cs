using HostelManagement.Server.Models;

namespace HostelManagement.Server.Auth;

public interface ITokenService
{
    string IssueToken(Owner owner);
}
