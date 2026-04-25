using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Application.Commands.AddSubject;

/// <summary>
/// Handles the addition of a subject under a topic within a knowledge structure (project clone).
/// </summary>
public class AddSubjectHandler : BaseCommandHandler<AddSubjectCommand, Guid>
{
    private readonly IKnowledgeStructureRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddSubjectHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure repository.</param>
    public AddSubjectHandler(IKnowledgeStructureRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result<Guid>> Handle(
        AddSubjectCommand request,
        CancellationToken cancellationToken)
    {
        KS? structure = await _repository.GetByExternalIdWithFullTreeAsync(
            request.StructureExternalId,
            cancellationToken);

        if (structure is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Estructura", "La estructura de conocimiento no fue encontrada."));
        }

        try
        {
            var subject = structure.AddSubject(
                request.TopicExternalId,
                request.Name,
                request.Description,
                request.SortOrder);

            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            return Success(subject.ExternalId);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }
    }
}
