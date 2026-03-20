using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.Extensions.FluentValidation.Blazor;

internal sealed class EditModelNestedFieldValidationRequestedArgs(
    object source,
    object originalSource,
    FieldIdentifier fullFieldPath,
    FieldIdentifier subFieldIdentifier) : EditModelValidationRequestArgs(source, originalSource)
{
    public FieldIdentifier FullFieldPath { get; } = fullFieldPath;
    public FieldIdentifier SubFieldIdentifier { get; } = subFieldIdentifier;
}
