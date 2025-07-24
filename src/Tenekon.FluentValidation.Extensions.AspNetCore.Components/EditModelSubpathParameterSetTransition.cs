namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class EditModelSubpathParameterSetTransition : EditContextualComponentBaseParameterSetTransition
{
    private ClassValueTransition<IEditModelValidationNotifier> RoutesOwningEditModelValidationNotifier =>
        field ??= new ClassValueTransition<IEditModelValidationNotifier>() { Revisioner = this };
    
    /// <summary>
    /// Indiactes that the current transition used a self-created actor edit context derived from ancestor edit context.
    /// </summary>
    public bool IsActorEditContextAncestorDerived { get; set; }
}
