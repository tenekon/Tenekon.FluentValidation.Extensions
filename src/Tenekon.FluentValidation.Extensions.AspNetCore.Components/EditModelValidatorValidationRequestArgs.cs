namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public abstract class EditModelValidatorValidationRequestArgs
{
    internal EditModelValidatorValidationRequestArgs(object source, object originalSource)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(originalSource);
        Source = source;
        OriginalSource = originalSource;
    }

    public object Source { get; set; }
    public object OriginalSource { get; }
}
