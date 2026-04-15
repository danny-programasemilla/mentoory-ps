using FluentAssertions;
using MediatR;
using Mentoory.Access.Application.Commands.AssignRole;
using Mentoory.Access.Application.Commands.ChangePassword;
using Mentoory.Access.Application.Commands.LoginUser;
using Mentoory.Access.Application.Commands.LogoutUser;
using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Access.Application.Commands.RevokeRole;
using Mentoory.Access.Application.Services;
using Mentoory.Access.Application.Commands.SetActiveContext;
using Mentoory.Access.Application.Commands.VerifyEmail;
using Mentoory.Access.Application.Queries.CheckPermission;
using Mentoory.Access.Application.Queries.GetUserContexts;
using Mentoory.Access.Application.Queries.ListIncubatorMembers;
using Mentoory.Access.Application.Queries.ListUsers;
using Mentoory.Access.Application.Queries.ValidateSession;
using Mentoory.Shared.Application.DataTables;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Application.Interfaces;
using Mentoory.Shared.Application.IntegrationEvents;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Infrastructure.Persistence;
using Mentoory.Tenant.Application.Commands.CreateIncubator;
using Mentoory.Tenant.Application.Commands.CreateProject;
using Mentoory.Tenant.Domain.Repositories;
using Mentoory.Tenant.Infrastructure.Persistence;
using Mentoory.Tests.Integration.Fixtures;
using Mentoory.Web.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.DependencyInjection;

/// <summary>
/// Smoke tests that verify every critical service and MediatR handler can be resolved
/// from the DI container. These catch missing registrations at startup time.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class ServiceResolutionTests : IntegrationTestBase
{
    public ServiceResolutionTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Theory]
    [InlineData(typeof(IMediator))]
    [InlineData(typeof(ISender))]
    [InlineData(typeof(IPublisher))]
    [InlineData(typeof(ITimeProvider))]
    [InlineData(typeof(ITenantContext))]
    [InlineData(typeof(IAuditService))]
    [InlineData(typeof(IIntegrationEventService))]
    [InlineData(typeof(IDbContextFactory))]
    public void SharedServices_ShouldResolve(Type serviceType)
    {
        using var scope = CreateScope();
        var service = scope.ServiceProvider.GetService(serviceType);
        service.Should().NotBeNull($"{serviceType.Name} must be registered in DI");
    }

    [Theory]
    [InlineData(typeof(AccessDbContext))]
    [InlineData(typeof(IUserRepository))]
    [InlineData(typeof(IAuthSessionRepository))]
    [InlineData(typeof(IPasswordHasher))]
    [InlineData(typeof(IUserRegistrationService))]
    public void IdentityInfrastructure_ShouldResolve(Type serviceType)
    {
        using var scope = CreateScope();
        var service = scope.ServiceProvider.GetService(serviceType);
        service.Should().NotBeNull($"{serviceType.Name} must be registered in DI");
    }

    [Theory]
    [InlineData(typeof(AccessDbContext))]
    [InlineData(typeof(IRoleAssignmentRepository))]
    public void AuthorizationInfrastructure_ShouldResolve(Type serviceType)
    {
        using var scope = CreateScope();
        var service = scope.ServiceProvider.GetService(serviceType);
        service.Should().NotBeNull($"{serviceType.Name} must be registered in DI");
    }

    [Theory]
    [InlineData(typeof(TenantDbContext))]
    [InlineData(typeof(IIncubatorRepository))]
    [InlineData(typeof(IProjectRepository))]
    public void TenantInfrastructure_ShouldResolve(Type serviceType)
    {
        using var scope = CreateScope();
        var service = scope.ServiceProvider.GetService(serviceType);
        service.Should().NotBeNull($"{serviceType.Name} must be registered in DI");
    }

    [Theory]
    [InlineData(typeof(IRequestHandler<RegisterUserCommand, Mentoory.Shared.Application.Result>))]
    [InlineData(typeof(IRequestHandler<LoginUserCommand, Mentoory.Shared.Application.Result<LoginUserResult>>))]
    [InlineData(typeof(IRequestHandler<LogoutUserCommand, Mentoory.Shared.Application.Result>))]
    [InlineData(typeof(IRequestHandler<ChangePasswordCommand, Mentoory.Shared.Application.Result>))]
    [InlineData(typeof(IRequestHandler<VerifyEmailCommand, Mentoory.Shared.Application.Result>))]
    [InlineData(typeof(IRequestHandler<ValidateSessionQuery, Mentoory.Shared.Application.Result<Mentoory.Access.Domain.Aggregates.AuthSession.AuthSession?>>))]
    [InlineData(typeof(IRequestHandler<ListUsersQuery, Mentoory.Shared.Application.Result<DataTableResponse<UserListItemDto>>>))]
    [InlineData(typeof(IRequestHandler<ListIncubatorMembersQuery, Mentoory.Shared.Application.Result<DataTableResponse<IncubatorMemberListItemDto>>>))]
    [InlineData(typeof(IRequestHandler<AssignRoleCommand, Mentoory.Shared.Application.Result>))]
    [InlineData(typeof(IRequestHandler<RevokeRoleCommand, Mentoory.Shared.Application.Result>))]
    [InlineData(typeof(IRequestHandler<SetActiveContextCommand, Mentoory.Shared.Application.Result<Mentoory.Access.Domain.ReadModels.UserContext>>))]
    [InlineData(typeof(IRequestHandler<CheckPermissionQuery, Mentoory.Shared.Application.Result<bool>>))]
    [InlineData(typeof(IRequestHandler<GetUserContextsQuery, Mentoory.Shared.Application.Result<System.Collections.Generic.List<Mentoory.Access.Domain.ReadModels.UserContext>>>))]
    [InlineData(typeof(IRequestHandler<CreateIncubatorCommand, Mentoory.Shared.Application.Result<System.Guid>>))]
    [InlineData(typeof(IRequestHandler<CreateProjectCommand, Mentoory.Shared.Application.Result<System.Guid>>))]
    public void MediatRHandlers_ShouldResolve(Type handlerType)
    {
        using var scope = CreateScope();
        var handler = scope.ServiceProvider.GetService(handlerType);
        handler.Should().NotBeNull($"Handler {handlerType.Name} must be resolvable from DI");
    }

    [Theory]
    [InlineData(typeof(FluentValidation.IValidator<RegisterUserCommand>))]
    [InlineData(typeof(FluentValidation.IValidator<LoginUserCommand>))]
    [InlineData(typeof(FluentValidation.IValidator<AssignRoleCommand>))]
    [InlineData(typeof(FluentValidation.IValidator<RevokeRoleCommand>))]
    [InlineData(typeof(FluentValidation.IValidator<SetActiveContextCommand>))]
    public void Validators_ShouldResolve(Type validatorType)
    {
        using var scope = CreateScope();
        var validators = scope.ServiceProvider.GetServices(validatorType);
        validators.Should().NotBeEmpty($"At least one validator for {validatorType.GenericTypeArguments[0].Name} must be registered");
    }

    [Fact]
    public void DbContextFactory_ShouldResolveDbContextForIdentity()
    {
        using var scope = CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory>();
        var result = factory.TryGetDbContextForRequest<RegisterUserCommand>(out var dbContext);
        result.Should().BeTrue();
        dbContext.Should().NotBeNull();
    }

    [Fact]
    public void DbContextFactory_ShouldResolveDbContextForAuthorization()
    {
        using var scope = CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory>();
        var result = factory.TryGetDbContextForRequest<AssignRoleCommand>(out var dbContext);
        result.Should().BeTrue();
        dbContext.Should().NotBeNull();
    }

    [Fact]
    public void DbContextFactory_ShouldResolveDbContextForTenant()
    {
        using var scope = CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory>();
        var result = factory.TryGetDbContextForRequest<CreateIncubatorCommand>(out var dbContext);
        result.Should().BeTrue();
        dbContext.Should().NotBeNull();
    }
}
