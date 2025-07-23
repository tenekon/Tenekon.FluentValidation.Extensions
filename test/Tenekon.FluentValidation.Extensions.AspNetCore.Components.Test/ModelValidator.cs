using FluentValidation;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class ModelValidator : AbstractValidator<Model>
{
    public ModelValidator()
    {
        When(x => x.Hello is not null, () => RuleFor(x => x.Hello).Equal("World"));
        When(x => x.Child.Hello is not null, () => RuleFor(x => x.Child.Hello).Equal("World"));
    }
}
