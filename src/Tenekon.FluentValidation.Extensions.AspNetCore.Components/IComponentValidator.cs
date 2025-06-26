using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal interface IComponentValidator
{
    void ValidateModel();
    void ValidateDirectField(FieldIdentifier fieldIdentifier);
    void ValidateNestedField(FieldIdentifier directFieldIdentifier, FieldIdentifier nestedFieldIdentifier);
}
