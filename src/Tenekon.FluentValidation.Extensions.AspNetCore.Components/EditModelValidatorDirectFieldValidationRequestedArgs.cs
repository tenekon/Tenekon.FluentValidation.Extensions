using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public sealed class EditModelValidatorDirectFieldValidationRequestedArgs(
    object source,
    object originalSource,
    FieldIdentifier fieldIdentifier) : EditModelValidatorValidationRequestArgs(source, originalSource)
{
    public FieldIdentifier FieldIdentifier { get; } = fieldIdentifier;
}
