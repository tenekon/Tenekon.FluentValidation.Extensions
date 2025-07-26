using FluentValidation;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class ChildModelValidator : AbstractValidator<Model.ChildModel>
{
    public ChildModelValidator()
    {
        When(x => x.Hello is not null, () => RuleFor(x => x.Hello).Equal("World"));
    }
}
