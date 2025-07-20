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
        HandlingParametersTransitionAccessor<ComponentValidatorRoutes>.ParametersTransitionHandlerRegistry.RegisterHandler(
            CopyAncestorEditContextFieldReferencesToActorEditContext,
            // If we want to make the registry API public, we should consider to make adding position relative to already added handlers,
            // because now it is sufficient to insert the handler at the start to fulfill the contract required by the above handler.
            HandlerAddingPosition.Start);

        return;

        static void CopyAncestorEditContextFieldReferencesToActorEditContext(ParametersTransition transition)
        {
            // We assume that actor edit context never changes only once at first transition.

            if (transition.AncestorContextTransition.IsOldReferenceDifferentToNew || transition.ActorContextTransition.IsFirstTransition) {
                if (transition.AncestorContextTransition.IsNewNonNull && transition.ActorContextTransition.IsNewNonNull) {
                    var newAncestorEditContext = transition.AncestorContextTransition.New;
                    var newActorEditContext = transition.ActorContextTransition.New;

                    // ISSUE:
                    //  The problem is, that root edit context may become the ancestor edit context, then the ancestor edit references of
                    //  field states and properties are copied to new actor edit context, thus it is problematic to occupy counter-based
                    //  properties before and then deoccupy counter-based properties after the copying.
                    // PROPOSAL (IMPLEMETED):
                    //  Copy field references before any mutation of any edit context on every parameters transition.

                    // Cascade EditContext._fieldStates
                    var editContextFieldStatesMemberAccessor = EditContextFieldStatesMemberAccessor;
                    var fieldStates = editContextFieldStatesMemberAccessor.GetValue(newAncestorEditContext);
                    editContextFieldStatesMemberAccessor.SetValue(newActorEditContext, fieldStates);

                    // Cascade EditContext.Properties
                    EditContextAccessor.GetProperties(newActorEditContext) = EditContextAccessor.GetProperties(newAncestorEditContext);

                    // Cascade EditContext.Model
                    EditContextAccessor.GetModel(newActorEditContext) = EditContextAccessor.GetModel(newAncestorEditContext);
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

    private Dictionary<ModelIdentifier, FieldIdentifier>? _modelRoutes;
    private ModelIdentifier _ancestorEditContextModelIdentifier;

    [CascadingParameter]
    private IComponentValidator? RoutesOwningComponentValidator { get; set; }

    [Parameter]
    [EditorRequired]
    public Expression<Func<object>>[]? Routes { get; set; }

    EditContext? IEditContextualComponentTrait.ActorEditContext { get; } = new(s_modelSentinel);

    protected override async Task OnParametersSetAsync()
    {
        if (RoutesOwningComponentValidator is null) {
            throw new InvalidOperationException(
                $"{GetType().Name} requires a non-null cascading parameter of type {typeof(IComponentValidator)}, internally provided by e.g. {nameof(ComponentValidatorRootpath)} or {nameof(ComponentValidatorSubpath)}.");
        }

        if (Routes is null) {
            throw new InvalidOperationException($"{GetType().Name} requires a {nameof(Routes)} parameter.");
        }

        await base.OnParametersSetAsync();

        InitializeModelRoutes();

        Debug.Assert(_ancestorEditContext is not null);
        if (_ancestorEditContextChangedSinceLastParametersSet) {
            _ancestorEditContextModelIdentifier = new ModelIdentifier(_ancestorEditContext.Model);
        }

        return;

        void InitializeModelRoutes()
        {
            if (_modelRoutes is not { } nestedModelRoutes) {
                _modelRoutes = nestedModelRoutes = [];
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
        // We must NOOP-out the handler, because we already bubble up the model validation request of actor edit context to
        // ancestor edit context and we do not want 
        
        ArgumentNullException.ThrowIfNull(sender);
        Debug.Assert(RoutesOwningComponentValidator is not null);
        var componentValidator = RoutesOwningComponentValidator;
        var validationRequestArgs = new ComponentValidatorModelValidationRequestArgs(sender, sender);
        componentValidator.ValidateModel(this, validationRequestArgs);
    }

    protected override void OnValidateField(object? sender, FieldChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(sender);
        Debug.Assert(RoutesOwningComponentValidator is not null);
        var componentValidator = RoutesOwningComponentValidator;

        // Scenario 1: Given () => City.Address.Street, then Model is Address and Street is FieldName 
        var directFieldIdentifier = e.FieldIdentifier;
        var directModelIdentifier = new ModelIdentifier(directFieldIdentifier.Model);
        if (directModelIdentifier.Equals(_ancestorEditContextModelIdentifier)) {
            var directFieldValidationRequestArgs = new ComponentValidatorDirectFieldValidationRequestArgs(
                sender,
                sender,
                directFieldIdentifier);
            componentValidator.ValidateDirectField(this, directFieldValidationRequestArgs);
            goto notifyValidationStateChanged;
        }

        Debug.Assert(_modelRoutes is not null);
        // Scenario 1: Using Address as the ModelIdentifier key,
        //  get the model route with City as the Model and City.Address as the FieldName
        if (!_modelRoutes.TryGetValue(directModelIdentifier, out var nestedModelRoute)) {
            throw new InvalidOperationException(
                $"The model of type {directModelIdentifier.Model.GetType()} is unrecognized. Is it registered as a potencial route?");
        }

        // Scenario 1: Concenate City.Address and Street
        var nestedFieldPath = $"{nestedModelRoute.FieldName}.{directFieldIdentifier.FieldName}";
        // Scenario 1: Build a FieldIdentifier with City as the Model and City.Address.Street as the FieldName
        var nestedFieldIdentifier = new FieldIdentifier(nestedModelRoute.Model, nestedFieldPath);
        var nestedFieldValidationRequestArgs = new ComponentValidatorNestedFieldValidationRequestArgs(
            sender,
            sender,
            directFieldIdentifier,
            nestedFieldIdentifier);
        componentValidator.ValidateNestedField(this, nestedFieldValidationRequestArgs);

        notifyValidationStateChanged:
        Debug.Assert(_actorEditContext is not null);
        _actorEditContext.NotifyValidationStateChanged();
    }
}
