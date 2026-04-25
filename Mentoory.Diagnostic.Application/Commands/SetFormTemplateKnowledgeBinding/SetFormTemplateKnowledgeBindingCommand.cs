using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Diagnostic.Application.Commands.SetFormTemplateKnowledgeBinding;

/// <summary>
/// Binds (or clears) a form template's default knowledge structure template.
/// When bound, subsequent <c>CloneFormTemplateCommand</c> invocations rewrite template-topic ids
/// to the matching project-topic ids in the auto-provisioned project <c>KnowledgeStructure</c>,
/// closing the cascade described in FR-K20 through FR-K23.
/// </summary>
/// <param name="FormTemplateExternalId">The external identifier of the form template to update.</param>
/// <param name="DefaultKnowledgeStructureTemplateExternalId">
/// The external identifier of the knowledge structure template to bind, or <c>null</c> to clear
/// the existing binding.
/// </param>
public sealed record SetFormTemplateKnowledgeBindingCommand(
    Guid FormTemplateExternalId,
    Guid? DefaultKnowledgeStructureTemplateExternalId) : IBaseRequest;
