using FluentValidation;

namespace Tenekon.Extensions.FluentValidation.Blazor;

internal class ConfigueValidationContextArguments
{
    public ValidationContext<object> ValidationContext { get; }

    internal ConfigueValidationContextArguments(ValidationContext<object> validationContext) => ValidationContext = validationContext;
}
