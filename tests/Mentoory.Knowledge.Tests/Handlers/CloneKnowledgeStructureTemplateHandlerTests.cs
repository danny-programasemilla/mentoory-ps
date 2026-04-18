using FluentAssertions;
using Mentoory.Knowledge.Application.Commands.CloneKnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Tests.Handlers;

/// <summary>
/// Unit tests for <see cref="CloneKnowledgeStructureTemplateHandler"/>, the US2 entry point
/// that deep-copies a <see cref="KnowledgeStructureTemplate"/> into a project-scoped
/// <see cref="KS"/> clone.
/// </summary>
public class CloneKnowledgeStructureTemplateHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IKnowledgeStructureTemplateRepository> _templateRepositoryMock = new();
    private readonly Mock<IKnowledgeStructureRepository> _structureRepositoryMock = new();
    private readonly Mock<ITimeProvider> _timeProviderMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public CloneKnowledgeStructureTemplateHandlerTests()
    {
        _timeProviderMock.Setup(t => t.UtcNow).Returns(UtcNow);
        _structureRepositoryMock.Setup(r => r.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Clone_WithValidTemplate_CreatesStructureAndReturnsExternalId()
    {
        var template = FreshPopulatedTemplate();
        _templateRepositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        KS? captured = null;
        _structureRepositoryMock
            .Setup(r => r.Add(It.IsAny<KS>()))
            .Callback<KS>(s => captured = s)
            .Returns<KS>(s => s);

        var handler = BuildHandler();

        var result = await handler.Handle(
            new CloneKnowledgeStructureTemplateCommand(template.ExternalId, ProjectId: 42, IncubatorId: 7),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        captured.Should().NotBeNull();
        captured!.ExternalId.Should().Be(result.Value);
        captured.Modules.Should().HaveCount(1);

        var module = captured.Modules.Single();
        module.Topics.Should().HaveCount(1);

        var topic = module.Topics.Single();
        topic.Subjects.Should().HaveCount(1);

        var subject = topic.Subjects.Single();
        subject.Resources.Should().HaveCount(1);

        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Clone_WithMissingTemplate_ReturnsFailure()
    {
        _templateRepositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeStructureTemplate?)null);

        var handler = BuildHandler();

        var result = await handler.Handle(
            new CloneKnowledgeStructureTemplateCommand(Guid.NewGuid(), ProjectId: 42, IncubatorId: 7),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().Contain("no fue encontrada");
        _structureRepositoryMock.Verify(r => r.Add(It.IsAny<KS>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Clone_WithArchivedTemplate_ReturnsFailure()
    {
        var template = FreshPopulatedTemplate();
        template.Archive();

        _templateRepositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var handler = BuildHandler();

        var result = await handler.Handle(
            new CloneKnowledgeStructureTemplateCommand(template.ExternalId, ProjectId: 42, IncubatorId: 7),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().Contain("archivada");
        _structureRepositoryMock.Verify(r => r.Add(It.IsAny<KS>()), Times.Never);
    }

    [Fact]
    public async Task Clone_StampsProjectIdAndIncubatorId()
    {
        var template = FreshPopulatedTemplate();
        _templateRepositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        KS? captured = null;
        _structureRepositoryMock
            .Setup(r => r.Add(It.IsAny<KS>()))
            .Callback<KS>(s => captured = s)
            .Returns<KS>(s => s);

        var handler = BuildHandler();

        var result = await handler.Handle(
            new CloneKnowledgeStructureTemplateCommand(template.ExternalId, ProjectId: 101, IncubatorId: 55),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.ProjectId.Should().Be(101);
        captured.IncubatorId.Should().Be(55);
    }

    [Fact]
    public async Task Clone_SetsCreatedAtUtcFromTimeProvider()
    {
        var template = FreshPopulatedTemplate();
        _templateRepositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        KS? captured = null;
        _structureRepositoryMock
            .Setup(r => r.Add(It.IsAny<KS>()))
            .Callback<KS>(s => captured = s)
            .Returns<KS>(s => s);

        var handler = BuildHandler();

        await handler.Handle(
            new CloneKnowledgeStructureTemplateCommand(template.ExternalId, ProjectId: 1, IncubatorId: 1),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.CreatedAtUtc.Should().Be(UtcNow);
    }

    [Fact]
    public async Task Clone_DefaultsToDisconnectedSyncMode()
    {
        var template = FreshPopulatedTemplate();
        _templateRepositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        KS? captured = null;
        _structureRepositoryMock
            .Setup(r => r.Add(It.IsAny<KS>()))
            .Callback<KS>(s => captured = s)
            .Returns<KS>(s => s);

        var handler = BuildHandler();

        await handler.Handle(
            new CloneKnowledgeStructureTemplateCommand(template.ExternalId, ProjectId: 1, IncubatorId: 1),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.SyncMode.Should().Be(SyncMode.Disconnected);
    }

    private static KnowledgeStructureTemplate FreshPopulatedTemplate()
    {
        var t = KnowledgeStructureTemplate.Create(
            "Test",
            "desc",
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var m = t.AddModule("Mod", null, 1);
        var topic = t.AddTopic(m.ExternalId, "Top", null, 1);
        var subject = t.AddSubject(topic.ExternalId, "Sub", null, 1);
        t.AddResource(subject.ExternalId, "Res", null, "https://example.com/a", ResourceType.Link, 1);
        return t;
    }

    private CloneKnowledgeStructureTemplateHandler BuildHandler() =>
        new(
            _templateRepositoryMock.Object,
            _structureRepositoryMock.Object,
            _timeProviderMock.Object,
            Mock.Of<ILogger<CloneKnowledgeStructureTemplateHandler>>());
}
