using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain.Repositories;
using SupportDesk.Presentation.Security;

namespace SupportDesk.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(
    IAgentRepository agents,
    IAgentPasswordHasher passwordHasher,
    JwtTokenService tokens) : ControllerBase
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();
        var agent = await agents.GetByEmailAsync(email, ct);
        if (agent is null || !agent.IsActive ||
            !passwordHasher.Verify(agent, agent.PasswordHash, request.Password))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = InvalidCredentialsMessage
            });
        }

        var (token, expiresAtUtc) = tokens.CreateToken(agent);
        return Ok(new LoginResponse(token, expiresAtUtc,
            new AuthenticatedAgent(agent.Id, agent.FullName, agent.Email)));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<AuthenticatedAgent> Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue(ClaimTypes.Name);
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (!int.TryParse(id, out var agentId) || name is null || email is null)
        {
            return Unauthorized();
        }

        return Ok(new AuthenticatedAgent(agentId, name, email));
    }
}

public sealed record LoginRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(200)] string Password);

public sealed record AuthenticatedAgent(int Id, string FullName, string Email);

public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, AuthenticatedAgent User);
