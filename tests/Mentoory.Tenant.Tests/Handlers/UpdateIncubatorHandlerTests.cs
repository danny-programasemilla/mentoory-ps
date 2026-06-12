using FluentAssertions;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Application.Commands.UpdateIncubator;
using Mentoory.Tenant.Domain.Aggregates.Incubator;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Tenant.Tests.Handlers;

public class UpdateIncubatorHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IIncubatorRepository> _repo = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly UpdateIncubatorHandler _handler;

    public UpdateIncubatorHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);

        _handler = new UpdateIncubatorHandler(
            NullLogger<UpdateIncubatorHandler>.Instance,
            _repo.Object,
            _timeProvider.Object);
    }

    [Fact]
    public async Task Handle_WhenIsActiveFalse_DeactivatesAndUpdatesFields()
    {
        // An incubator is created active (IsActive defaults to true).
        var incubator = GivenExistingIncubator("Original Name", "Original description", UtcNow.AddDays(-1));

        var command = new UpdateIncubatorCommand(
            incubator.ExternalId, "Updated Name", "Updated description", IsActive: false, CallerIncubatorId: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        incubator.IsActive.Should().BeFalse();
        incubator.Name.Should().Be("Updated Name");
        incubator.Description.Should().Be("Updated description");
        incubator.UpdatedAtUtc.Should().Be(UtcNow);
        _repo.Verify(r => r.Update(incubator), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIsActiveTrue_ReactivatesDeactivatedIncubator()
    {
        var incubator = GivenExistingIncubator("Name", "Desc", UtcNow.AddDays(-1));
        incubator.Deactivate(UtcNow.AddDays(-1));
        incubator.IsActive.Should().BeFalse();

        var command = new UpdateIncubatorCommand(
            incubator.ExternalId, "Name", "Desc", IsActive: true, CallerIncubatorId: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        incubator.IsActive.Should().BeTrue();
        incubator.UpdatedAtUtc.Should().Be(UtcNow);
        _repo.Verify(r => r.Update(incubator), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOnlyEstadoChanges_LeavesNameAndDescriptionUnchanged()
    {
        // FR-010 / US3 acceptance: changing ONLY estado must not alter Name/Description.
        var incubator = GivenExistingIncubator("Keep Name", "Keep description", UtcNow.AddDays(-1));

        var command = new UpdateIncubatorCommand(
            incubator.ExternalId, "Keep Name", "Keep description", IsActive: false, CallerIncubatorId: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        incubator.IsActive.Should().BeFalse();
        incubator.Name.Should().Be("Keep Name");
        incubator.Description.Should().Be("Keep description");
    }

    [Fact]
    public async Task Handle_WhenIsActiveTrueOnActiveIncubator_RemainsActive()
    {
        // US3 acceptance: saving without changing estado leaves it as-is (no-op true -> true).
        var incubator = GivenExistingIncubator("Name", "Desc", UtcNow.AddDays(-1));
        incubator.IsActive.Should().BeTrue();

        var command = new UpdateIncubatorCommand(
            incubator.ExternalId, "Name", "Desc", IsActive: true, CallerIncubatorId: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        incubator.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenIsActiveFalseOnInactiveIncubator_RemainsInactive()
    {
        // US3 acceptance: saving without changing estado leaves it as-is (no-op false -> false).
        var incubator = GivenExistingIncubator("Name", "Desc", UtcNow.AddDays(-1));
        incubator.Deactivate(UtcNow.AddDays(-1));
        incubator.IsActive.Should().BeFalse();

        var command = new UpdateIncubatorCommand(
            incubator.ExternalId, "Name", "Desc", IsActive: false, CallerIncubatorId: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        incubator.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenCallerScopeDoesNotMatchIncubator_ReturnsFailureWithoutUpdating()
    {
        // A scoped caller (active incubator id != target) must not be able to modify another incubator.
        var incubator = GivenExistingIncubator("Other Incubator", "Desc", UtcNow.AddDays(-1));

        var command = new UpdateIncubatorCommand(
            incubator.ExternalId, "Hijacked", "Hijacked", IsActive: false, CallerIncubatorId: 999L);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        incubator.Name.Should().Be("Other Incubator");
        incubator.IsActive.Should().BeTrue();
        _repo.Verify(r => r.Update(It.IsAny<Incubator>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenIncubatorNotFound_ReturnsFailure()
    {
        var externalId = Guid.NewGuid();
        _repo.Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incubator?)null);

        var command = new UpdateIncubatorCommand(externalId, "Name", null, IsActive: true, CallerIncubatorId: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _repo.Verify(r => r.Update(It.IsAny<Incubator>()), Times.Never);
    }

    private Incubator GivenExistingIncubator(string name, string? description, DateTime createdAtUtc)
    {
        var incubator = Incubator.Create(name, description, createdAtUtc);
        _repo.Setup(r => r.GetByExternalIdAsync(incubator.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incubator);
        return incubator;
    }
}
