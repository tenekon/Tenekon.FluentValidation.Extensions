namespace Tenekon.Extensions.FluentValidation.Blazor;

internal static class ParameterSetTransitionHandlerRegistryAccessor<T> where T : IParameterSetTransitionHandlerRegistryProvider
{
    public static ParameterSetTransitionHandlerRegistry ParameterSetTransitionHandlerRegistry => T.ParameterSetTransitionHandlerRegistry;
}
