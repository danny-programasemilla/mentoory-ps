using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;

public class FollowUpQuestion : Entity
{
    private FollowUpQuestion()
    {
    }

    public string QuestionText { get; private set; } = null!;
    public int SortOrder { get; private set; }

    internal static FollowUpQuestion Create(string questionText, int sortOrder)
    {
        return new FollowUpQuestion
        {
            QuestionText = questionText,
            SortOrder = sortOrder,
        };
    }
}
