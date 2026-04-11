using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Queries.GetProjectForm;

/// <summary>
/// Handles the query to retrieve a project form by its external identifier.
/// </summary>
public class GetProjectFormHandler : BaseCommandHandler<GetProjectFormQuery, ProjectFormDto?>
{
    private readonly IProjectFormRepository _projectFormRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetProjectFormHandler"/> class.
    /// </summary>
    /// <param name="projectFormRepository">The project form repository for data access.</param>
    public GetProjectFormHandler(IProjectFormRepository projectFormRepository)
    {
        _projectFormRepository = projectFormRepository;
    }

    /// <inheritdoc />
    public override async Task<Result<ProjectFormDto?>> Handle(GetProjectFormQuery request, CancellationToken cancellationToken)
    {
        var form = request.ProjectId.HasValue
            ? await _projectFormRepository.GetByExternalIdWithQuestionsAsync(
                request.ExternalId, request.ProjectId.Value, cancellationToken)
            : await _projectFormRepository.GetByExternalIdWithQuestionsAsync(
                request.ExternalId, cancellationToken);

        if (form is null)
        {
            return Success((ProjectFormDto?)null);
        }

        var dto = new ProjectFormDto(
            form.ExternalId,
            form.Name,
            form.ProjectId,
            form.IncubatorId,
            form.SyncMode,
            form.CreatedAtUtc,
            form.Questions
                .OrderBy(q => q.SortOrder)
                .Select(q => new QuestionDto(
                    q.Id,
                    q.ExternalId,
                    q.TopicId,
                    q.QuestionText,
                    q.QuestionType,
                    q.StageApplicability,
                    q.SortOrder,
                    q.BlockGroup,
                    q.IsOptional,
                    q.AnswerOptions
                        .OrderBy(ao => ao.SortOrder)
                        .Select(ao => new AnswerOptionDto(
                            ao.Id,
                            ao.OptionText,
                            ao.Score,
                            ao.SwotClassification,
                            ao.OdsrOrientation,
                            ao.SortOrder))
                        .ToList(),
                    q.FollowUpQuestions
                        .OrderBy(fq => fq.SortOrder)
                        .Select(fq => new FollowUpQuestionDto(
                            fq.Id,
                            fq.QuestionText,
                            fq.SortOrder))
                        .ToList()))
                .ToList());

        return Success((ProjectFormDto?)dto);
    }
}
