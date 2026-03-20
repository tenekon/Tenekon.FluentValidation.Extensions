using FluentValidation;

namespace Tenekon.Extensions.FluentValidation.Blazor;

public class ChildModelValidator : AbstractValidator<Model.ChildModel>
{
    public ChildModelValidator() => When(x => x.Field1 is not null, () => RuleFor(x => x.Field1).NotEqual("FAILURE"));
}
