using FluentAssertions;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Mentoory.Tenant.Application.Abstractions;
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
    private static readonly Guid KsTemplateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly Mock<IIncubatorRepository> _incubatorRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IKnowledgeStructureTemplateRepository> _ksTemplateRepo = new();
    private readonly Mock<IKnowledgeStructureProvisioner> _provisioner = new();
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

        _ksTemplateRepo
            .Setup(r => r.ExistsByExternalIdAsync(KsTemplateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _provisioner
            .Setup(p => p.CloneForProjectAsync(KsTemplateId, It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Guid.NewGuid()));

        _handler = new CreateProjectHandler(
            NullLogger<CreateProjectHandler>.Instance,
            _incubatorRepo.Object,
            _projectRepo.Object,
            _ksTemplateRepo.Object,
            _provisioner.Object,
            _timeProvider.Object);
    }

    [Fact]
    public async Task Handle_WithValidInputs_CreatesProjectAndProvisionsKnowledge()
    {
        var incubator = Incubator.Create("Test Inc", null, UtcNow);

        _incubatorRepo
            .Setup(r => r.GetByExternalIdAsync(incubator.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incubator);

        var command = new CreateProjectCommand(incubator.ExternalId, "New Project", "Description", KsTemplateId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _projectRepo.Verify(r => r.Add(It.Is<Project>(p =>
            p.Name == "New Project"
            && p.KnowledgeStructureTemplateExternalId == KsTemplateId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _provisioner.Verify(p => p.CloneForProjectAsync(
            KsTemplateId, It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentIncubator_ReturnsFailure()
    {
        var fakeId = Guid.NewGuid();
        _incubatorRepo
            .Setup(r => r.GetByExternalIdAsync(fakeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incubator?)null);

        var command = new CreateProjectCommand(fakeId, "New Project", "Description", KsTemplateId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        _projectRepo.Verify(r => r.Add(It.IsAny<Project>()), Times.Never);
        _provisioner.Verify(p => p.CloneForProjectAsync(
            It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMissingKsTemplate_ReturnsFailureBeforeSaving()
    {
        var incubator = Incubator.Create("Test Inc", null, UtcNow);
        var unknownTemplateId = Guid.NewGuid();

        _incubatorRepo
            .Setup(r => r.GetByExternalIdAsync(incubator.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incubator);
        _ksTemplateRepo
            .Setup(r => r.ExistsByExternalIdAsync(unknownTemplateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateProjectCommand(incubator.ExternalId, "Project", null, unknownTemplateId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _projectRepo.Verify(r => r.Add(It.IsAny<Project>()), Times.Never);
        _provisioner.Verify(p => p.CloneForProjectAsync(
            It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProvisionerFails_ReturnsFailureButProjectAlreadySaved()
    {
        var incubator = Incubator.Create("Test Inc", null, UtcNow);

        _incubatorRepo
            .Setup(r => r.GetByExternalIdAsync(incubator.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incubator);
        _provisioner
            .Setup(p => p.CloneForProjectAsync(KsTemplateId, It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Failure(
                ResultErrorCodes.GenericError,
                ("Plantilla", "La plantilla de conocimiento no fue encontrada.")));

        var command = new CreateProjectCommand(incubator.ExternalId, "Project", null, KsTemplateId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().Contain(m => m.Context == "KnowledgeStructure");
        _projectRepo.Verify(r => r.Add(It.IsAny<Project>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
