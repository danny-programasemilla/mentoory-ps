# Mentoory Web Layer Patterns

## Controller Patterns

### Routing Convention — Attribute Routing Only

Controllers that use `externalId` route parameters (e.g. `[HttpGet("{externalId:guid}")]`) **must** use full attribute routing. Mixing attribute routes on actions with conventional routing on the controller causes route resolution failures.

```csharp
[Area("Platform")]
[Route("[area]/[controller]")]          // ← REQUIRED when any action has a route template
[Authorize(Roles = "GlobalAdmin")]
public class IncubatorsController : Controller
{
    [HttpGet("")]                       // Index — matches /Platform/Incubators
    public IActionResult Index() { }

    [HttpPost("[action]")]              // Data — matches /Platform/Incubators/Data
    public Task<IActionResult> Data() { }

    [HttpGet("[action]")]               // Create GET — matches /Platform/Incubators/Create
    public IActionResult Create() { }

    [HttpPost("[action]")]              // Create POST
    public Task<IActionResult> Create(CreateViewModel m) { }

    [HttpGet("{externalId:guid}")]      // Details — matches /Platform/Incubators/{guid}
    public Task<IActionResult> Details(Guid externalId) { }

    [HttpGet("{externalId:guid}/[action]")]  // Edit GET
    public Task<IActionResult> Edit(Guid externalId) { }

    [HttpPost("{externalId:guid}/[action]")] // Edit POST
    public Task<IActionResult> Edit(Guid externalId, EditViewModel m) { }
}
```

**Rules:**
- Every action MUST have an explicit route template (`""`, `"[action]"`, or custom)
- Never use `[HttpGet]`/`[HttpPost]` without a template on an attribute-routed controller
- In DataTable JS, use `System.Guid.Empty` as placeholder (not `"__ID__"`) — string placeholders fail the `:guid` constraint during URL generation:
```javascript
var placeholder = '00000000-0000-0000-0000-000000000000';
var url = '@Url.Action("Details", "Ctrl", new { area = "X", externalId = System.Guid.Empty })'.replace(placeholder, data);
```

### Base Controller Inheritance
```csharp
public class ProjectsController : BaseController  // ✅ Not Controller
{
    // Provides access to helper methods
}
```

## ViewModels and Forms

### Form Validation Pattern
```csharp
public class CreateProjectViewModel
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [Display(Name = "Nombre del Proyecto")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "La descripción es requerida")]
    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string Description { get; set; }
}
```

## Security Patterns

### CSRF Protection
- Automatically included in forms with tag helpers
- Manual: `@Html.AntiForgeryToken()`

### Input Validation
- Server-side validation always required
- Client-side for UX only
- Sanitize all inputs
- File upload validation (size, type, content)

## Bootstrap Integration
- Use Bootstrap 5 classes
- Consistent spacing: `mb-3`, `mt-4`, etc.
- Responsive tables: `table-responsive`
- Form controls: `form-control`, `form-select`
- Buttons: `btn btn-primary`, `btn btn-secondary`

## Phoenix Admin Template Compliance

### Use Only Template Components
**Policy**: Use only Phoenix Admin Template built-in components. Never add external libraries.

### Modal Dialogs
Use Bootstrap modals instead of external libraries:
```javascript
// Create Bootstrap modal helper
function showConfirmModal(title, message, confirmText, cancelText) {
    return new Promise((resolve) => {
        const modalHtml = `
            <div class="modal fade" id="confirmModal" tabindex="-1">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">${title}</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <p>${message}</p>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">${cancelText}</button>
                            <button type="button" class="btn btn-primary" id="confirmBtn">${confirmText}</button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        $('#confirmModal').remove();
        $('body').append(modalHtml);
        
        const modal = new bootstrap.Modal(document.getElementById('confirmModal'));
        
        $('#confirmBtn').on('click', () => {
            modal.hide();
            resolve(true);
        });
        
        document.getElementById('confirmModal').addEventListener('hidden.bs.modal', () => {
            $('#confirmModal').remove();
            resolve(false);
        });
        
        modal.show();
    });
}
```

### Toast Notifications
Use Bootstrap toasts for notifications:
```javascript
function showToast(message, type = 'info') {
    const toastHtml = `
        <div class="toast align-items-center text-white bg-${type} border-0">
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;
    
    const toastElement = $(toastHtml);
    $('#toastContainer').append(toastElement);
    const toast = new bootstrap.Toast(toastElement[0]);
    toast.show();
}
```

### Avoid External Dependencies
- ❌ NO SweetAlert2
- ❌ NO custom notification libraries
- ❌ NO external wizard libraries
- ✅ Use Bootstrap components
- ✅ Use Phoenix theme utilities

