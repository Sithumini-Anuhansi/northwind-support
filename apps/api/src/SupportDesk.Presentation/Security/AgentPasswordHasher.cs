using Microsoft.AspNetCore.Identity;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Presentation.Security;

public sealed class AgentPasswordHasher : IAgentPasswordHasher
{
    private readonly PasswordHasher<Agent> _hasher = new();

    public string Hash(Agent agent, string password) => _hasher.HashPassword(agent, password);

    public bool Verify(Agent agent, string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(agent, passwordHash, password) != PasswordVerificationResult.Failed;
}
