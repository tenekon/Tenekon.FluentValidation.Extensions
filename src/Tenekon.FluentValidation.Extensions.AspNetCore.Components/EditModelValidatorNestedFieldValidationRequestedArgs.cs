using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public sealed class EditModelValidatorNestedFieldValidationRequestedArgs(
    object source,
    object originalSource,
    FieldIdentifier fullFieldPath,
    FieldIdentifier subFieldIdentifier) : EditModelValidatorValidationRequestArgs(source, originalSource)
{
    public FieldIdentifier FullFieldPath { get; } = fullFieldPath;
    public FieldIdentifier SubFieldIdentifier { get; } = subFieldIdentifier;
}
