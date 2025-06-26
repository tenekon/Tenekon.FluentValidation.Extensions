using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class ComponentValidatorSubpath : ComponentValidatorBase, IEditContextualComponentTrait, IComponentValidatorSubpathTrait
{
    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public object? Model {
#pragma warning restore BL0007 // Component parameters should be auto properties
        get => ((IComponentValidatorSubpathTrait)this).Model;
        set => ((IComponentValidatorSubpathTrait)this).Model = value;
    }

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public EditContext? EditContext {
#pragma warning restore BL0007 // Component parameters should be auto properties
        get => ((IComponentValidatorSubpathTrait)this).OwnEditContext;
        set => ((IComponentValidatorSubpathTrait)this).SetOwnEditContextExplictly(value);
    }

    bool IComponentValidatorSubpathTrait.HasOwnEditContextBeenSetExplicitly { get; set; }
    object? IComponentValidatorSubpathTrait.Model { get; set; }
    EditContext? IComponentValidatorSubpathTrait.OwnEditContext { get; set; }

    EditContext? IEditContextualComponentTrait.OwnEditContext =>
        ((IComponentValidatorSubpathTrait)this).OwnEditContext ?? throw new InvalidOperationException();

    // ReSharper disable once MemberHidesInterfaceMemberWithDefaultImplementation
    protected override  void OnParametersSet()
    {
        ((IComponentValidatorSubpathTrait)this).OnSubpathParametersSet();
        
        var ownEditContext = ((IComponentValidatorSubpathTrait)this).OwnEditContext;
        Debug.Assert(ownEditContext is not null);
        if (Validator is null && ValidatorType is null) {
            ValidatorType = typeof(IValidator<>).MakeGenericType(ownEditContext.Model.GetType());
        }

        base.OnParametersSet();
    }
    
    /* TODO: Make pluggable */
    // protected override void OnOwnEditContextChanged(EditContextChangedEventArgs args)
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
    //     base.OnOwnEditContextChanged(args);
    // }
    
    /* TODO: Make pluggable */
    // protected override void DeinitializeOwnEditContext()
    // {
    //     var editContext = _ownEditContext;
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
    //     base.DeinitializeOwnEditContext();
    // }
}
