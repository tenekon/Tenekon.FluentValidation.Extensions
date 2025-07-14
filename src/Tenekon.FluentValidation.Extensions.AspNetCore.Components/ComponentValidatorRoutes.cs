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
}

// This component is highly specialized and tighly coupled with the internals of EditContext.
// The component must be the owner of super EditContext's event invocations, but does not want to be the owner of super EditContext
public class ComponentValidatorRoutes : EditContextualComponentBase<ComponentValidatorRoutes>, IEditContextualComponentTrait,
    IComponentValidatorSubpathTrait, IHandlingParametersTransition
{
    static ParametersTransitionHandlerRegistry IHandlingParametersTransition.ParametersTransitionHandlerRegistry { get; } = new();

    static ComponentValidatorRoutes()
    {
        HandlingParametersTransitionAccessor<ComponentValidatorRoutes>.ParametersTransitionHandlerRegistry.RegisterHandler(
            CopyRootEditContextFieldReferencesToSuperEditContext,
            // If we want to make the registry API public, we should consider to make adding position relative to already added handlers,
            // because now it is sufficient to insert the handler at the start to fulfill the contract required by the above handler.
            HandlerAddingPosition.Start);

        return;

        static void CopyRootEditContextFieldReferencesToSuperEditContext(ParametersTransition transition)
        {
            // We assume that own edit context never changes only once at first transition.

            if (transition.SuperContextTransition.IsOldReferenceDifferentToNew || transition.OwnContextTransition.IsFirstTransition) {
                if (transition.SuperContextTransition.IsNewNonNull && transition.OwnContextTransition.IsNewNonNull) {
                    var newSuperEditContext = transition.SuperContextTransition.New;
                    var newOwnEditContext = transition.OwnContextTransition.New;

                    // ISSUE:
                    //  The problem is, that root edit context may become super edit context, then super edit references of field states
                    //  and properties are copied to new own edit context, thus it is problematic to occupy counter-based properties before
                    //  and then deoccupy counter-based properties after the copying.
                    // SOLUTION:
                    //  Copy field references before any mutation of any edit context on every parameters transition.

                    // Cascade EditContext._fieldStates
                    var editContextFieldStatesMemberAccessor = EditContextFieldStatesMemberAccessor;
                    var fieldStates = editContextFieldStatesMemberAccessor.GetValue(newSuperEditContext);
                    editContextFieldStatesMemberAccessor.SetValue(newOwnEditContext, fieldStates);

                    // Cascade EditContext.Properties
                    EditContextAccessor.GetProperties(newOwnEditContext) = EditContextAccessor.GetProperties(newSuperEditContext);
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
    private ModelIdentifier _superEditContextModelIdentifier;

    [CascadingParameter]
    private IComponentValidator? RoutesOwningComponentValidator { get; set; }

    [Parameter]
    [EditorRequired]
    public Expression<Func<object>>[]? Routes { get; set; }

    EditContext? IEditContextualComponentTrait.OwnEditContext =>
        ((IComponentValidatorSubpathTrait)this).OwnEditContext ?? throw new InvalidOperationException();

    bool IComponentValidatorSubpathTrait.HasOwnEditContextBeenSetExplicitly { get; set; }

    // The sentinel ensures always having a new created EditContext.
    object? IComponentValidatorSubpathTrait.Model { get; set; } = s_modelSentinel;

    EditContext? IComponentValidatorSubpathTrait.OwnEditContext { get; set; }

    protected override void OnParametersSet()
    {
        if (RoutesOwningComponentValidator is null) {
            throw new InvalidOperationException(
                $"{GetType().Name} requires a non-null cascading parameter of type {typeof(IComponentValidator)}, internally provided by e.g. {nameof(ComponentValidatorRootpath)} or {nameof(ComponentValidatorSubpath)}.");
        }

        if (Routes is null) {
            throw new InvalidOperationException($"{GetType().Name} requires a {nameof(Routes)} parameter.");
        }

        ((IComponentValidatorSubpathTrait)this).OnSubpathParametersSet();
        base.OnParametersSet();

        InitializeModelRoutes();

        Debug.Assert(_superEditContext is not null);
        if (_superEditContextChangedSinceLastParametersSet) {
            _superEditContextModelIdentifier = new ModelIdentifier(_superEditContext.Model);
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

    protected override void OnValidateModel(object? sender, ValidationRequestedEventArgs e)
    {
        Debug.Assert(RoutesOwningComponentValidator is not null);
        var componentValidator = RoutesOwningComponentValidator;
        componentValidator.ValidateModel();
    }

    protected override void OnValidateField(object? sender, FieldChangedEventArgs e)
    {
        Debug.Assert(RoutesOwningComponentValidator is not null);
        var componentValidator = RoutesOwningComponentValidator;

        // Scenario 1: Given () => City.Address.Street, then Model is Address and Street is FieldName 
        var directFieldIdentifier = e.FieldIdentifier;
        var directModelIdentifier = new ModelIdentifier(directFieldIdentifier.Model);
        if (directModelIdentifier.Equals(_superEditContextModelIdentifier)) {
            componentValidator.ValidateDirectField(directFieldIdentifier);
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
        componentValidator.ValidateNestedField(directFieldIdentifier, nestedFieldIdentifier);

        notifyValidationStateChanged:
        Debug.Assert(_ownEditContext is not null);
        _ownEditContext.NotifyValidationStateChanged();
    }
}
