using MediatR;
using Mentoory.Diagnostic.Domain.Aggregates.DiagnosticResponse;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Shared.Application.Interfaces;
using Mentoory.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Diagnostic.Infrastructure.Persistence;

/// <summary>
/// Database context for the Diagnostic domain.
/// </summary>
public class DiagnosticDbContext : SharedAbstractDbContext
{
    private readonly ITenantContext _tenantContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticDbContext"/> class.
    /// </summary>
    public DiagnosticDbContext(DbContextOptions<DiagnosticDbContext> options, IMediator mediator, ITenantContext tenantContext)
        : base(options, mediator)
    {
        _tenantContext = tenantContext;
    }

    public virtual DbSet<FormTemplate> FormTemplates { get; set; } = null!;
    public virtual DbSet<QuestionTemplate> QuestionTemplates { get; set; } = null!;
    public virtual DbSet<AnswerOptionTemplate> AnswerOptionTemplates { get; set; } = null!;
    public virtual DbSet<ProjectForm> ProjectForms { get; set; } = null!;
    public virtual DbSet<Question> Questions { get; set; } = null!;
    public virtual DbSet<AnswerOption> AnswerOptions { get; set; } = null!;
    public virtual DbSet<FollowUpQuestion> FollowUpQuestions { get; set; } = null!;
    public virtual DbSet<DiagnosticResponse> DiagnosticResponses { get; set; } = null!;
    public virtual DbSet<QuestionResponse> QuestionResponses { get; set; } = null!;
    public virtual DbSet<AnswerCorrection> AnswerCorrections { get; set; } = null!;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureFormTemplate(modelBuilder);
        ConfigureQuestionTemplate(modelBuilder);
        ConfigureAnswerOptionTemplate(modelBuilder);
        ConfigureProjectForm(modelBuilder);
        ConfigureQuestion(modelBuilder);
        ConfigureAnswerOption(modelBuilder);
        ConfigureFollowUpQuestion(modelBuilder);
        ConfigureDiagnosticResponse(modelBuilder);
        ConfigureQuestionResponse(modelBuilder);
        ConfigureAnswerCorrection(modelBuilder);
    }

    private void ConfigureFormTemplate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FormTemplate>(entity =>
        {
            entity.ToTable("FormTemplates", "diagnostic");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.SubscriptionTier).HasMaxLength(50);
            entity.Property(e => e.Version).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAtUtc).IsRequired();

            entity.HasMany(e => e.Questions)
                .WithOne()
                .HasForeignKey("FormTemplateId")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureQuestionTemplate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuestionTemplate>(entity =>
        {
            entity.ToTable("QuestionTemplates", "diagnostic");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TopicId).IsRequired();
            entity.Property(e => e.QuestionText).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.QuestionType).IsRequired().HasConversion<byte>();
            entity.Property(e => e.StageApplicability).IsRequired().HasConversion<byte>();
            entity.Property(e => e.SortOrder).IsRequired();
            entity.Property(e => e.BlockGroup).HasMaxLength(100);
            entity.Property(e => e.IsOptional).IsRequired().HasDefaultValue(false);

            entity.Property<long>("FormTemplateId").IsRequired();

            entity.HasMany(e => e.AnswerOptions)
                .WithOne()
                .HasForeignKey("QuestionTemplateId")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureAnswerOptionTemplate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnswerOptionTemplate>(entity =>
        {
            entity.ToTable("AnswerOptionTemplates", "diagnostic");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OptionText).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Score).IsRequired().HasPrecision(10, 2);
            entity.Property(e => e.SwotClassification).IsRequired().HasConversion<byte>();
            entity.Property(e => e.OdsrOrientation).IsRequired().HasConversion<byte>();
            entity.Property(e => e.SortOrder).IsRequired();

            entity.Property<long>("QuestionTemplateId").IsRequired();
        });
    }

    private void ConfigureProjectForm(ModelBuilder modelBuilder)
    {
        var tenantContext = _tenantContext;
        modelBuilder.Entity<ProjectForm>(entity =>
        {
            entity.ToTable("ProjectForms", "diagnostic");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();

            entity.Property(e => e.ProjectId).IsRequired();
            entity.HasIndex(e => e.ProjectId);

            entity.Property(e => e.IncubatorId).IsRequired();
            entity.HasIndex(e => e.IncubatorId);

            entity.Property(e => e.SourceTemplateId);
            entity.Property(e => e.SourceTemplateVersion);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SyncMode).IsRequired().HasConversion<byte>().HasDefaultValue(SyncMode.Disconnected);
            entity.Property(e => e.CreatedAtUtc).IsRequired();

            // Multi-tenant query filter
            entity.HasQueryFilter(e => tenantContext.CurrentIncubatorId == null || e.IncubatorId == tenantContext.CurrentIncubatorId);

            entity.HasMany(e => e.Questions)
                .WithOne()
                .HasForeignKey("ProjectFormId")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureQuestion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("Questions", "diagnostic");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();

            entity.Property(e => e.TopicId).IsRequired();
            entity.Property(e => e.QuestionText).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.QuestionType).IsRequired().HasConversion<byte>();
            entity.Property(e => e.StageApplicability).IsRequired().HasConversion<byte>();
            entity.Property(e => e.SortOrder).IsRequired();
            entity.Property(e => e.BlockGroup).HasMaxLength(100);
            entity.Property(e => e.IsOptional).IsRequired().HasDefaultValue(false);

            entity.Property<long>("ProjectFormId").IsRequired();

            entity.HasMany(e => e.AnswerOptions)
                .WithOne()
                .HasForeignKey("QuestionId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.FollowUpQuestions)
                .WithOne()
                .HasForeignKey("QuestionId")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureAnswerOption(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnswerOption>(entity =>
        {
            entity.ToTable("AnswerOptions", "diagnostic");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OptionText).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Score).IsRequired().HasPrecision(10, 2);
            entity.Property(e => e.SwotClassification).IsRequired().HasConversion<byte>();
            entity.Property(e => e.OdsrOrientation).IsRequired().HasConversion<byte>();
            entity.Property(e => e.SortOrder).IsRequired();

            entity.Property<long>("QuestionId").IsRequired();
        });
    }

    private void ConfigureFollowUpQuestion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FollowUpQuestion>(entity =>
        {
            entity.ToTable("FollowUpQuestions", "diagnostic");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.QuestionText).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.SortOrder).IsRequired();

            entity.Property<long>("QuestionId").IsRequired();
        });
    }

    private void ConfigureDiagnosticResponse(ModelBuilder modelBuilder)
    {
        var tenantContext = _tenantContext;
        modelBuilder.Entity<DiagnosticResponse>(entity =>
        {
            entity.ToTable("DiagnosticResponses", "diagnostic");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalId).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();

            entity.Property(e => e.ProjectFormId).IsRequired();
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.IncubatorId).IsRequired();
            entity.HasIndex(e => e.IncubatorId);

            entity.Property(e => e.EntrepreneurUserId).IsRequired();
            entity.Property(e => e.EvaluationStage).IsRequired().HasConversion<byte>();
            entity.Property(e => e.IsCompleted).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.CompletedAtUtc);
            entity.Property(e => e.CreatedAtUtc).IsRequired();

            // Multi-tenant query filter
            entity.HasQueryFilter(e => tenantContext.CurrentIncubatorId == null || e.IncubatorId == tenantContext.CurrentIncubatorId);

            entity.HasIndex(e => new { e.ProjectFormId, e.EntrepreneurUserId, e.EvaluationStage })
                .IsUnique();

            entity.HasMany(e => e.QuestionResponses)
                .WithOne()
                .HasForeignKey("DiagnosticResponseId")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureQuestionResponse(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuestionResponse>(entity =>
        {
            entity.ToTable("QuestionResponses", "diagnostic");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.QuestionId).IsRequired();
            entity.Property(e => e.TextValue).HasMaxLength(4000);
            entity.Property(e => e.NumericValue).HasPrecision(10, 2);
            entity.Property(e => e.SelectedOptionIdsRaw)
                .HasColumnName("SelectedOptionIds")
                .HasMaxLength(500);
            entity.Property(e => e.CreatedAtUtc).IsRequired();

            entity.Property<long>("DiagnosticResponseId").IsRequired();

            entity.Ignore(e => e.SelectedOptionIds);

            entity.HasMany(e => e.Corrections)
                .WithOne()
                .HasForeignKey("QuestionResponseId")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureAnswerCorrection(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnswerCorrection>(entity =>
        {
            entity.ToTable("AnswerCorrections", "diagnostic");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PreviousTextValue).HasMaxLength(4000);
            entity.Property(e => e.PreviousNumericValue).HasPrecision(10, 2);
            entity.Property(e => e.PreviousSelectedOptionIds).HasMaxLength(500);
            entity.Property(e => e.CorrectedByUserId).IsRequired();
            entity.Property(e => e.CorrectedAtUtc).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(500);

            entity.Property<long>("QuestionResponseId").IsRequired();
        });
    }
}
