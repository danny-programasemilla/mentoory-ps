using FluentAssertions;
using Mentoory.Knowledge.Application.Commands.AddModule;
using Mentoory.Knowledge.Application.Commands.AddResource;
using Mentoory.Knowledge.Application.Commands.AddSubject;
using Mentoory.Knowledge.Application.Commands.AddTopic;
using Mentoory.Knowledge.Application.Commands.DeleteModule;
using Mentoory.Knowledge.Application.Commands.SetSyncMode;
using Mentoory.Knowledge.Application.Commands.UpdateKnowledgeStructure;
using Mentoory.Knowledge.Application.Commands.UpdateModule;
using Mentoory.Knowledge.Application.Commands.UpdateTopicPriorityRanges;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Domain.SeedWork;
using Moq;
using Xunit;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Tests.Handlers;

/// <summary>
/// Consolidated unit tests for the hierarchical CRUD handlers spanning the
/// Structure / Module / Topic / Subject / Resource levels of the
/// <see cref="KS"/> (project clone) aggregate.
/// </summary>
public class ProjectStructureCrudHandlerTests
{
    private readonly Mock<IKnowledgeStructureRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public ProjectStructureCrudHandlerTests()
    {
        _repositoryMock.Setup(r => r.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task UpdateKnowledgeStructure_UpdatesName()
    {
        var structure = FreshClone();
        _repositoryMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);

        var handler = new UpdateKnowledgeStructureHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new UpdateKnowledgeStructureCommand(structure.ExternalId, "Nombre Renombrado", "nueva desc"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        structure.Name.Should().Be("Nombre Renombrado");
        structure.Description.Should().Be("nueva desc");
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddModule_AddsLocallyOnlyModule()
    {
        var structure = FreshClone();
        SetupFullTree(structure);

        var handler = new AddModuleHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new AddModuleCommand(structure.ExternalId, "Módulo Local", "desc", 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var added = structure.Modules.Single(m => m.ExternalId == result.Value);
        added.Name.Should().Be("Módulo Local");
        added.SourceTemplateModuleExternalId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateModule_UpdatesName()
    {
        var structure = FreshClone();
        var moduleExternalId = structure.Modules.Single().ExternalId;
        SetupFullTree(structure);

        var handler = new UpdateModuleHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new UpdateModuleCommand(structure.ExternalId, moduleExternalId, "Módulo Renombrado", "nueva"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        structure.Modules.Single().Name.Should().Be("Módulo Renombrado");
        structure.Modules.Single().Description.Should().Be("nueva");
    }

    [Fact]
    public async Task DeleteModule_RemovesModule()
    {
        var structure = FreshClone();
        var moduleExternalId = structure.Modules.Single().ExternalId;
        SetupFullTree(structure);

        var handler = new DeleteModuleHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new DeleteModuleCommand(structure.ExternalId, moduleExternalId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        structure.Modules.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTopic_AddsLocallyOnlyTopic()
    {
        var structure = FreshClone();
        var moduleExternalId = structure.Modules.Single().ExternalId;
        SetupFullTree(structure);

        var handler = new AddTopicHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new AddTopicCommand(structure.ExternalId, moduleExternalId, "Tema Local", "desc", 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var addedTopic = structure.Modules.Single().Topics.Single(t => t.ExternalId == result.Value);
        addedTopic.Name.Should().Be("Tema Local");
        addedTopic.SourceTemplateTopicExternalId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTopicPriorityRanges_WithValidRanges_AppliesRanges()
    {
        var structure = FreshClone();
        var topicExternalId = structure.Modules.Single().Topics.Single().ExternalId;
        SetupFullTree(structure);

        var handler = new UpdateTopicPriorityRangesHandler(_repositoryMock.Object, Mock.Of<MediatR.IMediator>());

        var result = await handler.Handle(
            new UpdateTopicPriorityRangesCommand(
                structure.ExternalId,
                topicExternalId,
                High: new PriorityRangeInput(8m, 10m),
                Medium: new PriorityRangeInput(4m, 7m),
                Low: new PriorityRangeInput(0m, 3m)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var topic = structure.Modules.Single().Topics.Single();
        topic.HighRangeMin.Should().Be(8m);
        topic.HighRangeMax.Should().Be(10m);
        topic.MediumRangeMin.Should().Be(4m);
        topic.MediumRangeMax.Should().Be(7m);
        topic.LowRangeMin.Should().Be(0m);
        topic.LowRangeMax.Should().Be(3m);
    }

    [Fact]
    public async Task AddSubject_AddsLocallyOnlyInStructure()
    {
        var structure = FreshClone();
        var topicExternalId = structure.Modules.Single().Topics.Single().ExternalId;
        SetupFullTree(structure);

        var handler = new AddSubjectHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new AddSubjectCommand(structure.ExternalId, topicExternalId, "Materia Local", "desc", 3),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var addedSubject = structure.Modules
            .Single().Topics
            .Single().Subjects
            .Single(s => s.ExternalId == result.Value);
        addedSubject.Name.Should().Be("Materia Local");
    }

    [Fact]
    public async Task AddResource_WithValidUri_AddsResource()
    {
        var structure = FreshClone();
        var subjectExternalId = structure.Modules.Single().Topics.Single().Subjects.Single().ExternalId;
        SetupFullTree(structure);

        var handler = new AddResourceHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new AddResourceCommand(
                structure.ExternalId,
                subjectExternalId,
                "Recurso Local",
                "desc",
                "https://example.com/clip",
                ResourceType.Video,
                1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var addedResource = structure.Modules
            .Single().Topics
            .Single().Subjects
            .Single().Resources
            .Single(r => r.ExternalId == result.Value);
        addedResource.Url.Should().Be("https://example.com/clip");
        addedResource.ResourceType.Should().Be(ResourceType.Video);
    }

    [Fact]
    public async Task SetSyncMode_ToPartialSync_Succeeds_WhenSourceTemplateIdIsSet()
    {
        var structure = FreshClone();
        _repositoryMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);

        var handler = new SetSyncModeHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new SetSyncModeCommand(structure.ExternalId, SyncMode.PartialSync),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        structure.SyncMode.Should().Be(SyncMode.PartialSync);
    }

    [Fact]
    public async Task AddResource_WhenStructureNotFound_ReturnsFailure()
    {
        _repositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KS?)null);

        var handler = new AddResourceHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new AddResourceCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Recurso",
                null,
                "https://example.com/x",
                ResourceType.Link,
                1),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().Contain("no fue encontrada");
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static KS FreshClone()
    {
        var template = KnowledgeStructureTemplate.Create("T", "desc", DateTime.UtcNow);
        var tplModule = template.AddModule("Módulo 1", null, 1);
        var tplTopic = template.AddTopic(tplModule.ExternalId, "Tema 1", null, 1);
        template.AddSubject(tplTopic.ExternalId, "Materia 1", null, 1);

        return KS.CloneFromTemplate(template, projectId: 42, incubatorId: 7, DateTime.UtcNow);
    }

    private void SetupFullTree(KS structure)
    {
        _repositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);
    }
}
