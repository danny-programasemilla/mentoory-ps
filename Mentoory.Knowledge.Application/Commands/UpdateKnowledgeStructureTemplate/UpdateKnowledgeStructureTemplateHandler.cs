using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.UpdateKnowledgeStructureTemplate;

/// <summary>
/// Handles updates to the details of an existing knowledge structure template.
/// </summary>
public class UpdateKnowledgeStructureTemplateHandler
    : BaseCommandHandler<UpdateKnowledgeStructureTemplateCommand>
{
    private readonly IKnowledgeStructureTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateKnowledgeStructureTemplateHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure template repository.</param>
    public UpdateKnowledgeStructureTemplateHandler(IKnowledgeStructureTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(
        UpdateKnowledgeStructureTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetByExternalIdAsync(request.ExternalId, cancellationToken);

        if (template is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Plantilla", "La plantilla no fue encontrada."));
        }

        template.UpdateDetails(request.Name, request.Description);

        await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return Success();
    }
}
