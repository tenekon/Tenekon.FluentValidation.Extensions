using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
public class EditModelSubpath : EditContextualComponentBase<EditModelSubpath>, IEditContextualComponentTrait, IEditModelSubpathTrait,
    IParameterSetTransitionHandlerRegistryProvider
{
    public static readonly Func<IEditModelSubpathTrait.ErrorContext, Exception> DefaultExceptionFactory = DefaultExceptionFactoryImpl;

    static EditModelSubpath()
    {
        // Because Subpath component is NOT validating, but redirecting plain edit context validation requested notifications,
        // we must not listen to validation requested notifications of root edit context, to prevent double notification.
        ParameterSetTransitionHandlerRegistryProvider<EditModelSubpath>.ParameterSetTransitionHandlerRegistry.RemoveHandler(
            SubscribeToRootEditContextOnValidationRequestedAction);

        ParameterSetTransitionHandlerRegistryProvider<EditModelSubpath>.ParameterSetTransitionHandlerRegistry.RegisterHandler(
            CopyAncestorEditContextFieldReferencesToActorEditContextAction,
            // ISSUE:
            //  The problem is, that root edit context may become the ancestor edit context, then the ancestor edit references of
            //  field states and properties are copied to new actor edit context, thus it is problematic to occupy counter-based
            //  properties before and then deoccupy counter-based properties after the copying.
            // PROPOSAL (IMPLEMETED):
            //  Copy field references before any mutation of any edit context on every parameters transition.
            //
            // TODO: If we want to make the registry API public, we should consider to make adding position relative to already added
            //  handlers, because now it is sufficient to insert the handler at the start to fulfill the contract required by the above
            //  handler.
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

        var component = Unsafe.As<EditModelSubpath>(transition.Component);
        var actorEditContextTransition = transition.ActorEditContext;
        var ancestorEditContextTransition = transition.AncestorEditContext;

        if (actorEditContextTransition.IsNewNull) {
            var lastTransition = Unsafe.As<EditModelSubpathParameterSetTransition>(component.LastParameterSetTransition);
            if (lastTransition is { IsActorEditContextAncestorDerived: true } && ancestorEditContextTransition.IsNewSame) {
                // Reuse old actor edit context if it was already derived from the ancestor and the ancestor didn't change.
                actorEditContextTransition.New = actorEditContextTransition.Old;
            } else {
                // Create a new actor edit context based on the ancestor model.
                var newActorEditContext = new EditContext(ancestorEditContextTransition.New.Model);
                actorEditContextTransition.New = newActorEditContext;

                // Only copy field references if the ancestor is the direct ancestor.
                if (component.Ancestor is { IsDirectAncestor: true }) {
                    // Cascade EditContext._fieldStates
                    var editContextFieldStatesMemberAccessor = EditContextAccessor.EditContextFieldStatesMemberAccessor;
                    var fieldStates = editContextFieldStatesMemberAccessor.GetValue(ancestorEditContextTransition.New);
                    editContextFieldStatesMemberAccessor.SetValue(newActorEditContext, fieldStates);

                    // Cascade EditContext.Properties
                    EditContextAccessor.GetProperties(newActorEditContext) =
                        EditContextAccessor.GetProperties(ancestorEditContextTransition.New);
                }
            }

            var transition2 = Unsafe.As<EditModelSubpathParameterSetTransition>(transition);
            transition2.IsActorEditContextAncestorDerived = true;
        }
    };

    Func<IEditModelSubpathTrait.ErrorContext, Exception> IEditModelSubpathTrait.ExceptionFactory => DefaultExceptionFactory;

    private static Exception DefaultExceptionFactoryImpl(IEditModelSubpathTrait.ErrorContext context) =>
        context.Identifier switch {
            IEditModelSubpathTrait.ErrorIdentifier.ActorEditContextAndModel => new InvalidOperationException(
                $"{context.Provocateur?.GetType().Name} requires a non-null {nameof(Model)} parameter or a non-null {nameof(EditContext)} parameter, but not both."),
            _ => IEditModelSubpathTrait.DefaultExceptionFactory(context)
        };

    private Dictionary<ModelIdentifier, FieldIdentifier>? _subModelAccessPathMap;
    private ModelIdentifier _ancestorEditContextModelIdentifier;
    private IEditModelValidationNotifier? _editModelValidationNotifier;

    [CascadingParameter]
    private IEditModelValidationNotifier? RoutesOwningEditModelValidationNotifier { get; set; }

    [field: AllowNull]
    [field: MaybeNull]
    private AncestorEditContextValidationNotifierImpl AncestorEditContextValidationNotifier =>
        field ??= new AncestorEditContextValidationNotifierImpl(this);

    [Parameter]
    public Ancestor? Ancestor { get; set; }

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public object? Model {
#pragma warning restore BL0007 // Component parameters should be auto properties
        get => ((IEditModelSubpathTrait)this).Model;
        set => ((IEditModelSubpathTrait)this).Model = value;
    }

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public EditContext? EditContext {
#pragma warning restore BL0007 // Component parameters should be auto properties
        get => ((IEditModelSubpathTrait)this).ActorEditContext;
        set => ((IEditModelSubpathTrait)this).SetActorEditContextExplicitly(value);
    }

    bool IEditModelSubpathTrait.HasActorEditContextBeenSetExplicitly { get; set; }
    EditContext? IEditModelSubpathTrait.ActorEditContext { get; set; }
    object? IEditModelSubpathTrait.Model { get; set; }

    EditContext? IEditContextualComponentTrait.ActorEditContext => ((IEditModelSubpathTrait)this).ActorEditContext;

    [Parameter]
    public Expression<Func<object>>[]? Routes { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await ((IEditModelSubpathTrait)this).OnSubpathParametersSetAsync();
        await base.OnParametersSetAsync();
    }

    internal override async Task OnParametersTransitioningAsync()
    {
        await base.OnParametersTransitioningAsync();

        if (RoutesOwningEditModelValidationNotifier is { } validationNotifier) {
            var validationScopeContext = new ValidationScopeContext(LastParameterSetTransition.RootEditContext.New);
            validationNotifier.EvaluateValidationScope(validationScopeContext);
            if (validationScopeContext.IsWithinScope) {
                _editModelValidationNotifier = RoutesOwningEditModelValidationNotifier;
            } else {
                _editModelValidationNotifier = AncestorEditContextValidationNotifier;
            }
        } else {
            _editModelValidationNotifier = AncestorEditContextValidationNotifier;
        }

        InitializeModelRoutes();

        var ancestorEditContextTransition = LastParameterSetTransition.AncestorEditContext;
        if (ancestorEditContextTransition.IsNewSame) {
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

            if (Routes is not { } routes) {
                return;
            }

            foreach (var route in routes) {
                var modelIdentifier = ModelIdentifier.Create(route);
                var modelRoute = FieldIdentifierExtension.WithPropertyPath(route);
                if (!nestedModelRoutes.TryAdd(modelIdentifier, modelRoute)) {
                    throw new InvalidOperationException(
                        $"An enlistment in the {nameof(Routes)} parameter must be unique, ensuring that the type of the target model, regardless of the accessor's path, is not already included.");
                }
            }
        }
    }

    internal override EditContextualComponentBaseParameterSetTransition CreateParameterSetTransition() =>
        new EditModelSubpathParameterSetTransition();

    protected override void OnValidateModel(object? sender, ValidationRequestedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(sender);
        Debug.Assert(_editModelValidationNotifier is not null);
        var validationRequestedArgs = new EditModelModelValidationRequestedArgs(sender, sender);
        _editModelValidationNotifier.NotifyModelValidationRequested(validationRequestedArgs);
    }

    protected override void OnValidateField(object? sender, FieldChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(sender);
        Debug.Assert(_editModelValidationNotifier is not null);

        // Scenario 1: Given () => City.Address.Street, then Model = Address and FieldName = "Street" 
        var subFieldIdentifier = e.FieldIdentifier;
        var subFieldModelIdentifier = new ModelIdentifier(subFieldIdentifier.Model);
        if (subFieldModelIdentifier.Equals(_ancestorEditContextModelIdentifier)) {
            var directFieldValidationRequestArgs = new EditModelDirectFieldValidationRequestedArgs(this, sender, subFieldIdentifier);
            _editModelValidationNotifier.NotifyDirectFieldValidationRequested(directFieldValidationRequestArgs);
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
        var nestedFieldValidationRequestedArgs = new EditModelNestedFieldValidationRequestedArgs(
            this,
            sender,
            fullFieldPath,
            subFieldIdentifier);
        _editModelValidationNotifier.NotifyNestedFieldValidationRequested(nestedFieldValidationRequestedArgs);

        notifyValidationStateChanged:
        LastParameterSetTransition.ActorEditContext.New.NotifyValidationStateChanged();
    }

    private class AncestorEditContextValidationNotifierImpl(EditModelSubpath component) : IEditModelValidationNotifier
    {
        public void EvaluateValidationScope(ValidationScopeContext candidate) => candidate.IsDirectDescendant = true;

        public void NotifyModelValidationRequested(EditModelModelValidationRequestedArgs args) =>
            component.LastParameterSetTransition.AncestorEditContext.New.Validate();

        public void NotifyDirectFieldValidationRequested(EditModelDirectFieldValidationRequestedArgs args) =>
            component.LastParameterSetTransition.AncestorEditContext.New.NotifyFieldChanged(args.FieldIdentifier);

        public void NotifyNestedFieldValidationRequested(EditModelNestedFieldValidationRequestedArgs args) =>
            // Align with default behavior: delegate to ancestor edit context and its validators, which may throw an exception.
            component.LastParameterSetTransition.AncestorEditContext.New.NotifyFieldChanged(args.SubFieldIdentifier);
    }
}
