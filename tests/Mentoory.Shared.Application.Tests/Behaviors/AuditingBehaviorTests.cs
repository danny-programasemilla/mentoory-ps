using FluentAssertions;
using MediatR;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Application.Behaviors;
using Mentoory.Shared.Application.Interfaces;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using IBaseRequest = Mentoory.Shared.Application.MediatR.IBaseRequest;

namespace Mentoory.Shared.Application.Tests.Behaviors;

public class AuditingBehaviorTests
{
    private static readonly DateTime FakeNow = new(2026, 4, 18, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid FakeCorrelationId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<ITenantContext> _tenantContext = new();
    private readonly Mock<ICorrelationContext> _correlationContext = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();

    public AuditingBehaviorTests()
    {
        _tenantContext.SetupGet(t => t.UserId).Returns(42);
        _tenantContext.SetupGet(t => t.UserEmail).Returns("actor@example.com");
        _tenantContext.SetupGet(t => t.CurrentIncubatorId).Returns(7);
        _tenantContext.SetupGet(t => t.IncubatorId).Returns(7);
        _tenantContext.SetupGet(t => t.ProjectId).Returns(13);
        _tenantContext.SetupGet(t => t.Role).Returns("GlobalAdmin");
        _correlationContext.SetupGet(c => c.CorrelationId).Returns(FakeCorrelationId);
        _correlationContext.SetupGet(c => c.ClientIpAddress).Returns("127.0.0.1");
        _timeProvider.SetupGet(t => t.UtcNow).Returns(FakeNow);
    }

    [Fact]
    public async Task No_attribute_passes_through_without_audit()
    {
        var behavior = CreateBehavior<PlainCommand, Result>();
        var response = await behavior.Handle(new PlainCommand(), _ => Task.FromResult(Result.Success()), CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        _auditService.Verify(a => a.LogAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Manual_mode_passes_through_without_audit()
    {
        var behavior = CreateBehavior<ManualCommand, Result>();
        await behavior.Handle(new ManualCommand(), _ => Task.FromResult(Result.Success()), CancellationToken.None);

        _auditService.Verify(a => a.LogAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Automatic_success_writes_success_outcome()
    {
        AuditEntry? captured = null;
        _auditService.Setup(a => a.LogAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEntry, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        var behavior = CreateBehavior<AutoCommand, Result>();
        await behavior.Handle(new AutoCommand("visible"), _ => Task.FromResult(Result.Success()), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.EventType.Should().Be("Test.Event");
        captured.Outcome.Should().Be("Success");
        captured.ExceptionType.Should().BeNull();
        captured.Action.Should().Be(nameof(AutoCommand));
        captured.OccurredAtUtc.Should().Be(FakeNow);
        captured.CorrelationId.Should().Be(FakeCorrelationId);
    }

    [Fact]
    public async Task Automatic_failure_result_writes_failure_outcome()
    {
        AuditEntry? captured = null;
        _auditService.Setup(a => a.LogAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEntry, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        var behavior = CreateBehavior<AutoCommand, Result>();
        await behavior.Handle(
            new AutoCommand("visible"),
            _ => Task.FromResult(Result.Failure(ResultErrorCodes.GenericError, ("x", "y"))),
            CancellationToken.None);

        captured!.Outcome.Should().Be("Failure");
        captured.ExceptionType.Should().BeNull();
    }

    [Fact]
    public async Task Automatic_exception_writes_failure_with_exception_type_and_rethrows()
    {
        AuditEntry? captured = null;
        _auditService.Setup(a => a.LogAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEntry, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        var behavior = CreateBehavior<AutoCommand, Result>();
        Func<Task> act = async () => await behavior.Handle(
            new AutoCommand("visible"),
            _ => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        captured!.Outcome.Should().Be("Failure");
        captured.ExceptionType.Should().Be(typeof(InvalidOperationException).FullName);
    }

    [Fact]
    public async Task Redaction_runs_end_to_end_via_details_payload()
    {
        AuditEntry? captured = null;
        _auditService.Setup(a => a.LogAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEntry, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        var behavior = CreateBehavior<SecretCommand, Result>();
        await behavior.Handle(
            new SecretCommand("someone@x.com", "sup3r$ecret"),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        captured!.Details.Should().NotContain("sup3r$ecret");
        captured.Details.Should().Contain("***REDACTED***");
        captured.Details.Should().Contain("someone@x.com");
    }

    [Fact]
    public async Task Anonymous_resolver_overrides_user_email()
    {
        AuditEntry? captured = null;
        _auditService.Setup(a => a.LogAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEntry, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        // Tenant context is anonymous here.
        _tenantContext.SetupGet(t => t.UserId).Returns((long?)null);
        _tenantContext.SetupGet(t => t.UserEmail).Returns((string?)null);

        var services = new ServiceCollection();
        services.AddScoped<IAuditAnonymousResolver<AnonCommand>, AnonResolver>();
        var provider = services.BuildServiceProvider();

        var behavior = new AuditingBehavior<AnonCommand, Result>(
            _auditService.Object, _tenantContext.Object, _correlationContext.Object,
            _timeProvider.Object, Options.Create(new AuditOptions()), provider,
            NullLogger<AuditingBehavior<AnonCommand, Result>>.Instance);

        await behavior.Handle(
            new AnonCommand("anon@x.com"),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        captured!.UserEmail.Should().Be("anon@x.com");
        captured.UserId.Should().BeNull();
    }

    [Fact]
    public async Task Audit_service_failure_is_swallowed_and_command_still_succeeds()
    {
        _auditService
            .Setup(a => a.LogAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated DB outage"));

        var behavior = CreateBehavior<AutoCommand, Result>();

        var response = await behavior.Handle(
            new AutoCommand("hello"),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        response.IsSuccess.Should().BeTrue(
            "a failing audit service must not prevent the command from returning its result");
    }

    private AuditingBehavior<TReq, TRes> CreateBehavior<TReq, TRes>()
        where TReq : IRequest<TRes>
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        return new AuditingBehavior<TReq, TRes>(
            _auditService.Object,
            _tenantContext.Object,
            _correlationContext.Object,
            _timeProvider.Object,
            Options.Create(new AuditOptions()),
            provider,
            NullLogger<AuditingBehavior<TReq, TRes>>.Instance);
    }

    public sealed record PlainCommand : IBaseRequest;

    [Audited("Test.Manual", Mode = AuditMode.Manual)]
    public sealed record ManualCommand : IBaseRequest;

    [Audited("Test.Event", EntityType = "TestEntity")]
    public sealed record AutoCommand(string Note) : IBaseRequest;

    [Audited("Test.Secret")]
    public sealed record SecretCommand(string Email, string Password) : IBaseRequest;

    [Audited("Test.Anon")]
    public sealed record AnonCommand(string Email) : IBaseRequest;

    public sealed class AnonResolver : IAuditAnonymousResolver<AnonCommand>
    {
        public (long? UserId, string? UserEmail) Resolve(AnonCommand request) => (null, request.Email);
    }
}
