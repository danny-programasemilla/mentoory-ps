using FluentAssertions;
using MediatR;
using Mentoory.Knowledge.Application.Commands.UpdateTopicPriorityRanges;
using Mentoory.Knowledge.Application.IntegrationEvents;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Domain.SeedWork;
using Moq;
using Xunit;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Tests.Handlers;

/// <summary>
/// Unit tests for US4: verifies that <see cref="UpdateTopicPriorityRangesHandler"/>
/// publishes the <see cref="TopicPriorityRangesChanged"/> integration event on successful
/// clone-side priority-range updates, and NEVER publishes on failure paths.
/// </summary>
public class UpdateTopicPriorityRangesEventTests
{
    private readonly Mock<IKnowledgeStructureRepository> _repositoryMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public UpdateTopicPriorityRangesEventTests()
    {
        _repositoryMock.Setup(r => r.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task PublishesEvent_OnSuccessfulRangeUpdate()
    {
        var structure = BuildCloneWithOneTopic(out var structureExternalId, out var topicExternalId);
        _repositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);

        TopicPriorityRangesChanged? captured = null;
        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<TopicPriorityRangesChanged>(), It.IsAny<CancellationToken>()))
            .Callback<TopicPriorityRangesChanged, CancellationToken>((evt, _) => captured = evt)
            .Returns(Task.CompletedTask);

        var handler = new UpdateTopicPriorityRangesHandler(_repositoryMock.Object, _mediatorMock.Object);

        var result = await handler.Handle(
            new UpdateTopicPriorityRangesCommand(
                structureExternalId,
                topicExternalId,
                new PriorityRangeInput(8m, 10m),
                null,
                null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<TopicPriorityRangesChanged>(), It.IsAny<CancellationToken>()),
            Times.Once);

        captured.Should().NotBeNull();
        captured!.TopicExternalId.Should().Be(topicExternalId);
        captured.ProjectId.Should().Be(structure.ProjectId);
        captured.HighRange.Should().NotBeNull();
        captured.HighRange!.Min.Should().Be(8m);
        captured.HighRange.Max.Should().Be(10m);
    }

    [Fact]
    public async Task DoesNotPublishEvent_WhenValidationFails()
    {
        var structure = BuildCloneWithOneTopic(out var structureExternalId, out var topicExternalId);
        _repositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);

        var handler = new UpdateTopicPriorityRangesHandler(_repositoryMock.Object, _mediatorMock.Object);

        // Overlapping high/medium ranges -> failure expected.
        var result = await handler.Handle(
            new UpdateTopicPriorityRangesCommand(
                structureExternalId,
                topicExternalId,
                new PriorityRangeInput(5m, 10m),
                new PriorityRangeInput(8m, 12m),
                null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<TopicPriorityRangesChanged>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DoesNotPublishEvent_WhenStructureNotFound()
    {
        _repositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KS?)null);

        var handler = new UpdateTopicPriorityRangesHandler(_repositoryMock.Object, _mediatorMock.Object);

        var result = await handler.Handle(
            new UpdateTopicPriorityRangesCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new PriorityRangeInput(8m, 10m),
                null,
                null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<TopicPriorityRangesChanged>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static KS BuildCloneWithOneTopic(out Guid structureExternalId, out Guid topicExternalId)
    {
        var template = KnowledgeStructureTemplate.Create("T", null, DateTime.UtcNow);
        var tplModule = template.AddModule("M", null, 1);
        template.AddTopic(tplModule.ExternalId, "Top", null, 1);

        var structure = KS.CloneFromTemplate(template, projectId: 42, incubatorId: 7, DateTime.UtcNow);
        structureExternalId = structure.ExternalId;
        topicExternalId = structure.Modules.Single().Topics.Single().ExternalId;
        return structure;
    }
}
