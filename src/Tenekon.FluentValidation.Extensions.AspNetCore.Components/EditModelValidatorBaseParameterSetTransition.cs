using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class EditModelValidatorBaseParameterSetTransition : EditContextualComponentBaseParameterSetTransition
{
    [field: AllowNull]
    [field: MaybeNull]
    public virtual ClassValueTransition<Expression<Func<object>>[]> Routes {
        get => field ??= new ClassValueTransition<Expression<Func<object>>[]>() { Revisioner = this };
    }
}
