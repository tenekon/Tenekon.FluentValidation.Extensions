using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal interface IComponentValidator
{
    void ValidateModel(object? sender, ComponentValidatorModelValidationRequestArgs args);
    void ValidateDirectField(object? sender, ComponentValidatorDirectFieldValidationRequestArgs args);
    void ValidateNestedField(object? sender, ComponentValidatorNestedFieldValidationRequestArgs args);
}

public abstract class ValidationRequestArgs
{
    internal ValidationRequestArgs(object source, object originalSource)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(originalSource);
        Source = source;
        OriginalSource = originalSource;
    }

    public object Source { get; }
    public object OriginalSource { get; }
}

public sealed class ComponentValidatorModelValidationRequestArgs(object source, object originalSource)
    : ValidationRequestArgs(source, originalSource);

public sealed class ComponentValidatorDirectFieldValidationRequestArgs(
    object source,
    object originalSource,
    FieldIdentifier fieldIdentifier) : ValidationRequestArgs(source, originalSource)
{
    public FieldIdentifier FieldIdentifier { get; } = fieldIdentifier;
}

public sealed class ComponentValidatorNestedFieldValidationRequestArgs(
    object source,
    object originalSource,
    FieldIdentifier directFieldIdentifier,
    FieldIdentifier nestedFieldIdentifier) : ValidationRequestArgs(source, originalSource)
{
    public FieldIdentifier DirectFieldIdentifier { get; } = directFieldIdentifier;
    public FieldIdentifier NestedFieldIdentifier { get; } = nestedFieldIdentifier;
}
