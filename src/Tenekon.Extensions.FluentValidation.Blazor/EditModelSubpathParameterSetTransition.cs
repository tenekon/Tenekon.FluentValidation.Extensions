using System.Diagnostics.CodeAnalysis;

namespace Tenekon.Extensions.FluentValidation.Blazor;

internal class EditModelValidatorRoutesParameterSetTransition : EditModelScopeParameterSetTransition
{
    [field: AllowNull]
    [field: MaybeNull]
    public ClassValueTransition<IEditModelValidationNotifier> AncestorEditModelValidationNotifier =>
        field ??= new ClassValueTransition<IEditModelValidationNotifier> { Revisioner = this };
}
