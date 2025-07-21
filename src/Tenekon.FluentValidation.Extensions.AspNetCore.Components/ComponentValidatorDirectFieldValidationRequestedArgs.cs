using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public sealed class ComponentValidatorDirectFieldValidationRequestedArgs(
    object source,
    object originalSource,
    FieldIdentifier fieldIdentifier) : ComponentValidatorValidationRequestArgs(source, originalSource)
{
    public FieldIdentifier FieldIdentifier { get; } = fieldIdentifier;
}
