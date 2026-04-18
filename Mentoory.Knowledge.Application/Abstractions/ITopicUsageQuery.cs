namespace Mentoory.Knowledge.Application.Abstractions;

/// <summary>
/// Cross-module read-side query asking external modules (e.g. Diagnostic) whether they
/// currently reference a given project-Topic Id. Used by the Knowledge module's
/// <c>DeleteTopic</c> handler (EC-30) to block deletion of Topics still referenced by
/// diagnostic Questions.
/// </summary>
public interface ITopicUsageQuery
{
    /// <summary>
    /// Returns the number of Diagnostic Questions whose TopicId equals <paramref name="topicId"/>.
    /// </summary>
    Task<int> CountQuestionsReferencingTopicAsync(long topicId, CancellationToken cancellationToken);
}
