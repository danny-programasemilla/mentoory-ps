using System.Reflection;
using FluentAssertions;
using Mentoory.Knowledge.Application.Commands.SyncFromTemplate;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Domain.SeedWork;
using Moq;
using Xunit;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Tests.Handlers;

/// <summary>
/// Unit tests for <see cref="SyncFromTemplateHandler"/>, the US5 entry point that partially
/// syncs a project clone with its source template.
/// </summary>
public class SyncFromTemplateHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IKnowledgeStructureRepository> _structureRepositoryMock = new();
    private readonly Mock<IKnowledgeStructureTemplateRepository> _templateRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public SyncFromTemplateHandlerTests()
    {
        _structureRepositoryMock.Setup(r => r.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Sync_Success_ReturnsDtoWithCounts()
    {
        var template = BuildTemplate(templateId: 500);
        var structure = KS.CloneFromTemplate(template, projectId: 1, incubatorId: 1, UtcNow);
        structure.SetSyncMode(SyncMode.PartialSync);

        // Template adds a new topic — clone is now behind.
        var moduleExternalId = template.Modules.Single().ExternalId;
        template.AddTopic(moduleExternalId, "Tema Nuevo", null, 2);

        _structureRepositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);
        _templateRepositoryMock
            .Setup(r => r.GetByIdWithFullTreeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var handler = BuildHandler();

        var result = await handler.Handle(
            new SyncFromTemplateCommand(structure.ExternalId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TopicsAdded.Should().Be(1);
        result.Value.ModulesAdded.Should().Be(0);
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sync_WhenStructureNotFound_ReturnsFailure()
    {
        _structureRepositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KS?)null);

        var handler = BuildHandler();

        var result = await handler.Handle(
            new SyncFromTemplateCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().Contain("no fue encontrada");
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Sync_WhenSourceTemplateIdIsNull_ReturnsFailure()
    {
        // Build a clone and then null out its SourceTemplateId via reflection (only way in v1).
        var template = BuildTemplate(templateId: 501);
        var structure = KS.CloneFromTemplate(template, projectId: 1, incubatorId: 1, UtcNow);
        SetSourceTemplateIdToNull(structure);

        _structureRepositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);

        var handler = BuildHandler();

        var result = await handler.Handle(
            new SyncFromTemplateCommand(structure.ExternalId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().Contain("plantilla de origen");
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Sync_WhenTemplateNotFound_ReturnsFailure()
    {
        var template = BuildTemplate(templateId: 502);
        var structure = KS.CloneFromTemplate(template, projectId: 1, incubatorId: 1, UtcNow);
        structure.SetSyncMode(SyncMode.PartialSync);

        _structureRepositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);
        _templateRepositoryMock
            .Setup(r => r.GetByIdWithFullTreeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeStructureTemplate?)null);

        var handler = BuildHandler();

        var result = await handler.Handle(
            new SyncFromTemplateCommand(structure.ExternalId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().Contain("no fue encontrada");
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Sync_WhenInDisconnectedMode_ReturnsFailure()
    {
        var template = BuildTemplate(templateId: 503);
        var structure = KS.CloneFromTemplate(template, projectId: 1, incubatorId: 1, UtcNow);

        // Leave SyncMode = Disconnected (the default).
        _structureRepositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);
        _templateRepositoryMock
            .Setup(r => r.GetByIdWithFullTreeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var handler = BuildHandler();

        var result = await handler.Handle(
            new SyncFromTemplateCommand(structure.ExternalId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().Contain("sincronización parcial");
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
    private static KnowledgeStructureTemplate BuildTemplate(long templateId)
    {
        var template = KnowledgeStructureTemplate.Create("Plantilla", "desc", UtcNow);
        var module = template.AddModule("Módulo 1", null, 1);
        var topic = template.AddTopic(module.ExternalId, "Tema 1", null, 1);
        var subject = template.AddSubject(topic.ExternalId, "Materia 1", null, 1);
        template.AddResource(
            subject.ExternalId,
            "Recurso 1",
            null,
            "https://example.com/r",
            ResourceType.Link,
            1);

        SetEntityId(template, templateId);
        return template;
    }

    private static void SetEntityId(Entity entity, long id)
    {
        var prop = typeof(Entity).GetProperty(
            nameof(Entity.Id),
            BindingFlags.Instance | BindingFlags.Public);
        prop!.SetValue(entity, id);
    }

    /// <summary>
    /// Forces <see cref="KS.SourceTemplateId"/> to null via reflection so we can exercise
    /// the guard clause that rejects sync when the clone has no source template.
    /// </summary>
    private static void SetSourceTemplateIdToNull(KS structure)
    {
        var prop = typeof(KS).GetProperty(
            nameof(KS.SourceTemplateId),
            BindingFlags.Instance | BindingFlags.Public);
        prop!.SetValue(structure, null);
    }

    private SyncFromTemplateHandler BuildHandler() =>
        new(_structureRepositoryMock.Object, _templateRepositoryMock.Object);
}
