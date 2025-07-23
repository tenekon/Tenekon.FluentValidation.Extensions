using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class EditModelValidatorRootpath : EditModelValidatorBase<EditModelValidatorRootpath>, IEditContextualComponentTrait,
    IParameterSetTransitionHandlerRegistryProvider
{
    private static readonly object s_editContextModelSentinel = new();

    static EditModelValidatorRootpath()
    {
        ParameterSetTransitionHandlerRegistryProvider<EditModelValidatorRootpath>.ParameterSetTransitionHandlerRegistry.RegisterHandler(
            CopyAncestorEditContextFieldReferencesToActorEditContextAction,
            HandlerInsertPosition.After,
            SetProvidedEditContexts);
    }

    static ParameterSetTransitionHandlerRegistry IParameterSetTransitionHandlerRegistryProvider.ParameterSetTransitionHandlerRegistry {
        get;
    } = new();

    private static Action<EditContextualComponentBaseParameterSetTransition>
        CopyAncestorEditContextFieldReferencesToActorEditContextAction { get; } = static transition => {
        if (transition.IsDisposing) {
            return;
        }

        var transition2 = Unsafe.As<EditModelValidatorBaseParameterSetTransition>(transition);

        if (transition2.AncestorEditContext.IsNewDifferent || transition2.ChildContent.IsNewNullStateChanged ||
            transition2.Routes.IsNewNullStateChanged) {
            // We only isolate actor edit context if ChildContent is null and Routes are not, because if Routes is not null,
            // then Subpath already provides scoped edit context.
            if (transition2.AncestorEditContext.IsNewNonNull && transition2.ChildContent.IsNewNonNull && transition2.Routes.IsNewNull) {
                // We must re-create the sentinel, to allow correct deinitialiazation of possible non-null old ancestor edit context
                var actorEditContext = CreateEditContextSentinel();
                transition.ActorEditContext.New = actorEditContext;
                ParametersTransitioners.CopyAncestorEditContextFieldReferencesToActorEditContextAction(transition);
            }
        } else if (transition2.ActorEditContext.IsOldNonNull) {
            transition.ActorEditContext.New = transition2.ActorEditContext.Old;
        }
    };

    private static EditContext CreateEditContextSentinel() => new(s_editContextModelSentinel);

    EditContext? IEditContextualComponentTrait.ActorEditContext => AncestorEditContext;

    // TODO: Consolidate duplicate code in EditModelValidatorSubpath
    /* TODO: Make pluggable */
    // protected override void OnAncestorEditContextChanged(EditContextChangedEventArgs args)
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
    //     base.OnAncestorEditContextChanged(args);
    // }


    /* TODO: Make pluggable */
    // protected override void DeinitializeAncestorEditContext()
    // {
    //     var editContext = _ancestorEditContext;
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
    //     base.DeinitializeAncestorEditContext();
    // }
}
