using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class EditModelScope : EditModelScopeBase<EditModelScope>, IParameterSetTransitionHandlerRegistryProvider, IEditModelScopeBaseTrait
{
    internal static readonly Func<IEditModelScopeBaseTrait.ErrorContext, Exception> s_defaultExceptionFactory = DefaultExceptionFactoryImpl;

    Func<IEditModelScopeBaseTrait.ErrorContext, Exception> IEditModelScopeBaseTrait.ExceptionFactory => s_defaultExceptionFactory;

    static ParameterSetTransitionHandlerRegistry IParameterSetTransitionHandlerRegistryProvider.ParameterSetTransitionHandlerRegistry {
        get;
    } = new();

    private static Exception DefaultExceptionFactoryImpl(IEditModelScopeBaseTrait.ErrorContext context) =>
        context.Identifier switch {
            IEditModelScopeBaseTrait.ErrorIdentifier.ActorEditContextAndModel => new InvalidOperationException(
                $"{context.Provocateur?.GetType().Name} requires exactly one non-null {nameof(Model)} parameter or non-null {nameof(EditContext)} parameter."),
            _ => IEditModelScopeBaseTrait.DefaultExceptionFactory(context)
        };

    bool IEditModelScopeBaseTrait.HasActorEditContextBeenSetExplicitly { get; set; }
    EditContext? IEditModelScopeBaseTrait.ActorEditContext { get; set; }
    object? IEditModelScopeBaseTrait.Model { get; set; }

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public object? Model {
#pragma warning restore BL0007 // Component parameters should be auto properties
        get => ((IEditModelScopeBaseTrait)this).Model;
        set => ((IEditModelScopeBaseTrait)this).Model = value;
    }

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public EditContext? EditContext {
#pragma warning restore BL0007 // Component parameters should be auto properties
        get => ((IEditModelScopeBaseTrait)this).ActorEditContext;
        set => ((IEditModelScopeBaseTrait)this).SetActorEditContextExplicitly(value);
    }
}
