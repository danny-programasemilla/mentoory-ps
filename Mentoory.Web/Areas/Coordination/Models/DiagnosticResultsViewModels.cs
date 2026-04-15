namespace Mentoory.Web.Areas.Coordination.Models;

public sealed class TimelineViewModel
{
    public long EntrepreneurUserId { get; set; }
    public List<TimelineEntryViewModel> Entries { get; set; } = new();
}

public sealed class TimelineEntryViewModel
{
    public Guid ResponseExternalId { get; set; }
    public Guid AssignmentExternalId { get; set; }
    public long ProjectStageId { get; set; }
    public string FormName { get; set; } = null!;
    public DateTime CompletedAtUtc { get; set; }
    public int ResponseCount { get; set; }
}

public sealed class CompareViewModel
{
    public ComparisonSideViewModel Earlier { get; set; } = null!;
    public ComparisonSideViewModel Later { get; set; } = null!;
    public List<TopicComparisonViewModel> TopicComparisons { get; set; } = new();
    public List<QuestionComparisonViewModel> SharedQuestions { get; set; } = new();
}

public sealed class ComparisonSideViewModel
{
    public Guid ResponseExternalId { get; set; }
    public string FormName { get; set; } = null!;
    public DateTime CompletedAtUtc { get; set; }
}

public sealed class TopicComparisonViewModel
{
    public long TopicId { get; set; }
    public decimal PreviousScore { get; set; }
    public decimal CurrentScore { get; set; }
    public decimal Delta { get; set; }
    public decimal PercentageChange { get; set; }
}

public sealed class QuestionComparisonViewModel
{
    public long QuestionId { get; set; }
    public string QuestionText { get; set; } = null!;
    public string? EarlierAnswer { get; set; }
    public string? LaterAnswer { get; set; }
    public decimal? EarlierNumeric { get; set; }
    public decimal? LaterNumeric { get; set; }
}
