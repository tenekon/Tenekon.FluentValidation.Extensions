namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal static class ParameterSetTransitionHandlerRegistryProvider<T> where T : IParameterSetTransitionHandlerRegistryProvider
{
    public static ParameterSetTransitionHandlerRegistry ParameterSetTransitionHandlerRegistry => T.ParameterSetTransitionHandlerRegistry;
}
