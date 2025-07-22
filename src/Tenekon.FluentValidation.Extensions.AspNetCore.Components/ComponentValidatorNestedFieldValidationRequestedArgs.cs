using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public sealed class ComponentValidatorNestedFieldValidationRequestedArgs(
    object source,
    object originalSource,
    FieldIdentifier fullFieldPath,
    FieldIdentifier subFieldIdentifier) : ComponentValidatorValidationRequestArgs(source, originalSource)
{
    public FieldIdentifier FullFieldPath { get; } = fullFieldPath;
    public FieldIdentifier SubFieldIdentifier { get; } = subFieldIdentifier;
}
