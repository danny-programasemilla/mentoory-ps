using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.ReorderSubjectTemplates;

/// <summary>
/// Handles the reordering of subjects within a topic of a knowledge structure template.
/// </summary>
public class ReorderSubjectTemplatesHandler : BaseCommandHandler<ReorderSubjectTemplatesCommand>
{
    private readonly IKnowledgeStructureTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReorderSubjectTemplatesHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure template repository.</param>
    public ReorderSubjectTemplatesHandler(IKnowledgeStructureTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(
        ReorderSubjectTemplatesCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetByExternalIdWithFullTreeAsync(request.TemplateExternalId, cancellationToken);
        if (template is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Plantilla", "La plantilla no fue encontrada."));
        }

        try
        {
            template.ReorderSubjects(request.TopicExternalId, request.SubjectExternalIdsInOrder);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }

        await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return Success();
    }
}
