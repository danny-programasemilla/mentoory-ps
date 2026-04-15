using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Aggregates.StageFormAssignment;

public class StageFormAssignment : Entity, IAggregateRoot
{
    private readonly List<AssignedQuestion> _assignedQuestions = new();

    private StageFormAssignment()
    {
    }

    public Guid ExternalId { get; private set; }
    public long ProjectId { get; private set; }
    public long IncubatorId { get; private set; }
    public long ProjectStageId { get; private set; }
    public long ProjectFormId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<AssignedQuestion> AssignedQuestions => _assignedQuestions.AsReadOnly();

    public static StageFormAssignment Create(
        long projectId,
        long incubatorId,
        long projectStageId,
        long projectFormId,
        IReadOnlyList<long> selectedQuestionIds,
        DateTime utcNow)
    {
        if (selectedQuestionIds is null || selectedQuestionIds.Count == 0)
        {
            throw new ArgumentException("At least one question must be selected.", nameof(selectedQuestionIds));
        }

        var assignment = new StageFormAssignment
        {
            ExternalId = Guid.NewGuid(),
            ProjectId = projectId,
            IncubatorId = incubatorId,
            ProjectStageId = projectStageId,
            ProjectFormId = projectFormId,
            IsActive = true,
            CreatedAtUtc = utcNow,
        };

        for (var i = 0; i < selectedQuestionIds.Count; i++)
        {
            assignment._assignedQuestions.Add(AssignedQuestion.Create(selectedQuestionIds[i], i));
        }

        return assignment;
    }

    public void UpdateQuestionSelection(IReadOnlyList<long> questionIds)
    {
        if (questionIds is null || questionIds.Count == 0)
        {
            throw new ArgumentException("At least one question must be selected.", nameof(questionIds));
        }

        if (questionIds.Distinct().Count() != questionIds.Count)
        {
            throw new ArgumentException("Duplicate question IDs are not allowed.", nameof(questionIds));
        }

        _assignedQuestions.Clear();

        for (var i = 0; i < questionIds.Count; i++)
        {
            _assignedQuestions.Add(AssignedQuestion.Create(questionIds[i], i));
        }
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
