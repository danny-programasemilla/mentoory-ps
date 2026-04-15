using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Aggregates.StageFormAssignment;

public class AssignedQuestion : Entity
{
    private AssignedQuestion()
    {
    }

    public long QuestionId { get; private set; }
    public int SortOrder { get; private set; }

    internal static AssignedQuestion Create(long questionId, int sortOrder)
    {
        return new AssignedQuestion
        {
            QuestionId = questionId,
            SortOrder = sortOrder,
        };
    }
}
