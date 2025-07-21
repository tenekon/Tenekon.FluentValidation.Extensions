using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public sealed class ComponentValidatorNestedFieldValidationRequestedArgs(
    object source,
    object originalSource,
    FieldIdentifier directFieldIdentifier,
    FieldIdentifier nestedFieldIdentifier) : ComponentValidatorValidationRequestArgs(source, originalSource)
{
    public FieldIdentifier DirectFieldIdentifier { get; } = directFieldIdentifier;
    public FieldIdentifier NestedFieldIdentifier { get; } = nestedFieldIdentifier;
}
