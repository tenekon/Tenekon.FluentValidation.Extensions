namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class ParametersTransition
{
    public bool IsDisposing { get; init; }
    public virtual required object EditContextualComponentBase { get; set; }
    public required EditContextTransition RootContextTransition { get; init; }
    public required EditContextTransition SuperContextTransition { get; init; }
    public required EditContextTransition OwnContextTransition { get; init; }
}
