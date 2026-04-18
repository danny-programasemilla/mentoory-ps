using FluentAssertions;
using Mentoory.Knowledge.Application.Abstractions;
using Mentoory.Knowledge.Application.Commands.DeleteTopic;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Domain.SeedWork;
using Moq;
using Xunit;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Tests.Handlers;

/// <summary>
/// Unit tests for <see cref="DeleteTopicHandler"/>, enforcing EC-30: deletion is blocked
/// whenever the target Topic is referenced by diagnostic Questions.
/// </summary>
public class DeleteTopicHandlerTests
{
    private readonly Mock<IKnowledgeStructureRepository> _repositoryMock = new();
    private readonly Mock<ITopicUsageQuery> _topicUsageQueryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public DeleteTopicHandlerTests()
    {
        _repositoryMock.Setup(r => r.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Delete_WhenTopicHasZeroReferencingQuestions_Succeeds()
    {
        var structure = BuildStructureWithOneTopic(out var structureExternalId, out var topicExternalId);
        _repositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);
        _topicUsageQueryMock
            .Setup(q => q.CountQuestionsReferencingTopicAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = new DeleteTopicHandler(_repositoryMock.Object, _topicUsageQueryMock.Object);

        var result = await handler.Handle(
            new DeleteTopicCommand(structureExternalId, topicExternalId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        structure.Modules.Single().Topics.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenTopicHasReferencingQuestions_ReturnsFailure()
    {
        var structure = BuildStructureWithOneTopic(out var structureExternalId, out var topicExternalId);
        _repositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);
        _topicUsageQueryMock
            .Setup(q => q.CountQuestionsReferencingTopicAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var handler = new DeleteTopicHandler(_repositoryMock.Object, _topicUsageQueryMock.Object);

        var result = await handler.Handle(
            new DeleteTopicCommand(structureExternalId, topicExternalId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().ContainAny("referenciado", "pregunta");
        structure.Modules.Single().Topics.Should().HaveCount(1);
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WhenStructureNotFound_ReturnsFailure()
    {
        _repositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KS?)null);

        var handler = new DeleteTopicHandler(_repositoryMock.Object, _topicUsageQueryMock.Object);

        var result = await handler.Handle(
            new DeleteTopicCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().Contain("no fue encontrada");
        _topicUsageQueryMock.Verify(
            q => q.CountQuestionsReferencingTopicAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Delete_WhenTopicNotFound_ReturnsFailure()
    {
        var structure = BuildStructureWithOneTopic(out var structureExternalId, out _);
        _repositoryMock
            .Setup(r => r.GetByExternalIdWithFullTreeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(structure);

        var handler = new DeleteTopicHandler(_repositoryMock.Object, _topicUsageQueryMock.Object);

        var result = await handler.Handle(
            new DeleteTopicCommand(structureExternalId, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessages.Should().NotBeNull();
        result.ErrorMessages![0].Message.Should().Contain("no fue encontrado");
        _topicUsageQueryMock.Verify(
            q => q.CountQuestionsReferencingTopicAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static KS BuildStructureWithOneTopic(out Guid structureExternalId, out Guid topicExternalId)
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
