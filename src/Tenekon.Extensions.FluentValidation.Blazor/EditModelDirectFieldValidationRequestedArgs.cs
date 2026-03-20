using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.Extensions.FluentValidation.Blazor;

internal sealed class EditModelDirectFieldValidationRequestedArgs(object source, object originalSource, FieldIdentifier fieldIdentifier)
    : EditModelValidationRequestArgs(source, originalSource)
{
    public FieldIdentifier FieldIdentifier { get; } = fieldIdentifier;
}
