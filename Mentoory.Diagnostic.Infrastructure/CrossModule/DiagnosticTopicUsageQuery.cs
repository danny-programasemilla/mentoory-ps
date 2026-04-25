using Mentoory.Diagnostic.Infrastructure.Persistence;
using Mentoory.Knowledge.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Diagnostic.Infrastructure.CrossModule;

/// <summary>
/// Diagnostic-side implementation of <see cref="ITopicUsageQuery"/>. Counts rows in
/// <c>diagnostic.Questions</c> where <c>TopicId</c> matches the caller-supplied value.
/// </summary>
public class DiagnosticTopicUsageQuery : ITopicUsageQuery
{
    private readonly DiagnosticDbContext _dbContext;

    public DiagnosticTopicUsageQuery(DiagnosticDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> CountQuestionsReferencingTopicAsync(long topicId, CancellationToken cancellationToken)
    {
        return _dbContext.Questions
            .AsNoTracking()
            .CountAsync(q => q.TopicId == topicId, cancellationToken);
    }
}
