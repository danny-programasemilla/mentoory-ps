using FluentAssertions;
using Mentoory.Knowledge.Application.Commands.ArchiveKnowledgeStructureTemplate;
using Mentoory.Knowledge.Application.Commands.CreateKnowledgeStructureTemplate;
using Mentoory.Knowledge.Application.Commands.DeleteKnowledgeStructureTemplate;
using Mentoory.Knowledge.Application.Commands.UnarchiveKnowledgeStructureTemplate;
using Mentoory.Knowledge.Application.Commands.UpdateKnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Mentoory.Knowledge.Tests.Handlers;

/// <summary>
/// Unit tests for the five root-level command handlers of the
/// <see cref="KnowledgeStructureTemplate"/> aggregate: Create, Update, Archive, Unarchive, Delete.
/// </summary>
public class KnowledgeStructureTemplateHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IKnowledgeStructureTemplateRepository> _repositoryMock = new();
    private readonly Mock<ITimeProvider> _timeProviderMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public KnowledgeStructureTemplateHandlerTests()
    {
        _timeProviderMock.Setup(t => t.UtcNow).Returns(UtcNow);
        _repositoryMock.Setup(r => r.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task CreateHandler_WithValidInput_AddsTemplateAndReturnsExternalId()
    {
        KnowledgeStructureTemplate? captured = null;
        _repositoryMock.Setup(r => r.Add(It.IsAny<KnowledgeStructureTemplate>()))
            .Callback<KnowledgeStructureTemplate>(t => captured = t)
            .Returns<KnowledgeStructureTemplate>(t => t);

        var handler = new CreateKnowledgeStructureTemplateHandler(
            _repositoryMock.Object,
            _timeProviderMock.Object,
            Mock.Of<ILogger<CreateKnowledgeStructureTemplateHandler>>());

        var result = await handler.Handle(
            new CreateKnowledgeStructureTemplateCommand("Plantilla Base", "desc"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        captured.Should().NotBeNull();
        captured!.ExternalId.Should().Be(result.Value);
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateHandler_SetsCreatedAtUtcFromTimeProvider()
    {
        KnowledgeStructureTemplate? captured = null;
        _repositoryMock.Setup(r => r.Add(It.IsAny<KnowledgeStructureTemplate>()))
            .Callback<KnowledgeStructureTemplate>(t => captured = t)
            .Returns<KnowledgeStructureTemplate>(t => t);

        var handler = new CreateKnowledgeStructureTemplateHandler(
            _repositoryMock.Object,
            _timeProviderMock.Object,
            Mock.Of<ILogger<CreateKnowledgeStructureTemplateHandler>>());

        await handler.Handle(
            new CreateKnowledgeStructureTemplateCommand("Plantilla Base", null),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.CreatedAtUtc.Should().Be(UtcNow);
    }

    [Fact]
    public async Task UpdateHandler_WithExistingTemplate_CallsUpdateDetailsAndSaves()
    {
        var template = FreshTemplate();
        _repositoryMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var handler = new UpdateKnowledgeStructureTemplateHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new UpdateKnowledgeStructureTemplateCommand(template.ExternalId, "Nuevo Nombre", "nueva desc"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        template.Name.Should().Be("Nuevo Nombre");
        template.Description.Should().Be("nueva desc");
        template.Version.Should().Be(2);
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateHandler_WithNonExistingTemplate_ReturnsFailure()
    {
        _repositoryMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeStructureTemplate?)null);

        var handler = new UpdateKnowledgeStructureTemplateHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new UpdateKnowledgeStructureTemplateCommand(Guid.NewGuid(), "Nombre", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().Contain("no fue encontrada");
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ArchiveHandler_WithExistingTemplate_SetsIsArchivedTrue()
    {
        var template = FreshTemplate();
        var versionBefore = template.Version;
        _repositoryMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var handler = new ArchiveKnowledgeStructureTemplateHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new ArchiveKnowledgeStructureTemplateCommand(template.ExternalId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        template.IsArchived.Should().BeTrue();
        template.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public async Task UnarchiveHandler_WithArchivedTemplate_SetsIsArchivedFalse()
    {
        var template = FreshTemplate();
        template.Archive();
        _repositoryMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var handler = new UnarchiveKnowledgeStructureTemplateHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new UnarchiveKnowledgeStructureTemplateCommand(template.ExternalId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        template.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteHandler_WithExistingTemplateAndNoClones_CallsRemoveAndSaves()
    {
        var template = FreshTemplate();
        _repositoryMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var structureRepoMock = new Mock<IKnowledgeStructureRepository>();
        structureRepoMock
            .Setup(r => r.CountClonesBySourceTemplateIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = new DeleteKnowledgeStructureTemplateHandler(
            _repositoryMock.Object,
            structureRepoMock.Object,
            Mock.Of<ILogger<DeleteKnowledgeStructureTemplateHandler>>());

        var result = await handler.Handle(
            new DeleteKnowledgeStructureTemplateCommand(template.ExternalId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.Remove(template), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteHandler_WithExistingTemplateAndClones_ReturnsFailure()
    {
        var template = FreshTemplate();
        _repositoryMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var structureRepoMock = new Mock<IKnowledgeStructureRepository>();
        structureRepoMock
            .Setup(r => r.CountClonesBySourceTemplateIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var handler = new DeleteKnowledgeStructureTemplateHandler(
            _repositoryMock.Object,
            structureRepoMock.Object,
            Mock.Of<ILogger<DeleteKnowledgeStructureTemplateHandler>>());

        var result = await handler.Handle(
            new DeleteKnowledgeStructureTemplateCommand(template.ExternalId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages![0].Message.Should().Contain("clones");
        _repositoryMock.Verify(r => r.Remove(It.IsAny<KnowledgeStructureTemplate>()), Times.Never);
    }

    [Fact]
    public async Task DeleteHandler_WithNonExistingTemplate_ReturnsFailure()
    {
        _repositoryMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeStructureTemplate?)null);

        var structureRepoMock = new Mock<IKnowledgeStructureRepository>();

        var handler = new DeleteKnowledgeStructureTemplateHandler(
            _repositoryMock.Object,
            structureRepoMock.Object,
            Mock.Of<ILogger<DeleteKnowledgeStructureTemplateHandler>>());

        var result = await handler.Handle(
            new DeleteKnowledgeStructureTemplateCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().Contain("no fue encontrada");
        _repositoryMock.Verify(r => r.Remove(It.IsAny<KnowledgeStructureTemplate>()), Times.Never);
    }

    private static KnowledgeStructureTemplate FreshTemplate() =>
        KnowledgeStructureTemplate.Create("Test Template", "desc", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
}
