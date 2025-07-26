using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class EditModelValidatorSubpath : EditModelValidatorBase<EditModelValidatorSubpath>, IEditContextualComponentTrait,
    IEditModelValidatorSubpathTrait, IParameterSetTransitionHandlerRegistryProvider
{
    public static readonly Func<IEditModelValidatorSubpathTrait.ErrorContext, Exception> DefaultExceptionFactory =
        DefaultExceptionFactoryImpl;

    static ParameterSetTransitionHandlerRegistry IParameterSetTransitionHandlerRegistryProvider.ParameterSetTransitionHandlerRegistry {
        get;
    } = new();

    private static Exception DefaultExceptionFactoryImpl(IEditModelValidatorSubpathTrait.ErrorContext context) =>
        context.Identifier switch {
            IEditModelValidatorSubpathTrait.ErrorIdentifier.NotExactlyOneActorEditContextOrModel => new InvalidOperationException(
                $"{context.Provocateur?.GetType().Name} requires exactly one non-null {nameof(Model)} parameter or non-null {nameof(EditContext)} parameter."),
            _ => IEditModelValidatorSubpathTrait.DefaultExceptionFactory(context)
        };

    Func<IEditModelValidatorSubpathTrait.ErrorContext, Exception> IEditModelValidatorSubpathTrait.ExceptionFactory =>
        DefaultExceptionFactory;

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public object? Model {
#pragma warning restore BL0007 // Component parameters should be auto properties
        get => ((IEditModelValidatorSubpathTrait)this).Model;
        set => ((IEditModelValidatorSubpathTrait)this).Model = value;
    }

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public EditContext? EditContext {
#pragma warning restore BL0007 // Component parameters should be auto properties
        get => ((IEditModelValidatorSubpathTrait)this).ActorEditContext;
        set => ((IEditModelValidatorSubpathTrait)this).SetActorEditContextExplicitly(value);
    }

    bool IEditModelValidatorSubpathTrait.HasActorEditContextBeenSetExplicitly { get; set; }
    object? IEditModelValidatorSubpathTrait.Model { get; set; }
    EditContext? IEditModelValidatorSubpathTrait.ActorEditContext { get; set; }

    EditContext? IEditContextualComponentTrait.ActorEditContext =>
        ((IEditModelValidatorSubpathTrait)this).ActorEditContext ?? throw new InvalidOperationException();

    protected override async Task OnParametersSetAsync()
    {
        await ((IEditModelValidatorSubpathTrait)this).OnSubpathParametersSetAsync();
        await base.OnParametersSetAsync();
    }

    // ReSharper disable once MemberHidesInterfaceMemberWithDefaultImplementation
    internal override async Task OnParametersTransitioningAsync()
    {
        var actorEditContext = ((IEditModelValidatorSubpathTrait)this).ActorEditContext;
        Debug.Assert(actorEditContext is not null);
        if (Validator is null && ValidatorType is null) {
            ValidatorType = typeof(IValidator<>).MakeGenericType(actorEditContext.Model.GetType());
        }

        await base.OnParametersTransitioningAsync();
    }

    /* TODO: Make pluggable */
    // protected override void OnActorEditContextChanged(EditContextChangedEventArgs args)
    // {
    //     RootEditModelValidatorContext rootEditModelValidatorContext;
    //     if (args.New.Properties.TryGetValue(EditModelValidatorContextLookupKey.Standard, out var validatorContext)) {
    //         if (validatorContext is not RootEditModelValidatorContext rootValidatorContext2) {
    //             throw new InvalidOperationException("Root validator context lookup key was misued from a third-party.");
    //         }
    //         rootEditModelValidatorContext = rootValidatorContext2;
    //     } else {
    //         rootEditModelValidatorContext = new RootEditModelValidatorContext();
    //         args.New.Properties[EditModelValidatorContextLookupKey.Standard] = rootEditModelValidatorContext;
    //     }
    //     
    //     rootEditModelValidatorContext.AttachValidatorContext(_leafEditModelValidatorContext);
    //     base.OnActorEditContextChanged(args);
    // }

    /* TODO: Make pluggable */
    // protected override void DeinitializeActorEditContext()
    // {
    //     var editContext = _actorEditContext;
    //     if (editContext is null) {
    //         return;
    //     }
    //
    //     if (!editContext.TryGetEditModelValidatorContext<RootEditModelValidatorContext>(out var rootValidatorContext)) {
    //         throw new InvalidOperationException(
    //             "Root validator context lookup key was removed before the own implementation had the chance to properly detach its validator context.");
    //     }
    //
    //     if (rootValidatorContext.DetachValidatorContext(_leafEditModelValidatorContext)) {
    //         editContext.Properties.Remove(EditModelValidatorContextLookupKey.Standard);
    //     }
    //
    //     base.DeinitializeActorEditContext();
    // }
}
