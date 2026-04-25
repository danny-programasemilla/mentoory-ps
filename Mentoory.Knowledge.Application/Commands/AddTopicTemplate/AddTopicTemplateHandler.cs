using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.AddTopicTemplate;

/// <summary>
/// Handles the addition of a topic to a module within a knowledge structure template.
/// </summary>
public class AddTopicTemplateHandler : BaseCommandHandler<AddTopicTemplateCommand, Guid>
{
    private readonly IKnowledgeStructureTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddTopicTemplateHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure template repository.</param>
    public AddTopicTemplateHandler(IKnowledgeStructureTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result<Guid>> Handle(
        AddTopicTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetByExternalIdWithFullTreeAsync(request.TemplateExternalId, cancellationToken);
        if (template is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Plantilla", "La plantilla no fue encontrada."));
        }

        try
        {
            var topic = template.AddTopic(request.ModuleExternalId, request.Name, request.Description, request.SortOrder);
            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            return Success(topic.ExternalId);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }
    }
}
