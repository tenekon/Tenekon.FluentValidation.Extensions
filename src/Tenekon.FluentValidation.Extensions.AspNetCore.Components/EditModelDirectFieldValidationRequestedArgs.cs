using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public sealed class EditModelDirectFieldValidationRequestedArgs(
    object source,
    object originalSource,
    FieldIdentifier fieldIdentifier) : EditModelValidationRequestArgs(source, originalSource)
{
    public FieldIdentifier FieldIdentifier { get; } = fieldIdentifier;
}
