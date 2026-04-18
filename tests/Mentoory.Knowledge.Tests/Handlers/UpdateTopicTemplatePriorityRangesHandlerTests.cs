using FluentAssertions;
using MediatR;
using Mentoory.Knowledge.Application.Commands.UpdateTopicTemplatePriorityRanges;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Domain.SeedWork;
using Moq;
using Xunit;

namespace Mentoory.Knowledge.Tests.Handlers;

/// <summary>
/// Unit tests for <see cref="UpdateTopicTemplatePriorityRangesHandler"/> covering
/// the high/medium/low priority band configuration on a topic.
/// </summary>
public class UpdateTopicTemplatePriorityRangesHandlerTests
{
    private readonly Mock<IKnowledgeStructureTemplateRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public UpdateTopicTemplatePriorityRangesHandlerTests()
    {
        _repositoryMock.Setup(r => r.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task WithValidRanges_SetsAllThreeBands()
    {
        var template = TemplateWithTopic(out _, out var topicExternalId);
        SetupFullTree(template);
        var handler = new UpdateTopicTemplatePriorityRangesHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new UpdateTopicTemplatePriorityRangesCommand(
                template.ExternalId,
                topicExternalId,
                new PriorityRangeInput(8m, 10m),
                new PriorityRangeInput(5m, 7.99m),
                new PriorityRangeInput(0m, 4.99m)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var topic = template.Modules.Single().Topics.Single();
        topic.HighRangeMin.Should().Be(8m);
        topic.HighRangeMax.Should().Be(10m);
        topic.MediumRangeMin.Should().Be(5m);
        topic.MediumRangeMax.Should().Be(7.99m);
        topic.LowRangeMin.Should().Be(0m);
        topic.LowRangeMax.Should().Be(4.99m);
    }

    [Fact]
    public async Task WithOverlappingRanges_ReturnsFailure()
    {
        var template = TemplateWithTopic(out _, out var topicExternalId);
        SetupFullTree(template);
        var handler = new UpdateTopicTemplatePriorityRangesHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new UpdateTopicTemplatePriorityRangesCommand(
                template.ExternalId,
                topicExternalId,
                new PriorityRangeInput(5m, 10m),
                new PriorityRangeInput(8m, 12m),
                null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().NotBeNullOrWhiteSpace();

        var topic = template.Modules.Single().Topics.Single();
        topic.HighRangeMin.Should().BeNull();
        topic.HighRangeMax.Should().BeNull();
        topic.MediumRangeMin.Should().BeNull();
        topic.MediumRangeMax.Should().BeNull();
    }

    [Fact]
    public async Task WithMinGreaterThanMax_ReturnsFailure()
    {
        var template = TemplateWithTopic(out _, out var topicExternalId);
        SetupFullTree(template);
        var handler = new UpdateTopicTemplatePriorityRangesHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new UpdateTopicTemplatePriorityRangesCommand(
                template.ExternalId,
                topicExternalId,
                new PriorityRangeInput(10m, 5m),
                null,
                null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task WithArbitraryDecimalScale_Succeeds()
    {
        var template = TemplateWithTopic(out _, out var topicExternalId);
        SetupFullTree(template);
        var handler = new UpdateTopicTemplatePriorityRangesHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new UpdateTopicTemplatePriorityRangesCommand(
                template.ExternalId,
                topicExternalId,
                new PriorityRangeInput(7.50m, 8.00m),
                null,
                null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var topic = template.Modules.Single().Topics.Single();
        topic.HighRangeMin.Should().Be(7.50m);
        topic.HighRangeMax.Should().Be(8.00m);
    }

    [Fact]
    public async Task WithAllRangesNull_Succeeds()
    {
        var template = TemplateWithTopic(out _, out var topicExternalId);
        SetupFullTree(template);
        var handler = new UpdateTopicTemplatePriorityRangesHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new UpdateTopicTemplatePriorityRangesCommand(
                template.ExternalId,
                topicExternalId,
                null,
                null,
                null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var topic = template.Modules.Single().Topics.Single();
        topic.HighRangeMin.Should().BeNull();
        topic.MediumRangeMin.Should().BeNull();
        topic.LowRangeMin.Should().BeNull();
    }

    [Fact]
    public async Task HandleFailsWhenTemplateNotFound_ReturnsFailureWithSpanishMessage()
    {
        _repositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeStructureTemplate?)null);

        var handler = new UpdateTopicTemplatePriorityRangesHandler(_repositoryMock.Object);

        var result = await handler.Handle(
            new UpdateTopicTemplatePriorityRangesCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new PriorityRangeInput(0m, 10m),
                null,
                null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().Contain("no fue encontrada");
    }

    /// <summary>
    /// Asserts-by-construction that template-side priority-range edits never publish
    /// <c>TopicPriorityRangesChanged</c>: the handler must not depend on <see cref="IMediator"/>.
    /// Only clone-side edits are allowed to emit the integration event.
    /// </summary>
    [Fact]
    public void DoesNotPublishEvent_WhenTemplatePriorityRangesUpdated()
    {
        var handlerType = typeof(UpdateTopicTemplatePriorityRangesHandler);
        var mediatorFields = handlerType
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Where(f => f.FieldType.Name.Contains("IMediator"));
        mediatorFields.Should().BeEmpty("template priority-range edits must not publish TopicPriorityRangesChanged; only clone-side edits publish");
    }

    private static KnowledgeStructureTemplate FreshTemplate() =>
        KnowledgeStructureTemplate.Create("Test Template", "desc", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    private static KnowledgeStructureTemplate TemplateWithTopic(out Guid moduleExternalId, out Guid topicExternalId)
    {
        var t = FreshTemplate();
        var module = t.AddModule("Módulo 1", null, 1);
        moduleExternalId = module.ExternalId;
        var topic = t.AddTopic(moduleExternalId, "Tema 1", null, 1);
        topicExternalId = topic.ExternalId;
        return t;
    }

    private void SetupFullTree(KnowledgeStructureTemplate template)
    {
        _repositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
    }
}
