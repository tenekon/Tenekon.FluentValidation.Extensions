namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class HandlingParametersTransitionAccessor<T> where T : IHandlingParametersTransition
{
    public static ParametersTransitionHandlerRegistry ParametersTransitionHandlerRegistry => T.ParametersTransitionHandlerRegistry;
}
