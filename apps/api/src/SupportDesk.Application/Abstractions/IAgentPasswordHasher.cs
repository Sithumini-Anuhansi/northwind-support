using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Application.Abstractions;

/// <summary>Uses a vetted salted password-hashing implementation at the application boundary.</summary>
public interface IAgentPasswordHasher
{
    string Hash(Agent agent, string password);

    bool Verify(Agent agent, string passwordHash, string password);
}
