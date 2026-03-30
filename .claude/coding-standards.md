# LinaSys Coding Standards

## Critical Build Requirements
- **Zero tolerance policy**: `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- **All warnings are errors**: Must fix ALL StyleCop violations
- **Clean build required**: No exceptions before committing code

## StyleCop Rules (Most Common)

### SA1028 - No Trailing Whitespace
- Remove all spaces/tabs at end of lines
- Use Edit tool to fix individual lines

### SA1116/SA1117 - Parameter Formatting
```csharp
// ❌ WRONG
logger.LogInformation("Message {Token} with {Status}", 
    token, status);

// ✅ CORRECT
logger.LogInformation(
    "Message {Token} with {Status}",
    token,
    status);
```

### SA1202/SA1204 - Member Ordering
- Properties before methods
- Public before private/internal
- Static before instance
- Order: Properties → Static Methods (public → internal) → Instance Methods

### SA1210 - Using Directive Order
- System namespaces first
- Then project namespaces
- All alphabetically sorted

### SA1413 - Trailing Commas
```csharp
// ✅ Required in multi-line initializers
_ => isDescending
    ? items.OrderByDescending(i => i.CreatedAt)
    : items.OrderBy(i => i.CreatedAt), // Trailing comma required
```

### SA1513 - Blank Lines After Closing Braces
```csharp
if (success) 
{
    logger.LogInfo("Success");
}
// Blank line required here
break;
```

### SA1518 - File Must End with Single Newline
- Every .cs file must end with exactly one newline character
- Use MultiEdit tool: `{"old_string": "}", "new_string": "}\n"}`

## General Coding Conventions

### Language and Localization
- **Spanish UI**: All user-facing text, error messages, and validation messages must be in Spanish
- **Code comments**: English (if needed - avoid unnecessary comments)
- **Variable names**: English

### LINQ Best Practices
- **Performance over readability**: Always use `!Any(condition)` over `All(!condition)`
- **Null safety**: Use `?? string.Empty` for nullable strings in DTOs

### Null Handling Patterns
- Use `is null` and `is not null` instead of `== null`
- Use null-conditional operators: `?.` and `?[]`
- Use null-coalescing: `??` and `??=`

### Naming Conventions
- **Commands**: `{Verb}{Entity}Command` (e.g., `CreateProjectCommand`)
- **Queries**: `{Get|List}{Entity}Query` (e.g., `GetProjectByIdQuery`)
- **DTOs**: `{Entity}Dto` (e.g., `ProjectDto`)
- **ViewModels**: `{Action}{Entity}ViewModel` (e.g., `CreateProjectViewModel`)

### File Organization
- One class per file
- File name matches class name
- Organize by feature, not by type

### Method Guidelines
- Keep methods small and focused
- Early returns for validation
- Async all the way down
- Avoid nested conditionals

### Comments and Documentation
- **Avoid comments**: Code should be self-documenting
- **No commented-out code**: Delete it, version control remembers
- **XML docs**: Only for public APIs

## Fixing StyleCop Violations

### Workflow
1. Run `dotnet build` to get all SA error codes
2. Group similar violations
3. Fix systematically by error type
4. Use MultiEdit for bulk fixes when safe
5. Test build frequently

### Common Fixes
- **Trailing whitespace**: Use sed or Edit tool
- **File endings**: Add single newline with MultiEdit
- **Parameter formatting**: Break after opening parenthesis
- **Member ordering**: Rearrange by visibility and static modifier

### Tools
- **Edit**: For single changes
- **MultiEdit**: For multiple changes in same file
- **Build verification**: Always run `dotnet build` after fixes

## DateTime Handling
**NEVER use `DateTime.UtcNow` directly in code**

### Application Layer
```csharp
// ✅ CORRECT - Inject ITimeProvider
public class SaveDraftCommandHandler(
    IBusinessIncubatorRepository repository,
    ITimeProvider timeProvider)
{
    var submission = project.StartFormSubmission(
        request.ParticipantUserId,
        request.FormId,
        timeProvider.UtcNow); // Use injected provider
}
```

### Domain Entities
```csharp
// ✅ CORRECT - Accept DateTime as parameter
public static UserProjectAccess Create(
    string userId,
    long projectId,
    DateTime syncedAt) // Passed from application layer
{
    return new UserProjectAccess
    {
        LastSyncedAt = syncedAt
    };
}

// ❌ WRONG - Generating DateTime internally
public void Deactivate()
{
    LastSyncedAt = DateTime.UtcNow; // Never do this!
}
```

### Integration Events
```csharp
// ✅ CORRECT - DateTime as parameter only
public record UserAddedToProjectIntegrationEvent(
    string UserId,
    long ProjectId,
    DateTime OccurredAt) : INotification;

// ❌ WRONG - Computed property
public DateTime Timestamp { get; } = DateTime.UtcNow;
```

## Constants Over Magic Strings

### Role Names
```csharp
// ✅ CORRECT - Use constants from Roles.cs
using LinaSys.Shared.Domain.Constants;

if (role?.Name == Roles.Administrator)
{
    // Grant admin access
}

// ❌ WRONG - Hardcoded strings
if (role?.Name == "Administrator")
{
    // Don't use magic strings
}
```

### Available Role Constants
- `Roles.Starter`
- `Roles.Coordinator`
- `Roles.Mentor`
- `Roles.Guide`
- `Roles.Facilitator`
- `Roles.Liaison`
- `Roles.Administrator`
- `Roles.GlobalAdministrator`

## ASP.NET Core Routing Patterns

### Controller Action Route Attributes
```csharp
// ❌ WRONG - Creates double controller name in URL
[HttpGet("GetStats")]
public async Task<IActionResult> GetStats()
// Results in: /Area/Controller/Controller/GetStats

// ✅ CORRECT - Uses conventional routing
[HttpGet]
public async Task<IActionResult> GetStats()
// Results in: /Area/Controller/GetStats
```

### WebFeatures Security Registration
```sql
-- For actions with both GET and POST with same name
'Controller.Action.Page+Post'

-- For GET-only actions
'Controller.Action.Get'  

-- For POST-only actions
'Controller.Action.Post'
```

### URL Generation Pattern
```csharp
// ✅ CORRECT - Use ApplicationUrlService for cross-area URLs
public class DashboardController(
    IApplicationUrlService applicationUrlService)
{
    private string GetFormUrl(Guid incubatorId, Guid projectId, long formId)
    {
        return applicationUrlService.GetParticipantFormUrl(incubatorId, projectId, formId);
    }
}

// ❌ WRONG - Hardcoding URLs
ActionUrl = $"/BusinessIncubators/{id}/Projects/{projectId}/ParticipantForm"

// ✅ CORRECT - Use Url.Action for same-area URLs
return Url.Action("Index", "Dashboard", new { area = "Participant" })
```

## EPPlus License Configuration

### Static Constructor Pattern
```csharp
// ✅ CORRECT - Set license once in static constructor
public class ParticipantExcelService
{
    static ParticipantExcelService()
    {
        ExcelPackage.License.SetNonCommercialPersonal("LinaSys");
    }
}

// ❌ WRONG - Setting in Program.cs or per-instance
ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Obsolete in v8
```

## SQL Database Project Patterns

### File Organization
**One DDL statement per file - SQL Server Database Project requirement**

```
LindaDb/
├── auth/
│   ├── Tables/
│   │   ├── UserProjectAccess.sql      # Table definition only
│   │   └── AspNetUsers.sql
│   └── Indexes/
│       ├── IX_UserProjectAccess_UserId_IsActive.sql
│       └── IX_UserProjectAccess_ProjectId_IsActive.sql
```

### Table Files
```sql
-- ✅ CORRECT - Tables/UserProjectAccess.sql
CREATE TABLE [auth].[UserProjectAccess]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    CONSTRAINT [PK_UserProjectAccess] PRIMARY KEY CLUSTERED ([Id] ASC)
);
```

### Index Files
```sql
-- ✅ CORRECT - Indexes/IX_UserProjectAccess_UserId_IsActive.sql
CREATE NONCLUSTERED INDEX [IX_UserProjectAccess_UserId_IsActive]
ON [auth].[UserProjectAccess] ([UserId], [IsActive])
INCLUDE ([ProjectId], [Role]);
```

### Naming Convention for Indexes
- Pattern: `IX_{TableName}_{Columns}.sql`
- Example: `IX_UserProjectAccess_UserId_ProjectId_Active.sql`

### Seed Data Organization
- Location: `LindaDb/PostDeployment/` directory
- Naming: Sequential numbering `00X.SeedXXX.sql`
- Integration: Add to `Script.PostDeployment.sql`
- Pattern: Use DELETE/INSERT for idempotency
- Auth sync: Mirror BusinessIncubator relationships to Auth read models

## Application Layer Patterns

### Separation of UI Concerns
```csharp
// ❌ WRONG - Application layer handling display formatting
public class ProjectStageDto
{
    public ProjectStageType Type { get; set; }
    public string TypeDisplay { get; set; } // UI concern!
}

// ✅ CORRECT - Application layer returns pure data
public class ProjectStageDto
{
    public ProjectStageType Type { get; set; } // Just the enum
    // Display formatting handled in UI layer
}
```

### Entity Framework Configuration
```csharp
// ❌ WRONG - Separate configuration classes
public class ProjectStageConfiguration : IEntityTypeConfiguration<ProjectStage>
{
    public void Configure(EntityTypeBuilder<ProjectStage> builder) { }
}

// ✅ CORRECT - Inline configuration in OnModelCreating
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<ProjectStage>(entity =>
    {
        entity.ToTable("ProjectStages", "businessincubators");
        // All configuration inline
    });
}
```

## Service Layer Patterns

### Repository Pattern in Infrastructure Services
```csharp
// ✅ CORRECT - Services use repository interfaces
public class ProjectAccessService : IProjectAccessService
{
    private readonly IAuthRepository _repository;
    private readonly ITimeProvider _timeProvider;
    private readonly IMemoryCache _cache;
    
    public async Task<bool> ValidateProjectAccessAsync(...)
    {
        // Use repository methods, not DbContext directly
        var access = await _repository.GetUserProjectAccessAsync(
            userId, projectId, cancellationToken);
    }
}

// ❌ WRONG - Direct DbContext usage in services
public class ProjectAccessService
{
    private readonly AuthDbContext _context;  // Avoid direct DbContext
}
```

### Caching Pattern for Access Services
```csharp
// Standard 5-minute cache pattern for access control
public async Task<List<ProjectInfo>> GetUserProjectsAsync(...)
{
    var cacheKey = $"user-projects:{userId}:{incubatorId}";
    
    // Try cache first
    if (_cache.TryGetValue<List<ProjectInfo>>(cacheKey, out var cached) 
        && cached != null)
    {
        return cached;
    }
    
    // Fetch from repository
    var data = await _repository.GetUserProjectAccessesAsync(userId, cancellationToken);
    
    // Cache for 5 minutes
    var cacheOptions = new MemoryCacheEntryOptions()
        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
    _cache.Set(cacheKey, data, cacheOptions);
    
    return data;
}
```

### Null Checking in Modern C#
```csharp
// ✅ CORRECT - Modern C# null checking
if (projectAccess is not null && projectAccess.IsActive)

// ❌ AVOID - Old style null checking  
if (projectAccess != null && projectAccess.IsActive)
```

## JavaScript and Frontend Standards

### File Organization
- Place all JavaScript in `/wwwroot/js/` directory  
- Use descriptive names: `area-feature.js` (e.g., `coordination-user-create.js`)
- CSS customizations in `/wwwroot/assets/css/linasys.css`
- NO JavaScript in Razor views (use data attributes for configuration)

### Toast Notifications (Phoenix-Aligned)
```javascript
// ✅ CORRECT - Use global showToast with new signature
showToast('Usuario creado exitosamente', 'success');
showToast('Error al guardar', 'danger', 'Error Crítico');

// ❌ WRONG - Old signature with multiple parameters
showToast('success', 'Message', null, 0, 'Header');
showToast('info', 'Message', 'icon-name', 5000);
```

**Toast Type Durations (Non-configurable)**:
- `success`: 4 seconds - Quick acknowledgment
- `info`: 5 seconds - Informational messages
- `warning`: 8 seconds - Needs user attention
- `danger`: Sticky - Must be manually dismissed

**Features**:
- Progress bar shows time remaining
- Hover to pause auto-dismiss
- Click to dismiss immediately
- Dark mode fully supported

### Phoenix Theme Integration
```css
/* ✅ CORRECT - Use Phoenix CSS variables */
.custom-element {
    background: rgba(var(--phoenix-body-bg-rgb), 0.95);
    border-radius: var(--phoenix-border-radius);
    box-shadow: var(--phoenix-box-shadow-lg);
}

/* ❌ WRONG - Hardcoded values */
.custom-element {
    background: #f5f7fa;
    border-radius: 6px;
    box-shadow: 0 10px 40px rgba(0,0,0,0.1);
}
```