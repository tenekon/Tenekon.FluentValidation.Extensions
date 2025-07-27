namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class EditModelScopeParameterSetTransition : EditContextualComponentBaseParameterSetTransition
{
    /// <summary>
    /// Indiactes that the current transition used a self-created actor edit context derived from ancestor edit context.
    /// </summary>
    public bool IsActorEditContextAncestorDerived { get; set; }
}
