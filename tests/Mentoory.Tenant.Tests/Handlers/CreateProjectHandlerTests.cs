using FluentAssertions;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Mentoory.Tenant.Application.Commands.CreateProject;
using Mentoory.Tenant.Domain.Aggregates.Incubator;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Tenant.Tests.Handlers;

public class CreateProjectHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IIncubatorRepository> _incubatorRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<ITimeProvider> _timeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CreateProjectHandler _handler;

    public CreateProjectHandlerTests()
    {
        _timeProvider.Setup(t => t.UtcNow).Returns(UtcNow);
        _projectRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _incubatorRepo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _projectRepo.Setup(r => r.Add(It.IsAny<Project>())).Returns((Project p) => p);

        _handler = new CreateProjectHandler(
            NullLogger<CreateProjectHandler>.Instance,
            _incubatorRepo.Object,
            _projectRepo.Object,
            _timeProvider.Object);
    }

    [Fact]
    public async Task Handle_WithExistingIncubator_CreatesProject()
    {
        var incubator = Incubator.Create("Test Inc", null, UtcNow);
        var externalId = incubator.ExternalId;

        _incubatorRepo.Setup(r => r.GetByExternalIdAsync(externalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incubator);

        var command = new CreateProjectCommand(externalId, "New Project", "Description");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _projectRepo.Verify(r => r.Add(It.Is<Project>(p => p.Name == "New Project")), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentIncubator_ReturnsFailure()
    {
        var fakeId = Guid.NewGuid();
        _incubatorRepo.Setup(r => r.GetByExternalIdAsync(fakeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incubator?)null);

        var command = new CreateProjectCommand(fakeId, "New Project", "Description");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        _projectRepo.Verify(r => r.Add(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DoesNotCallSaveEntitiesAsync_DirectlyOnHandler()
    {
        var incubator = Incubator.Create("Test Inc", null, UtcNow);
        _incubatorRepo.Setup(r => r.GetByExternalIdAsync(incubator.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incubator);

        var command = new CreateProjectCommand(incubator.ExternalId, "Project", null);
        await _handler.Handle(command, CancellationToken.None);

        // Like CreateIncubatorHandler, this handler relies on TransactionBehavior
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
