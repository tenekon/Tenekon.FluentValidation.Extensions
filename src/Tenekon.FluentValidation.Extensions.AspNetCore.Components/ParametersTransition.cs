namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class ParametersTransition
{
    public bool IsDisposing { get; init; }
    public virtual required object EditContextualComponentBase { get; set; }
    public required EditContextTransition RootContextTransition { get; init; }
    public required EditContextTransition AncestorContextTransition { get; init; }
    public required EditContextTransition ActorContextTransition { get; init; }
}
