using FluentAssertions;
using Mentoory.Knowledge.Application.Commands.AddModuleTemplate;
using Mentoory.Knowledge.Application.Commands.AddResourceTemplate;
using Mentoory.Knowledge.Application.Commands.AddSubjectTemplate;
using Mentoory.Knowledge.Application.Commands.AddTopicTemplate;
using Mentoory.Knowledge.Application.Commands.DeleteModuleTemplate;
using Mentoory.Knowledge.Application.Commands.DeleteTopicTemplate;
using Mentoory.Knowledge.Application.Commands.ReorderModuleTemplates;
using Mentoory.Knowledge.Application.Commands.ReorderResourceTemplates;
using Mentoory.Knowledge.Application.Commands.UpdateModuleTemplate;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Domain.SeedWork;
using Moq;
using Xunit;

namespace Mentoory.Knowledge.Tests.Handlers;

/// <summary>
/// Consolidated unit tests for the hierarchical CRUD handlers spanning the
/// Module / Topic / Subject / Resource levels of the
/// <see cref="KnowledgeStructureTemplate"/> aggregate.
/// </summary>
public class ModuleTemplateCrudHandlerTests
{
    private readonly Mock<IKnowledgeStructureTemplateRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public ModuleTemplateCrudHandlerTests()
    {
        _repositoryMock.Setup(r => r.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task AddModuleTemplate_Success_AddsModuleAndReturnsGuid()
    {
        var template = FreshTemplate();
        SetupFullTree(template);

        var handler = new AddModuleTemplateHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new AddModuleTemplateCommand(template.ExternalId, "Módulo 1", "desc", 1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        template.Modules.Should().HaveCount(1);
        template.Modules.Single().Name.Should().Be("Módulo 1");
    }

    [Fact]
    public async Task UpdateModuleTemplate_Success_UpdatesName()
    {
        var template = TemplateWithOneModule(out var moduleExternalId);
        SetupFullTree(template);

        var handler = new UpdateModuleTemplateHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new UpdateModuleTemplateCommand(template.ExternalId, moduleExternalId, "Módulo Renombrado", "nueva"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        template.Modules.Single().Name.Should().Be("Módulo Renombrado");
        template.Modules.Single().Description.Should().Be("nueva");
    }

    [Fact]
    public async Task DeleteModuleTemplate_Success_RemovesModule()
    {
        var template = TemplateWithOneModule(out var moduleExternalId);
        SetupFullTree(template);

        var handler = new DeleteModuleTemplateHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new DeleteModuleTemplateCommand(template.ExternalId, moduleExternalId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        template.Modules.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReorderModuleTemplates_Success_SetsSortOrders()
    {
        var template = FreshTemplate();
        var m1 = template.AddModule("Módulo 1", null, 1);
        var m2 = template.AddModule("Módulo 2", null, 2);
        SetupFullTree(template);

        var handler = new ReorderModuleTemplatesHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new ReorderModuleTemplatesCommand(template.ExternalId, new[] { m2.ExternalId, m1.ExternalId }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        template.Modules.First(m => m.ExternalId == m2.ExternalId).SortOrder.Should().Be(1);
        template.Modules.First(m => m.ExternalId == m1.ExternalId).SortOrder.Should().Be(2);
    }

    [Fact]
    public async Task AddTopicTemplate_Success_AddsTopicToModule()
    {
        var template = TemplateWithOneModule(out var moduleExternalId);
        SetupFullTree(template);

        var handler = new AddTopicTemplateHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new AddTopicTemplateCommand(template.ExternalId, moduleExternalId, "Tema 1", "desc", 1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        var module = template.Modules.Single();
        module.Topics.Should().HaveCount(1);
        module.Topics.Single().Name.Should().Be("Tema 1");
    }

    [Fact]
    public async Task DeleteTopicTemplate_Success_RemovesTopic()
    {
        var template = TemplateWithOneTopic(out _, out var topicExternalId);
        SetupFullTree(template);

        var handler = new DeleteTopicTemplateHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new DeleteTopicTemplateCommand(template.ExternalId, topicExternalId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        template.Modules.Single().Topics.Should().BeEmpty();
    }

    [Fact]
    public async Task AddSubjectTemplate_Success_AddsSubjectToTopic()
    {
        var template = TemplateWithOneTopic(out _, out var topicExternalId);
        SetupFullTree(template);

        var handler = new AddSubjectTemplateHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new AddSubjectTemplateCommand(template.ExternalId, topicExternalId, "Materia 1", "desc", 1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        var topic = template.Modules.Single().Topics.Single();
        topic.Subjects.Should().HaveCount(1);
        topic.Subjects.Single().Name.Should().Be("Materia 1");
    }

    [Fact]
    public async Task AddResourceTemplate_Success_AddsResourceToSubject()
    {
        var template = TemplateWithOneSubject(out _, out _, out var subjectExternalId);
        SetupFullTree(template);

        var handler = new AddResourceTemplateHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new AddResourceTemplateCommand(
                template.ExternalId,
                subjectExternalId,
                "Recurso 1",
                "desc",
                "https://example.com/video",
                ResourceType.Video,
                1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        var subject = template.Modules.Single().Topics.Single().Subjects.Single();
        subject.Resources.Should().HaveCount(1);
        subject.Resources.Single().Url.Should().Be("https://example.com/video");
        subject.Resources.Single().ResourceType.Should().Be(ResourceType.Video);
    }

    [Fact]
    public async Task AddResourceTemplate_WhenTemplateNotFound_ReturnsFailure()
    {
        _repositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeStructureTemplate?)null);

        var handler = new AddResourceTemplateHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new AddResourceTemplateCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Recurso 1",
                null,
                "https://example.com/link",
                ResourceType.Link,
                1),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().Contain("no fue encontrada");
    }

    [Fact]
    public async Task ReorderResourceTemplates_Success_SetsSortOrders()
    {
        var template = TemplateWithOneSubject(out _, out _, out var subjectExternalId);
        var subject = template.Modules.Single().Topics.Single().Subjects.Single();
        var r1 = template.AddResource(subjectExternalId, "R1", null, "https://a", ResourceType.Link, 1);
        var r2 = template.AddResource(subjectExternalId, "R2", null, "https://b", ResourceType.Link, 2);
        SetupFullTree(template);

        var handler = new ReorderResourceTemplatesHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new ReorderResourceTemplatesCommand(
                template.ExternalId,
                subjectExternalId,
                new[] { r2.ExternalId, r1.ExternalId }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        subject.Resources.First(r => r.ExternalId == r2.ExternalId).SortOrder.Should().Be(1);
        subject.Resources.First(r => r.ExternalId == r1.ExternalId).SortOrder.Should().Be(2);
    }

    private static KnowledgeStructureTemplate FreshTemplate() =>
        KnowledgeStructureTemplate.Create("Test Template", "desc", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    private static KnowledgeStructureTemplate TemplateWithOneModule(out Guid moduleExternalId)
    {
        var t = FreshTemplate();
        var m = t.AddModule("Módulo 1", null, 1);
        moduleExternalId = m.ExternalId;
        return t;
    }

    private static KnowledgeStructureTemplate TemplateWithOneTopic(out Guid moduleExternalId, out Guid topicExternalId)
    {
        var t = TemplateWithOneModule(out moduleExternalId);
        var topic = t.AddTopic(moduleExternalId, "Tema 1", null, 1);
        topicExternalId = topic.ExternalId;
        return t;
    }

    private static KnowledgeStructureTemplate TemplateWithOneSubject(
        out Guid moduleExternalId,
        out Guid topicExternalId,
        out Guid subjectExternalId)
    {
        var t = TemplateWithOneTopic(out moduleExternalId, out topicExternalId);
        var subject = t.AddSubject(topicExternalId, "Materia 1", null, 1);
        subjectExternalId = subject.ExternalId;
        return t;
    }

    private void SetupFullTree(KnowledgeStructureTemplate template)
    {
        _repositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
    }
}
