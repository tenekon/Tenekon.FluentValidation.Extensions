using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.Extensions.FluentValidation.Blazor;

internal class ValidationScopeContext(EditContext editContext)
{
    public EditContext EditContext { get; } = editContext;
    public bool IsWithinScope { get; set; }
    public bool IsDirectDescendant { get; set; }
}
