using System.Diagnostics.CodeAnalysis;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class EditModelValidatorRoutesParameterSetTransition : EditModelScopeParameterSetTransition
{
    [field: AllowNull]
    [field: MaybeNull]
    public ClassValueTransition<IEditModelValidationNotifier> AncestorEditModelValidationNotifier =>
        field ??= new ClassValueTransition<IEditModelValidationNotifier> { Revisioner = this };
}
