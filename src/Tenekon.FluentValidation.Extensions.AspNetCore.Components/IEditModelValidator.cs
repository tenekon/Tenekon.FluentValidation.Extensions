using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal interface IEditModelValidator
{
    /// <summary>
    /// Initiates a model validation in the scope of this validator and those that are associated to the nearest EditContext,
    /// captured by the first validator component higher in the hierarchy (usually from a form).
    /// </summary>
    void ValidateFullModel();

    /// <summary>
    /// Initiates a model validation in the scope of this validator.
    /// </summary>
    void Validate();

    /// <summary>
    /// Initiates a top-level field validation in scope of this validator.
    /// </summary>
    void ValidateDirectField(FieldIdentifier fieldIdentifier);

    /// <summary>
    /// Initiates a nested-level field validation in scope of this validator.
    /// </summary>
    void ValidateNestedField(FieldIdentifier fullFieldPath, FieldIdentifier subFieldIdentifier);
}
