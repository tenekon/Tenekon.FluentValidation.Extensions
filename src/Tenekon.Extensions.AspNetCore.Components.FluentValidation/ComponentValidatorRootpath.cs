using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class ComponentValidatorRootpath : ComponentValidatorBase, IEditContextualComponentTrait
{
    EditContext? IEditContextualComponentTrait.OwnEditContext => SuperEditContext;

    // TODO: Consolidate duplicate code in ComponentValidatorSubpath
    /* TODO: Make pluggable */
    // protected override void OnSuperEditContextChanged(EditContextChangedEventArgs args)
    // {
    //     RootComponentValidatorContext rootComponentValidatorContext;
    //     if (args.New.Properties.TryGetValue(ComponentValidatorContextLookupKey.Standard, out var validatorContext)) {
    //         if (validatorContext is not RootComponentValidatorContext rootValidatorContext2) {
    //             throw new InvalidOperationException("Root validator context lookup key was misued from a third-party.");
    //         }
    //         rootComponentValidatorContext = rootValidatorContext2;
    //     } else {
    //         rootComponentValidatorContext = new RootComponentValidatorContext();
    //         args.New.Properties[ComponentValidatorContextLookupKey.Standard] = rootComponentValidatorContext;
    //     }
    //
    //     rootComponentValidatorContext.AttachValidatorContext(_leafComponentValidatorContext);
    //     base.OnSuperEditContextChanged(args);
    // }


    /* TODO: Make pluggable */
    // protected override void DeinitializeSuperEditContext()
    // {
    //     var editContext = _superEditContext;
    //     if (editContext is null) {
    //         return;
    //     }
    //
    //     if (!editContext.TryGetComponentValidatorContext<RootComponentValidatorContext>(out var rootValidatorContext)) {
    //         throw new InvalidOperationException(
    //             "Root validator context lookup key was removed before the own implementation had the chance to properly detach its validator context.");
    //     }
    //
    //     if (rootValidatorContext.DetachValidatorContext(_leafComponentValidatorContext)) {
    //         editContext.Properties.Remove(ComponentValidatorContextLookupKey.Standard);
    //     }
    //
    //     base.DeinitializeSuperEditContext();
    // }
}
