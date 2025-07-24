using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class ValidationScopeContext(EditContext editContext)
{
    public EditContext EditContext { get; } = editContext;
    public bool IsWithinScope { get; set; }
    public bool IsDirectDescendant { get; set; }
}
