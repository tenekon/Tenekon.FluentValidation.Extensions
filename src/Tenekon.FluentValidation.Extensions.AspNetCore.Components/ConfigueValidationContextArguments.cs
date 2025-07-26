using FluentValidation;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class ConfigueValidationContextArguments
{
    public ValidationContext<object> ValidationContext { get; }

    internal ConfigueValidationContextArguments(ValidationContext<object> validationContext) => ValidationContext = validationContext;
}
