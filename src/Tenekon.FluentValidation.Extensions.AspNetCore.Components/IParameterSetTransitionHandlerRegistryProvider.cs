namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public interface IParameterSetTransitionHandlerRegistryProvider
{
    internal static abstract ParameterSetTransitionHandlerRegistry ParameterSetTransitionHandlerRegistry { get; }
}
