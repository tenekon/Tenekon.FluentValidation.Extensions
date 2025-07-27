using System.Diagnostics.CodeAnalysis;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class EditModelSubpathParameterSetTransition : EditModelScopeParameterSetTransition
{
    [field: AllowNull]
    [field: MaybeNull]
    public ClassValueTransition<IEditModelValidationNotifier> RoutesOwningEditModelValidationNotifier =>
        field ??= new ClassValueTransition<IEditModelValidationNotifier> { Revisioner = this };
}
