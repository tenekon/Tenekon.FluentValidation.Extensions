using System.Diagnostics.CodeAnalysis;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class EditModelSubpathParameterSetTransition : EditContextualComponentBaseParameterSetTransition
{
    [field: AllowNull]
    [field: MaybeNull]
    public ClassValueTransition<IEditModelValidationNotifier> RoutesOwningEditModelValidationNotifier =>
        field ??= new ClassValueTransition<IEditModelValidationNotifier> { Revisioner = this };

    /// <summary>
    /// Indiactes that the current transition used a self-created actor edit context derived from ancestor edit context.
    /// </summary>
    public bool IsActorEditContextAncestorDerived { get; set; }
}
