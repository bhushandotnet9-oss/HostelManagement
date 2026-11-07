using HostelManagement.Server.Auth;
using HostelManagement.Server.Controllers.Dtos;
using HostelManagement.Server.Data;
using HostelManagement.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HostelManagement.Server.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ICurrentOwnerContext _ownerContext;
    private readonly PasswordHasher<Owner> _passwordHasher = new();

    public AccountController(AppDbContext db, ITokenService tokenService, ICurrentOwnerContext ownerContext)
    {
        _db = db;
        _tokenService = tokenService;
        _ownerContext = ownerContext;
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<Guid?> Me() => Ok(_ownerContext.OwnerOrgId);

    [HttpPost("setup")]
    public async Task<ActionResult<AuthResponse>> Setup(SetupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrgName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("OrgName, Email, and Password are required.");
        }

        // Owners has a global tenant query filter, so EF Core's generated SQL
        // normally scopes this to the current owner's org - but there is no
        // authenticated owner yet, so IgnoreQueryFilters is required to check
        // for an existing email across all orgs.
        var emailTaken = await _db.Owners.IgnoreQueryFilters()
            .AnyAsync(o => o.Email == request.Email);
        if (emailTaken)
        {
            return Conflict("An account with this email already exists.");
        }

        var org = new OwnerOrg
        {
            Id = Guid.NewGuid(),
            Name = request.OrgName,
            CreatedAt = DateTime.UtcNow,
        };

        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            OwnerOrgId = org.Id,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
        };
        owner.PasswordHash = _passwordHasher.HashPassword(owner, request.Password);

        _db.OwnerOrgs.Add(org);
        _db.Owners.Add(owner);
        await _db.SaveChangesAsync();

        var token = _tokenService.IssueToken(owner);
        return Ok(new AuthResponse(token, owner.Email, owner.OwnerOrgId));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var owner = await _db.Owners.IgnoreQueryFilters()
            .SingleOrDefaultAsync(o => o.Email == request.Email);

        if (owner is null)
        {
            return Unauthorized();
        }

        var result = _passwordHasher.VerifyHashedPassword(owner, owner.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }

        var token = _tokenService.IssueToken(owner);
        return Ok(new AuthResponse(token, owner.Email, owner.OwnerOrgId));
    }
}
