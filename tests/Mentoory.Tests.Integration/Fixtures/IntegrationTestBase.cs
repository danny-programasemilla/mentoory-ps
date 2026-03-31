using MediatR;
using Mentoory.Identity.Application.Commands.LoginUser;
using Mentoory.Identity.Application.Commands.RegisterUser;
using Mentoory.Identity.Domain.Aggregates.AuthSession;
using Mentoory.Identity.Domain.Enums;
using Mentoory.Identity.Infrastructure.Persistence;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Fixtures;

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IntegrationTestBase(MentooryWebApplicationFactory factory)
    {
        Factory = factory;
    }

    protected MentooryWebApplicationFactory Factory { get; }

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected IServiceScope CreateScope() => Factory.Services.CreateScope();

    protected async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request);
    }

    protected async Task<TResponse> SendWithTenantAsync<TResponse>(IRequest<TResponse> request, long? incubatorId)
    {
        using var scope = CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        ((Mentoory.Shared.Infrastructure.Services.TenantContextService)tenantContext).CurrentIncubatorId = incubatorId;
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request);
    }

    protected async Task<Result> RegisterUserAsync(
        string email = "test@example.com",
        string country = "CO",
        string nationalId = "123456789",
        string firstName = "Test",
        string lastName = "User",
        string password = "SecureP@ss123!")
    {
        return await SendAsync(new RegisterUserCommand(email, country, nationalId, firstName, lastName, password));
    }

    protected async Task<(Result RegisterResult, long UserId)> RegisterAndActivateUserAsync(
        string email = "test@example.com",
        string country = "CO",
        string nationalId = "123456789",
        string firstName = "Test",
        string lastName = "User",
        string password = "SecureP@ss123!")
    {
        var registerResult = await RegisterUserAsync(email, country, nationalId, firstName, lastName, password);

        if (registerResult.IsFailure)
        {
            return (registerResult, 0);
        }

        // Activate user directly via DbContext (email verification token flow
        // is not fully wired for integration tests)
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await dbContext.Users.FirstAsync(u => u.Email.NormalizedValue == normalizedEmail);
        user.Activate(DateTime.UtcNow);
        await dbContext.SaveChangesAsync();

        return (registerResult, user.Id);
    }

    protected async Task<(Result<AuthSession> LoginResult, long UserId)> RegisterActivateAndLoginAsync(
        string email = "test@example.com",
        string country = "CO",
        string nationalId = "123456789",
        string firstName = "Test",
        string lastName = "User",
        string password = "SecureP@ss123!")
    {
        var (registerResult, userId) = await RegisterAndActivateUserAsync(email, country, nationalId, firstName, lastName, password);

        if (registerResult.IsFailure)
        {
            return (Result<AuthSession>.Failure(ResultErrorCodes.GenericError, ("Setup", "Registration failed")), 0);
        }

        var loginResult = await SendAsync(new LoginUserCommand(email, password, "127.0.0.1", "TestAgent"));
        return (loginResult, userId);
    }
}
