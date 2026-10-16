using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Repositories;
using SupportDesk.Presentation.Controllers;
using SupportDesk.Presentation.Security;

namespace SupportDesk.UnitTests.Presentation;

public sealed class AuthControllerTests
{
    private const string Email = "alex.turner@northwind-support.example";

    [Fact]
    public async Task Login_UnknownEmailAndWrongPasswordReturnIdenticalGenericUnauthorized()
    {
        var agents = new Mock<IAgentRepository>();
        var passwordHasher = new Mock<IAgentPasswordHasher>();
        var activeAgent = NewAgent();
        agents.Setup(repository => repository.GetByEmailAsync(Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeAgent);
        passwordHasher.Setup(hasher => hasher.Verify(activeAgent, "encoded-hash", "wrong"))
            .Returns(false);
        activeAgent.SetPasswordHash("encoded-hash");
        var controller = Controller(agents, passwordHasher);

        var unknownEmail = await controller.Login(
            new LoginRequest("missing@example.test", "wrong"), CancellationToken.None);
        var wrongPassword = await controller.Login(
            new LoginRequest(Email, "wrong"), CancellationToken.None);

        var unknownProblem = Assert.IsType<ProblemDetails>(
            Assert.IsType<UnauthorizedObjectResult>(unknownEmail.Result).Value);
        var passwordProblem = Assert.IsType<ProblemDetails>(
            Assert.IsType<UnauthorizedObjectResult>(wrongPassword.Result).Value);
        Assert.Equal(unknownProblem.Status, passwordProblem.Status);
        Assert.Equal(unknownProblem.Title, passwordProblem.Title);
        Assert.Equal("Invalid email or password.", unknownProblem.Title);
    }

    [Fact]
    public async Task Login_InactiveAgentIsRejectedWithoutVerifyingPassword()
    {
        var agent = NewAgent();
        agent.Deactivate();
        var agents = new Mock<IAgentRepository>();
        agents.Setup(repository => repository.GetByEmailAsync(Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);
        var passwordHasher = new Mock<IAgentPasswordHasher>();

        var response = await Controller(agents, passwordHasher).Login(
            new LoginRequest(Email, "password"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(response.Result);
        passwordHasher.Verify(hasher => hasher.Verify(agent, It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void AgentPasswordHasher_HashesAndVerifiesWithoutStoringPlaintext()
    {
        var agent = NewAgent();
        var hasher = new AgentPasswordHasher();
        const string password = "NorthwindDev!2026";

        var passwordHash = hasher.Hash(agent, password);

        Assert.NotEqual(password, passwordHash);
        Assert.True(hasher.Verify(agent, passwordHash, password));
        Assert.False(hasher.Verify(agent, passwordHash, "incorrect"));
    }

    private static AuthController Controller(
        Mock<IAgentRepository> agents,
        Mock<IAgentPasswordHasher> passwordHasher)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "test-signing-key-that-is-long-enough-for-hmac-sha256",
            ["Jwt:Issuer"] = "NorthwindSupportDesk",
            ["Jwt:Audience"] = "NorthwindSupportDesk.Web"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new AuthController(agents.Object, passwordHasher.Object, new JwtTokenService(configuration));
    }

    private static Agent NewAgent()
    {
        var agent = new Agent("Alex Turner", Email, 10, DateTime.UtcNow);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(agent, 1);
        return agent;
    }
}
