using MediatR;
using Mentoory.Diagnostic.Application.Commands.CloneFormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Infrastructure.Persistence;
using Mentoory.Knowledge.Application.Commands.AddModuleTemplate;
using Mentoory.Knowledge.Application.Commands.AddTopicTemplate;
using Mentoory.Knowledge.Infrastructure.Persistence;
using Mentoory.Shared.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Tests.E2E.Infrastructure;

/// <summary>
/// Helpers that drive the production Application/Infrastructure layers directly (bypassing the
/// Playwright UI) to set up DB state and assert DB invariants that the browser surface does
/// not expose. Contract: <c>specs/017-knowledge-e2e-tests/contracts/test-files.md</c> §
/// Shared Integration Helpers.
///
/// All helpers open a fresh <see cref="IServiceScope"/> per call — never share DbContexts or
/// MediatR scopes across operations. The returned entities are addressed by ExternalId so
/// callers don't leak internal ids back into browser-level assertions.
/// </summary>
public static class KnowledgeIntegrationHelpers
{
    /// <summary>ExternalId of the seeded <c>Emprendimiento Básico</c> KS template (005.SeedKnowledgeData.sql).</summary>
    public static readonly Guid SeededKsTemplateExternalId =
        new("11111111-1111-1111-1111-111111111111");

    /// <summary>ExternalId of the seeded <c>Diagnóstico Básico de Emprendimiento</c> bound FormTemplate (005.SeedKnowledgeData.sql § 3).</summary>
    public static readonly Guid SeededBoundFormTemplateExternalId =
        new("33333333-3333-3333-3333-333333333333");

    /// <summary>
    /// Creates a new FormTemplate via direct DbContext insert (there is no production
    /// CreateFormTemplateCommand) and optionally binds it to a KS template by ExternalId.
    /// Question TopicIds are the soft-referenced <c>TopicTemplates.Id</c> values that
    /// <see cref="CloneFormTemplateHandler"/> rewrites at clone time.
    /// </summary>
    public static async Task<Guid> CreateFormTemplateAsync(
        WebApplicationFactory<Program> factory,
        Guid? boundKsTemplateExternalId,
        params (long TopicId, string Text)[] questions)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();

        var template = FormTemplate.Create(
            $"E2E FormTemplate {Guid.NewGuid():N}", null, null, DateTime.UtcNow);
        if (boundKsTemplateExternalId is Guid bound)
        {
            template.SetDefaultKnowledgeStructureTemplate(bound);
        }

        for (var i = 0; i < questions.Length; i++)
        {
            template.AddQuestion(
                questions[i].TopicId,
                questions[i].Text,
                QuestionType.Text,
                StageApplicability.Both,
                sortOrder: i + 1,
                blockGroup: null,
                isOptional: false);
        }

        db.FormTemplates.Add(template);
        await db.SaveChangesAsync();
        return template.ExternalId;
    }

    /// <summary>
    /// Adds a TopicTemplate to an existing KS template via <see cref="AddTopicTemplateCommand"/>.
    /// Auto-resolves SortOrder to <c>max(existing) + 1</c> to mirror the UI's insertion behavior.
    /// </summary>
    public static async Task<Guid> AddTopicToTemplateAsync(
        WebApplicationFactory<Program> factory,
        Guid ksTemplateExternalId,
        Guid moduleExternalId,
        string topicName)
    {
        int nextSortOrder;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
            var template = await db.KnowledgeStructureTemplates
                .Include(t => t.Modules).ThenInclude(m => m.Topics)
                .FirstAsync(t => t.ExternalId == ksTemplateExternalId);
            var module = template.Modules.First(m => m.ExternalId == moduleExternalId);
            nextSortOrder = module.Topics.Count == 0
                ? 1
                : module.Topics.Max(t => t.SortOrder) + 1;
        }

        var result = await SendAsync(factory, new AddTopicTemplateCommand(
            ksTemplateExternalId, moduleExternalId, topicName, Description: null, SortOrder: nextSortOrder));
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"AddTopicTemplateCommand failed: {FormatErrors(result)}");
        }

        return result.Value!;
    }

    /// <summary>
    /// Adds a ModuleTemplate to an existing KS template via <see cref="AddModuleTemplateCommand"/>.
    /// Exposed as a convenience for tests that need a test-local module to anchor new topics under.
    /// </summary>
    public static async Task<Guid> AddModuleToTemplateAsync(
        WebApplicationFactory<Program> factory,
        Guid ksTemplateExternalId,
        string moduleName)
    {
        int nextSortOrder;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
            var template = await db.KnowledgeStructureTemplates
                .Include(t => t.Modules)
                .FirstAsync(t => t.ExternalId == ksTemplateExternalId);
            nextSortOrder = template.Modules.Count == 0
                ? 1
                : template.Modules.Max(m => m.SortOrder) + 1;
        }

        var result = await SendAsync(factory, new AddModuleTemplateCommand(
            ksTemplateExternalId, moduleName, Description: null, SortOrder: nextSortOrder));
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"AddModuleTemplateCommand failed: {FormatErrors(result)}");
        }

        return result.Value!;
    }

    /// <summary>
    /// Counts <c>diagnostic.ProjectForms</c> rows for a given project. Used by US3 tests to
    /// assert the mismatch path creates no row (before/after comparison).
    /// </summary>
    public static async Task<int> CountProjectFormsAsync(
        WebApplicationFactory<Program> factory,
        long projectId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
        return await db.ProjectForms
            .AsNoTracking()
            .CountAsync(f => f.ProjectId == projectId);
    }

    /// <summary>
    /// Counts <c>knowledge.KnowledgeStructures</c> rows for a given project. Under Phase 9's
    /// UNIQUE(ProjectId) constraint this is always 0 (pre-creation) or 1 (post-creation) — any
    /// other value signals a regression.
    /// </summary>
    public static async Task<int> CountProjectKnowledgeStructuresAsync(
        WebApplicationFactory<Program> factory,
        long projectId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
        return await db.Set<KS>()
            .AsNoTracking()
            .CountAsync(s => s.ProjectId == projectId);
    }

    /// <summary>
    /// Clones a FormTemplate into a project via <see cref="CloneFormTemplateCommand"/> and,
    /// on success, returns the new <c>ProjectForm.ExternalId</c>. Failures propagate as
    /// <c>Result.Failure&lt;Guid&gt;</c> so callers can assert the Spanish error text.
    /// </summary>
    public static async Task<Result<Guid>> CloneFormIntoProjectAsync(
        WebApplicationFactory<Program> factory,
        Guid formTemplateExternalId,
        long projectId,
        long incubatorId)
    {
        var command = new CloneFormTemplateCommand(formTemplateExternalId, projectId, incubatorId);
        Result result;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            result = await mediator.Send(command);
        }

        if (!result.IsSuccess)
        {
            return Result<Guid>.Failure(
                result.ErrorCode ?? ResultErrorCodes.GenericError,
                result.ErrorMessages ?? Array.Empty<(string Context, string Message)>());
        }

        using var queryScope = factory.Services.CreateScope();
        var db = queryScope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
        var projectFormExternalId = await db.ProjectForms
            .AsNoTracking()
            .Where(f => f.ProjectId == projectId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ThenByDescending(f => f.Id)
            .Select(f => f.ExternalId)
            .FirstAsync();
        return Result.Success(projectFormExternalId);
    }

    /// <summary>
    /// Returns the seeded <c>Emprendimiento Básico</c> KS template ExternalId. Compile-time
    /// constant — no DB round-trip — but wrapped as async for parity with the contract and
    /// to allow future replacement with a Name-based lookup.
    /// </summary>
    public static Task<Guid> GetSeededKsTemplateExternalIdAsync(WebApplicationFactory<Program> factory)
        => Task.FromResult(SeededKsTemplateExternalId);

    /// <summary>
    /// Returns the seeded <c>Diagnóstico Básico de Emprendimiento</c> FormTemplate ExternalId.
    /// </summary>
    public static Task<Guid> GetSeededBoundFormTemplateExternalIdAsync(WebApplicationFactory<Program> factory)
        => Task.FromResult(SeededBoundFormTemplateExternalId);

    private static async Task<TResponse> SendAsync<TResponse>(
        WebApplicationFactory<Program> factory,
        IRequest<TResponse> request)
    {
        using var scope = factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request);
    }

    private static string FormatErrors(Result result)
    {
        if (result.ErrorMessages is null || result.ErrorMessages.Length == 0)
        {
            return result.ErrorCode?.ToString() ?? "<unknown>";
        }

        return string.Join("; ", result.ErrorMessages.Select(m => $"[{m.Context}] {m.Message}"));
    }
}
