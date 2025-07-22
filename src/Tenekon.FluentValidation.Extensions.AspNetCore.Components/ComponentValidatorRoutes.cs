using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

file static class EditContextAccessor
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Properties>k__BackingField")]
    public static extern ref EditContextProperties GetProperties(EditContext editContext);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Model>k__BackingField")]
    public static extern ref object GetModel(EditContext editContext);
}

// This component is highly specialized and tighly coupled with the internals of EditContext.
// The component wants to have an actor edit context with field references of the ancestor edit context, except the event invocations,
//   because the components alone wants to act on OnFieldChanged and OnValidationRequested independently from the ancestor edit context.
// CASCADING PARAMETER DEPENDENCIES:
//   IComponentValidator provided by ComponentValidatorSubpath or ComponentValidatorRootpath
public class ComponentValidatorRoutes : EditContextualComponentBase<ComponentValidatorRoutes>, IEditContextualComponentTrait,
    IHandlingParametersTransition
{
    static ParametersTransitionHandlerRegistry IHandlingParametersTransition.ParametersTransitionHandlerRegistry { get; } = new();

    static ComponentValidatorRoutes()
    {
        HandlingParametersTransitionAccessor<ComponentValidatorRoutes>.ParametersTransitionHandlerRegistry.RemoveHandler(
            SubscribeToRootEditContextOnValidationRequestedAction);

        HandlingParametersTransitionAccessor<ComponentValidatorRoutes>.ParametersTransitionHandlerRegistry.RegisterHandler(
            CopyAncestorEditContextFieldReferencesToActorEditContext,
            // If we want to make the registry API public, we should consider to make adding position relative to already added handlers,
            // because now it is sufficient to insert the handler at the start to fulfill the contract required by the above handler.
            HandlerAddingPosition.Start);

        return;

        static void CopyAncestorEditContextFieldReferencesToActorEditContext(ParametersTransition transition)
        {
            var ancestor = transition.AncestorEditContextTransition;
            var actor = transition.ActorEditContextTransition;

            // We assume that actor edit context never changes only once at first transition.

            if (ancestor.IsOldReferenceDifferentToNew || actor.IsFirstTransition) {
                if (ancestor.IsNewNonNull && actor.IsNewNonNull) {
                    // ISSUE:
                    //  The problem is, that root edit context may become the ancestor edit context, then the ancestor edit references of
                    //  field states and properties are copied to new actor edit context, thus it is problematic to occupy counter-based
                    //  properties before and then deoccupy counter-based properties after the copying.
                    // PROPOSAL (IMPLEMETED):
                    //  Copy field references before any mutation of any edit context on every parameters transition.

                    // Cascade EditContext._fieldStates
                    var editContextFieldStatesMemberAccessor = EditContextFieldStatesMemberAccessor;
                    var fieldStates = editContextFieldStatesMemberAccessor.GetValue(ancestor.New);
                    editContextFieldStatesMemberAccessor.SetValue(actor.New, fieldStates);

                    // Cascade EditContext.Properties
                    EditContextAccessor.GetProperties(actor.New) = EditContextAccessor.GetProperties(ancestor.New);

                    // Cascade EditContext.Model
                    EditContextAccessor.GetModel(actor.New) = EditContextAccessor.GetModel(ancestor.New);

                    actor.InvalidateCache();
                }
            }
        }
    }

    private const string EditContextFieldStatesFieldName = "_fieldStates";

    private static readonly object s_modelSentinel = new();

    // We cannot use UnsafeAccessor and must work with reflection because part of the targeting signature is internal. :/
    [field: AllowNull]
    [field: MaybeNull]
    private static FieldInfo EditContextFieldStatesMemberAccessor =>
        field ??= typeof(EditContext).GetField(EditContextFieldStatesFieldName, BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new NotImplementedException(
                $"{nameof(EditContext)} does not implement the {EditContextFieldStatesFieldName} field anymore.");

    private Dictionary<ModelIdentifier, FieldIdentifier>? _subModelAccessPathMap;
    private ModelIdentifier _ancestorEditContextModelIdentifier;

    [CascadingParameter]
    private IComponentValidationNotifier? RoutesOwningComponentValidationNotifier { get; set; }

    [Parameter]
    [EditorRequired]
    public Expression<Func<object>>[]? Routes { get; set; }

    EditContext? IEditContextualComponentTrait.ActorEditContext { get; } = new(s_modelSentinel);

    protected override async Task OnParametersSetAsync()
    {
        if (RoutesOwningComponentValidationNotifier is null) {
            throw new InvalidOperationException(
                $"{GetType().Name} requires a non-null cascading parameter of type {typeof(IComponentValidationNotifier)}, internally provided by e.g. {nameof(ComponentValidatorRootpath)} or {nameof(ComponentValidatorSubpath)}.");
        }

        if (Routes is null) {
            throw new InvalidOperationException($"{GetType().Name} requires a {nameof(Routes)} parameter.");
        }

        await base.OnParametersSetAsync();
    }

    internal override async Task OnParametersTransitioningAsync()
    {
        await base.OnParametersTransitioningAsync();

        Debug.Assert(RoutesOwningComponentValidationNotifier is not null);

        if (!RoutesOwningComponentValidationNotifier.IsInScope(LastParametersTransition.RootEditContextTransition.New)) {
            throw new InvalidOperationException(
                $"{GetType().Name} has a cascading parameter of type {nameof(IComponentValidationNotifier)}, but the cascaded component validation notifier operates in a different scope than this component.");
        }

        InitializeModelRoutes();

        var ancestorEditContextTransition = LastParametersTransition.AncestorEditContextTransition;
        if (ancestorEditContextTransition.IsOldReferenceEqualsToNew) {
            _ancestorEditContextModelIdentifier = new ModelIdentifier(ancestorEditContextTransition.New.Model);
        }

        return;

        void InitializeModelRoutes()
        {
            if (_subModelAccessPathMap is not { } nestedModelRoutes) {
                _subModelAccessPathMap = nestedModelRoutes = [];
            } else {
                nestedModelRoutes.Clear();
            }

            Debug.Assert(Routes is not null);
            foreach (var route in Routes) {
                var modelIdentifier = ModelIdentifier.Create(route);
                var modelRoute = FieldIdentifierExtension.WithPropertyPath(route);
                if (!nestedModelRoutes.TryAdd(modelIdentifier, modelRoute)) {
                    throw new InvalidOperationException(
                        $"An enlistment in the {nameof(Routes)} parameter must be unique, ensuring that the type of the target model, regardless of the accessor's path, is not already included.");
                }
            }
        }
    }

    protected override void OnValidateModel(object? sender, ValidationRequestedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(sender);
        Debug.Assert(RoutesOwningComponentValidationNotifier is not null);
        var componentValidator = RoutesOwningComponentValidationNotifier;
        var validationRequestedArgs = new ComponentValidatorModelValidationRequestedArgs(sender, sender);
        componentValidator.NotifyModelValidationRequested(validationRequestedArgs);
    }

    protected override void OnValidateField(object? sender, FieldChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(sender);
        Debug.Assert(RoutesOwningComponentValidationNotifier is not null);
        var componentValidator = RoutesOwningComponentValidationNotifier;

        // Scenario 1: Given () => City.Address.Street, then Model = Address and FieldName = "Street" 
        var subFieldIdentifier = e.FieldIdentifier;
        var subFieldModelIdentifier = new ModelIdentifier(subFieldIdentifier.Model);
        if (subFieldModelIdentifier.Equals(_ancestorEditContextModelIdentifier)) {
            var directFieldValidationRequestArgs = new ComponentValidatorDirectFieldValidationRequestedArgs(
                this,
                sender,
                subFieldIdentifier);
            componentValidator.NotifyDirectFieldValidationRequested(directFieldValidationRequestArgs);
            goto notifyValidationStateChanged;
        }

        Debug.Assert(_subModelAccessPathMap is not null);
        // Scenario 1: Using Address (Model) as the ModelIdentifier key,
        //  get the model route with City as Model and "City.Address" as FieldName
        if (!_subModelAccessPathMap.TryGetValue(subFieldModelIdentifier, out var subModelAccessPath)) {
            throw new InvalidOperationException(
                $"The model of type {subFieldModelIdentifier.Model.GetType()} is unrecognized. Is it registered as a potencial route?");
        }

        // Scenario 1: Concenate City.Address and Street
        var fullFieldPathString = $"{subModelAccessPath.FieldName}.{subFieldIdentifier.FieldName}";
        // Scenario 1: Build a FieldIdentifier with City as the Model and City.Address.Street as the FieldName
        var fullFieldPath = new FieldIdentifier(subModelAccessPath.Model, fullFieldPathString);
        var nestedFieldValidationRequestedArgs = new ComponentValidatorNestedFieldValidationRequestedArgs(
            this,
            sender,
            fullFieldPath,
            subFieldIdentifier);
        componentValidator.NotifyNestedFieldValidationRequested(nestedFieldValidationRequestedArgs);

        notifyValidationStateChanged:
        LastParametersTransition.ActorEditContextTransition.New.NotifyValidationStateChanged();
    }
}
