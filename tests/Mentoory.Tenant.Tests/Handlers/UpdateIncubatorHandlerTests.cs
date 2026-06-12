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
        var incubator = Incubator.Create("Original Name", "Original description", UtcNow);
        _repo.Setup(r => r.GetByExternalIdAsync(incubator.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incubator);

        var command = new UpdateIncubatorCommand(
            incubator.ExternalId, "Updated Name", "Updated description", IsActive: false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        incubator.IsActive.Should().BeFalse();
        incubator.Name.Should().Be("Updated Name");
        incubator.Description.Should().Be("Updated description");
        _repo.Verify(r => r.Update(incubator), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIsActiveTrue_ReactivatesDeactivatedIncubator()
    {
        var incubator = Incubator.Create("Name", "Desc", UtcNow);
        incubator.Deactivate(UtcNow);
        incubator.IsActive.Should().BeFalse();

        _repo.Setup(r => r.GetByExternalIdAsync(incubator.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incubator);

        var command = new UpdateIncubatorCommand(
            incubator.ExternalId, "Name", "Desc", IsActive: true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        incubator.IsActive.Should().BeTrue();
        _repo.Verify(r => r.Update(incubator), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIncubatorNotFound_ReturnsFailure()
    {
        var externalId = Guid.NewGuid();
        _repo.Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incubator?)null);

        var command = new UpdateIncubatorCommand(externalId, "Name", null, IsActive: true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _repo.Verify(r => r.Update(It.IsAny<Incubator>()), Times.Never);
    }
}
