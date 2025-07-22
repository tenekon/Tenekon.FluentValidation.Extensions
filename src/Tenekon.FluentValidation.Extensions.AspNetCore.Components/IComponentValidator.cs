using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal interface IComponentValidator
{
    /// <summary>
    /// Initiates a model validation in this component validators and those that are associated to the nearest EditContext,
    /// captured by the first validator component higher in the hierarchy (usually from a form).
    /// </summary>
    void ValidateFullModel();

    /// <summary>
    /// Initiates a model validation in this component validator.
    /// </summary>
    void Validate();

    /// <summary>
    /// Initiates a top-level field validation in this component validator.
    /// </summary>
    void ValidateDirectField(FieldIdentifier fieldIdentifier);

    /// <summary>
    /// Initiates a nested-level field validation in this component validator.
    /// </summary>
    void ValidateNestedField(FieldIdentifier directFieldIdentifier, FieldIdentifier nestedFieldIdentifier);
}
