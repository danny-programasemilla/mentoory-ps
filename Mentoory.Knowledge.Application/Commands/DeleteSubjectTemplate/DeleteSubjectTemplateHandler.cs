using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.DeleteSubjectTemplate;

/// <summary>
/// Handles the deletion of a subject from a knowledge structure template.
/// </summary>
public class DeleteSubjectTemplateHandler : BaseCommandHandler<DeleteSubjectTemplateCommand>
{
    private readonly IKnowledgeStructureTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSubjectTemplateHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure template repository.</param>
    public DeleteSubjectTemplateHandler(IKnowledgeStructureTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(
        DeleteSubjectTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetByExternalIdWithFullTreeAsync(request.TemplateExternalId, cancellationToken);
        if (template is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Plantilla", "La plantilla no fue encontrada."));
        }

        try
        {
            template.RemoveSubject(request.SubjectExternalId);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }

        await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return Success();
    }
}
