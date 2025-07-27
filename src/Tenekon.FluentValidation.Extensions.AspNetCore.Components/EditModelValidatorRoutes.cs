using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

// This component is highly specialized and tighly coupled with the internals of EditContext.
// The component wants to have an actor edit context with field references of the ancestor edit context, except the event invocations,
//   because the components alone wants to act on OnFieldChanged and OnValidationRequested independently from the ancestor edit context.
// CASCADING PARAMETER DEPENDENCIES:
//   IEditModelValidator provided by EditModelValidatorSubpath or EditModelValidatorRootpath
public class EditModelValidatorRoutes : EditModelScopeBase<EditModelValidatorRoutes>, IParameterSetTransitionHandlerRegistryProvider
{
    static EditModelValidatorRoutes()
    {
        RuntimeHelpers.RunClassConstructor(typeof(EditModelScopeBase<EditModelValidatorRoutes>).TypeHandle);

        ParameterSetTransitionHandlerRegistryProvider<EditModelValidatorRoutes>.ParameterSetTransitionHandlerRegistry.RegisterHandler(
            SetRoutesOwningEditModelValidationNotifier,
            HandlerInsertPosition.After);
    }

    static ParameterSetTransitionHandlerRegistry IParameterSetTransitionHandlerRegistryProvider.ParameterSetTransitionHandlerRegistry {
        get;
    } = new();

    private static Action<EditContextualComponentBaseParameterSetTransition> SetRoutesOwningEditModelValidationNotifier { get; } =
        static transition => {
            if (transition.IsDisposing) {
                return;
            }

            var component = Unsafe.As<EditModelValidatorRoutes>(transition.Component);
            var transition2 = Unsafe.As<EditModelSubpathParameterSetTransition>(transition);

            if (transition.RootEditContext.IsNewDifferent) {
                if (component.AncestorEditModelValidationNotifier is { } validationNotifier) {
                    var validationScopeContext = new ValidationScopeContext(transition.RootEditContext.New);
                    validationNotifier.EvaluateValidationScope(validationScopeContext);
                    if (validationScopeContext.IsWithinScope) {
                        transition2.RoutesOwningEditModelValidationNotifier.New = validationNotifier;
                    } else {
                        throw new InvalidOperationException(
                            $"{component.GetType().Name} has a cascading validation notifier, but it operates in a different scope than this component.");
                    }
                } else {
                    throw new InvalidOperationException(
                        $"{component.GetType().Name} requires a non-null cascading validation notifier, internally provided by e.g. {nameof(EditModelValidatorRootpath)} or {nameof(EditModelValidatorSubpath)}.");
                }
            } else if (transition2.RoutesOwningEditModelValidationNotifier.IsUnitialized) {
                throw new InvalidOperationException("missing root edit context");
            } else {
                transition2.RoutesOwningEditModelValidationNotifier.New = transition2.RoutesOwningEditModelValidationNotifier.Old;
            }
        };

    private Dictionary<ModelIdentifier, FieldIdentifier>? _nestedFieldModelAccessPathMap;
    private ModelIdentifier _ancestorEditContextModelIdentifier;

    [CascadingParameter]
    private IEditModelValidationNotifier? AncestorEditModelValidationNotifier { get; set; }

    internal override EditModelSubpathParameterSetTransition LastParameterSetTransition =>
        Unsafe.As<EditModelSubpathParameterSetTransition>(Unsafe.As<ILastParameterSetTransitionTrait>(this).LastParameterSetTransition);

    [Parameter]
    public Expression<Func<object>>[]? Routes { get; set; }

    internal override async Task OnParametersTransitioningAsync()
    {
        await base.OnParametersTransitioningAsync();

        InitializeModelRoutes();

        var ancestorEditContextTransition = LastParameterSetTransition.AncestorEditContext;
        if (ancestorEditContextTransition.IsNewSame) {
            _ancestorEditContextModelIdentifier = new ModelIdentifier(ancestorEditContextTransition.New.Model);
        }

        return;

        void InitializeModelRoutes()
        {
            if (_nestedFieldModelAccessPathMap is not { } nestedFieldModelAccessPathMap) {
                _nestedFieldModelAccessPathMap = nestedFieldModelAccessPathMap = [];
            } else {
                nestedFieldModelAccessPathMap.Clear();
            }

            if (Routes is not { } routes) {
                return;
            }

            foreach (var route in routes) {
                var nestedFieldModelIdentifier = ModelIdentifier.ResolveFromAccessPath(route);
                var nestedFieldModelAccessPath = FieldIdentifierExtension.CreateFullAccessPath(route);
                if (!nestedFieldModelAccessPathMap.TryAdd(nestedFieldModelIdentifier, nestedFieldModelAccessPath)) {
                    throw new InvalidOperationException(
                        $"An enlistment in the {nameof(Routes)} parameter must be unique, ensuring that the type of the target model, regardless of the accessor's path, is not already included.");
                }
            }
        }
    }

    internal override EditContextualComponentBaseParameterSetTransition CreateParameterSetTransition() =>
        new EditModelSubpathParameterSetTransition();

    internal override void OnValidateModel(object? sender, ValidationRequestedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(sender);
        Debug.Assert(LastParameterSetTransition.RoutesOwningEditModelValidationNotifier is not null);
        var validationRequestedArgs = new EditModelModelValidationRequestedArgs(sender, sender);
        LastParameterSetTransition.RoutesOwningEditModelValidationNotifier.New.NotifyModelValidationRequested(validationRequestedArgs);
    }

    internal override void OnValidateField(object? sender, FieldChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(sender);

        // Scenario 1: Given () => City.Address.Street, then Model = Address and FieldName = "Street" 
        var nestedFieldIdentifier = e.FieldIdentifier;
        var nestedFieldModelIdentifier = new ModelIdentifier(nestedFieldIdentifier.Model);
        if (nestedFieldModelIdentifier.Equals(_ancestorEditContextModelIdentifier)) {
            var directFieldValidationRequestArgs = new EditModelDirectFieldValidationRequestedArgs(this, sender, nestedFieldIdentifier);

            LastParameterSetTransition.RoutesOwningEditModelValidationNotifier.New.NotifyDirectFieldValidationRequested(
                directFieldValidationRequestArgs);

            goto notifyValidationStateChanged;
        }

        Debug.Assert(_nestedFieldModelAccessPathMap is not null);

        // Scenario 1: Using Address (Model) as the ModelIdentifier key,
        //  get the model route with City as Model and "City.Address" as FieldName
        if (!_nestedFieldModelAccessPathMap.TryGetValue(nestedFieldModelIdentifier, out var nestedFieldModelAccessPath)) {
            throw new InvalidOperationException(
                $"The model of type {nestedFieldModelIdentifier.Model.GetType()} is unrecognized. Is it registered as a potencial route?");
        }

        // Scenario 1: Concenate City.Address and Street
        var fullFieldPathString = $"{nestedFieldModelAccessPath.FieldName}.{nestedFieldIdentifier.FieldName}";

        // Scenario 1: Build a FieldIdentifier with City as the Model and City.Address.Street as the FieldName
        var fullFieldPath = new FieldIdentifier(nestedFieldModelAccessPath.Model, fullFieldPathString);

        var nestedFieldValidationRequestedArgs = new EditModelNestedFieldValidationRequestedArgs(
            this,
            sender,
            fullFieldPath,
            nestedFieldIdentifier);

        LastParameterSetTransition.RoutesOwningEditModelValidationNotifier.New.NotifyNestedFieldValidationRequested(
            nestedFieldValidationRequestedArgs);

        notifyValidationStateChanged:
        LastParameterSetTransition.ActorEditContext.New.NotifyValidationStateChanged();
    }
}
