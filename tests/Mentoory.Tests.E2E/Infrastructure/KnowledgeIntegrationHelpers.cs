using MediatR;
using Mentoory.Access.Application.Commands.AssignRole;
using Mentoory.Access.Domain.Aggregates.User;
using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Diagnostic.Application.Commands.CloneFormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Infrastructure.Persistence;
using Mentoory.Knowledge.Application.Commands.AddModuleTemplate;
using Mentoory.Knowledge.Application.Commands.AddTopicTemplate;
using Mentoory.Knowledge.Application.Commands.CreateKnowledgeStructureTemplate;
using Mentoory.Knowledge.Infrastructure.Persistence;
using Mentoory.Shared.Application;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Tenant.Application.Commands.CreateProject;
using Mentoory.Tenant.Infrastructure.Persistence;
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

    /// <summary>
    /// Creates a fresh <c>KnowledgeStructureTemplate</c> via <see cref="CreateKnowledgeStructureTemplateCommand"/>.
    /// Used by US3-2 to build a KS template distinct from the one the target project is bound to.
    /// </summary>
    public static async Task<Guid> CreateKsTemplateAsync(
        WebApplicationFactory<Program> factory,
        string templateName)
    {
        var result = await SendAsync(factory, new CreateKnowledgeStructureTemplateCommand(templateName, null));
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"CreateKnowledgeStructureTemplateCommand failed: {FormatErrors(result)}");
        }

        return result.Value!;
    }

    /// <summary>
    /// Looks up an incubator by exact name and returns its internal id and external id. The
    /// seeded incubators (e.g., <c>Incubadora Alpha</c>) use <c>NEWID()</c> for ExternalId so
    /// tests that need the ExternalId (e.g., to pass to <see cref="CreateProjectCommand"/>)
    /// must resolve it at runtime.
    /// </summary>
    public static async Task<(long Id, Guid ExternalId)> GetIncubatorByNameAsync(
        WebApplicationFactory<Program> factory,
        string incubatorName)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var incubator = await db.Incubators
            .AsNoTracking()
            .FirstAsync(i => i.Name == incubatorName);
        return (incubator.Id, incubator.ExternalId);
    }

    /// <summary>
    /// Looks up a project by exact name and returns its internal ids. The seeded projects
    /// (e.g., <c>Proyecto Innovación</c>) use <c>NEWID()</c> for ExternalId so tests that
    /// need the internal ids (e.g., for <see cref="CountProjectFormsAsync"/>) must resolve
    /// them at runtime. Applies <c>IgnoreQueryFilters()</c> so the lookup works even when
    /// no tenant context is installed on the scope.
    /// </summary>
    public static async Task<(long ProjectId, long IncubatorId, Guid ExternalId)> GetProjectByNameAsync(
        WebApplicationFactory<Program> factory,
        string projectName)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var project = await db.Projects
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(p => p.Name == projectName);
        return (project.Id, project.IncubatorId, project.ExternalId);
    }

    /// <summary>
    /// Inserts a fresh <see cref="User"/> + <see cref="Credential"/> pair directly via
    /// <see cref="AccessDbContext"/>, then marks the account <c>Active</c>. Email and
    /// NationalId are derived from a new <c>Guid</c> so concurrent invocations don't collide
    /// on the unique index. Returns the seeded user's internal id, email, and plaintext
    /// password ready for <c>LoginAndSelectAsync</c>.
    /// </summary>
    /// <remarks>
    /// This intentionally sidesteps the <c>RegisterUser</c> command path: that flow goes
    /// through MailKit and expects <c>PendingVerification → Active</c> via the email-
    /// verification token, which is noise for a test fixture. The seeded users in
    /// <c>004.SeedTestData.sql</c> use the same direct-insert approach.
    /// </remarks>
    public static async Task<(long UserId, string Email, string Password)>
        CreateTransientCoordinatorUserAsync(WebApplicationFactory<Program> factory)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"e2e-coord-{suffix}@test.mentoory.com";
        var nationalId = $"TEST-E2E-{suffix[..12]}";
        var utcNow = DateTime.UtcNow;

        var user = User.Register(
            email: email,
            country: "Chile",
            nationalId: nationalId,
            firstName: "E2E",
            lastName: $"Coord {suffix[..8]}",
            passwordHash: Transient.PasswordHash,
            utcNow: utcNow);
        user.VerifyEmail(utcNow);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return (user.Id, email, Transient.Password);
    }

    /// <summary>
    /// Creates a fresh project in <paramref name="incubatorName"/> bound to
    /// <paramref name="ksTemplateExternalId"/>, provisions a <b>throwaway</b> coordinator
    /// user (via <see cref="CreateTransientCoordinatorUserAsync"/>), and grants that user a
    /// <c>ProjectCoordinator</c> role assignment on the new project. The project goes
    /// through <see cref="CreateProjectCommand"/>, which invokes
    /// <c>IKnowledgeStructureProvisioner</c> to materialize the project's KS with
    /// <c>SourceTemplateTopicExternalId</c> correctly populated — a precondition for
    /// <see cref="CloneFormTemplateHandler"/>'s TopicId rewrite to succeed.
    /// </summary>
    /// <remarks>
    /// Each call creates its own throwaway user rather than reusing a seeded coordinator:
    /// E2E tests share DB state within a collection (no Respawn between tests), so granting
    /// additional assignments to a seeded account accumulates across the run and breaks any
    /// legacy test that asserts a specific coordinator's project count.
    /// </remarks>
    public static async Task<(long ProjectId, long IncubatorId, Guid ExternalId, string Name, string CoordinatorEmail, string CoordinatorPassword)>
        CreateProjectWithCoordinatorAsync(
            WebApplicationFactory<Program> factory,
            string incubatorName,
            Guid ksTemplateExternalId,
            string projectName)
    {
        var (incubatorId, incubatorExternalId) = await GetIncubatorByNameAsync(factory, incubatorName);
        var (userId, coordinatorEmail, coordinatorPassword) = await CreateTransientCoordinatorUserAsync(factory);

        var createResult = await SendAsync(factory, new CreateProjectCommand(
            incubatorExternalId, projectName, Description: null, ksTemplateExternalId));
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException(
                $"CreateProjectCommand failed for '{projectName}': {FormatErrors(createResult)}");
        }

        var projectExternalId = createResult.Value!;
        long projectId;

        using (var scope = factory.Services.CreateScope())
        {
            var tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            projectId = await tenantDb.Projects
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(p => p.ExternalId == projectExternalId)
                .Select(p => p.Id)
                .FirstAsync();
        }

        var assignResult = await SendAsync(factory, new AssignRoleCommand(
            userId, incubatorId, projectId, Roles.ProjectCoordinator));
        if (!assignResult.IsSuccess)
        {
            throw new InvalidOperationException(
                $"AssignRoleCommand failed for {coordinatorEmail} on project {projectName}: {FormatErrors(assignResult)}");
        }

        return (projectId, incubatorId, projectExternalId, projectName, coordinatorEmail, coordinatorPassword);
    }

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

    // Credentials for the throwaway users created by CreateTransientCoordinatorUserAsync.
    // Nested so both the plaintext and the hash stay implementation-detail of this helper —
    // exposing either at the class level would leak them into test code that should only
    // touch the plaintext via the tuple returned by CreateProjectWithCoordinatorAsync.
    private static class Transient
    {
        public const string Password = "Test123!@#";
        public const string PasswordHash =
            "pbkdf2-sha512$600000$vXEKVDDyckYOMgyiPBPUQg==$ZTWBL0Wx1XaoHBz9SRedZ1nbH0wZDh5taxDgqlKsn7A=";
    }
}
