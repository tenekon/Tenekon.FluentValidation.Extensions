using FluentValidation;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class ConfigueValidationContextArguments
{
    public ValidationContext<object> ValidationContext { get; }

    internal ConfigueValidationContextArguments(ValidationContext<object> validationContext) => ValidationContext = validationContext;
}
