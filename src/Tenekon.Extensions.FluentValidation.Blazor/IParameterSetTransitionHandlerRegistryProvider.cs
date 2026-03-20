namespace Tenekon.Extensions.FluentValidation.Blazor;

public interface IParameterSetTransitionHandlerRegistryProvider
{
    internal static abstract ParameterSetTransitionHandlerRegistry ParameterSetTransitionHandlerRegistry { get; }
}
