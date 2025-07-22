namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public abstract class EditModelValidationRequestArgs
{
    internal EditModelValidationRequestArgs(object source, object originalSource)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(originalSource);
        Source = source;
        OriginalSource = originalSource;
    }

    public object Source { get; set; }
    public object OriginalSource { get; }
}
