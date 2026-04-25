using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.ReorderResourceTemplates;

/// <summary>
/// Handles the reordering of resources within a subject of a knowledge structure template.
/// </summary>
public class ReorderResourceTemplatesHandler : BaseCommandHandler<ReorderResourceTemplatesCommand>
{
    private readonly IKnowledgeStructureTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReorderResourceTemplatesHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure template repository.</param>
    public ReorderResourceTemplatesHandler(IKnowledgeStructureTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(
        ReorderResourceTemplatesCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetByExternalIdWithFullTreeAsync(request.TemplateExternalId, cancellationToken);
        if (template is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Plantilla", "La plantilla no fue encontrada."));
        }

        try
        {
            template.ReorderResources(request.SubjectExternalId, request.ResourceExternalIdsInOrder);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }

        await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return Success();
    }
}
