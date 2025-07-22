using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class EditModelValidatorRootpath : EditModelValidatorBase, IEditContextualComponentTrait
{
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
