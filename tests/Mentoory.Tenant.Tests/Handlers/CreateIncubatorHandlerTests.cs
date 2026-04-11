using FluentAssertions;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Mentoory.Tenant.Application.Commands.CreateIncubator;
using Mentoory.Tenant.Domain.Aggregates.Incubator;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Tenant.Tests.Handlers;

public class CreateIncubatorHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IIncubatorRepository> _repo = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CreateIncubatorHandler _handler;

    public CreateIncubatorHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _repo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repo.Setup(r => r.Add(It.IsAny<Incubator>())).Returns((Incubator i) => i);

        _handler = new CreateIncubatorHandler(
            NullLogger<CreateIncubatorHandler>.Instance,
            _repo.Object,
            _timeProvider.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ReturnsExternalId()
    {
        var command = new CreateIncubatorCommand("Test Incubator", "A description");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _repo.Verify(r => r.Add(It.Is<Incubator>(i => i.Name == "Test Incubator")), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullDescription_Succeeds()
    {
        var command = new CreateIncubatorCommand("No Desc Inc", null);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithEmptyName_ReturnsFailure()
    {
        // The domain entity enforces this with ArgumentException
        var command = new CreateIncubatorCommand(string.Empty, null);
        var result = await _handler.Handle(command, CancellationToken.None);

        // CreateIncubatorHandler catches exceptions and returns failure
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.Unknown);
    }

    [Fact]
    public async Task Handle_DoesNotCallSaveEntitiesAsync_DirectlyOnHandler()
    {
        // CreateIncubatorHandler relies on TransactionBehavior for saving.
        // The handler does NOT call SaveEntitiesAsync explicitly.
        // This test documents this behavior - if the TransactionBehavior is removed,
        // data will NOT be persisted.
        var command = new CreateIncubatorCommand("Test", "Desc");
        await _handler.Handle(command, CancellationToken.None);

        // Verify SaveEntitiesAsync was NOT called by the handler
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
