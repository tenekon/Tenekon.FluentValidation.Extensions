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
    private const string EditContextFieldStatesFieldName = "_fieldStates";

    // We cannot use UnsafeAccessor and must work with reflection because part of the targeting signature is internal. :/
    [field: AllowNull]
    [field: MaybeNull]
    public static FieldInfo EditContextFieldStatesMemberAccessor =>
        field ??= typeof(EditContext).GetField(EditContextFieldStatesFieldName, BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new NotImplementedException(
                $"{nameof(EditContext)} does not implement the {EditContextFieldStatesFieldName} field anymore.");

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Properties>k__BackingField")]
    public static extern ref EditContextProperties GetProperties(EditContext editContext);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Model>k__BackingField")]
    public static extern ref object GetModel(EditContext editContext);
}

// This component is highly specialized and tighly coupled with the internals of EditContext.
// The component wants to have an actor edit context with field references of the ancestor edit context, except the event invocations,
//   because the components alone wants to act on OnFieldChanged and OnValidationRequested independently from the ancestor edit context.
// CASCADING PARAMETER DEPENDENCIES:
//   IEditModelValidator provided by EditModelValidatorSubpath or EditModelValidatorRootpath
public class EditModelSubpath : EditContextualComponentBase<EditModelSubpath>, IEditContextualComponentTrait, IEditModelSubpathTrait,
    IHandlingParametersTransition
{
    static ParametersTransitionHandlerRegistry IHandlingParametersTransition.ParametersTransitionHandlerRegistry { get; } = new();

    static EditModelSubpath()
    {
        HandlingParametersTransitionAccessor<EditModelSubpath>.ParametersTransitionHandlerRegistry.RemoveHandler(
            SubscribeToRootEditContextOnValidationRequestedAction);

        HandlingParametersTransitionAccessor<EditModelSubpath>.ParametersTransitionHandlerRegistry.RegisterHandler(
            CopyAncestorEditContextFieldReferencesToActorEditContext,
            // If we want to make the registry API public, we should consider to make adding position relative to already added handlers,
            // because now it is sufficient to insert the handler at the start to fulfill the contract required by the above handler.
            HandlerAddingPosition.Start);

        return;

        static void CopyAncestorEditContextFieldReferencesToActorEditContext(ParametersTransition transition)
        {
            var component = (EditModelSubpath)transition.EditContextualComponentBase;
            if (component._useEditContextSentinel) {
                return;
            }

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
                    var editContextFieldStatesMemberAccessor = EditContextAccessor.EditContextFieldStatesMemberAccessor;
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

    private static readonly object s_modelSentinel = new();

    private static Exception DefaultExceptionFactoryImpl(IEditModelSubpathTrait.ErrorContext context) =>
        context.Identifier switch {
            IEditModelSubpathTrait.ErrorIdentifier.ActorEditContextAndModel => new InvalidOperationException(
                $"{context.Provocateur?.GetType().Name} requires a non-null {nameof(Model)} parameter or a non-null {nameof(EditContext)} parameter, but not both."),
            _ => IEditModelSubpathTrait.DefaultExceptionFactory(context)
        };

    public static readonly Func<IEditModelSubpathTrait.ErrorContext, Exception> DefaultExceptionFactory = DefaultExceptionFactoryImpl;

    Func<IEditModelSubpathTrait.ErrorContext, Exception> IEditModelSubpathTrait.ExceptionFactory => DefaultExceptionFactory;

    private EditContext? ActorEditContextSentinel => field ??= new EditContext(s_modelSentinel);
    private Dictionary<ModelIdentifier, FieldIdentifier>? _subModelAccessPathMap;
    private ModelIdentifier _ancestorEditContextModelIdentifier;
    private bool _useEditContextSentinel;
    private IEditModelValidationNotifier? _editModelValidationNotifier;

    [CascadingParameter]
    private IEditModelValidationNotifier? RoutesOwningEditModelValidationNotifier { get; set; }

    [field: AllowNull]
    [field: MaybeNull]
    private AncestorEditContextValidatdionNotifier AncestorEditContextValidationNotifier =>
        field ??= new AncestorEditContextValidatdionNotifier(this);

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
    object? IEditModelSubpathTrait.Model { get; set; }
    EditContext? IEditModelSubpathTrait.ActorEditContext { get; set; }

    EditContext? IEditContextualComponentTrait.ActorEditContext =>
        ((IEditModelSubpathTrait)this).ActorEditContext ?? ActorEditContextSentinel;

    bool IEditModelSubpathTrait.IsActorEditContextComposable {
        set => _useEditContextSentinel = value;
    }

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

        if (RoutesOwningEditModelValidationNotifier is { } validationNotifier &&
            validationNotifier.IsInScope(LastParametersTransition.RootEditContextTransition.New)) {
            _editModelValidationNotifier = RoutesOwningEditModelValidationNotifier;
        } else {
            _editModelValidationNotifier = AncestorEditContextValidationNotifier;
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
        LastParametersTransition.ActorEditContextTransition.New.NotifyValidationStateChanged();
    }

    private class AncestorEditContextValidatdionNotifier(EditModelSubpath component) : IEditModelValidationNotifier
    {
        public bool IsInScope(EditContext candidate) => true;

        public void NotifyModelValidationRequested(EditModelModelValidationRequestedArgs args) =>
            component.LastParametersTransition.AncestorEditContextTransition.New.Validate();

        public void NotifyDirectFieldValidationRequested(EditModelDirectFieldValidationRequestedArgs args) =>
            component.LastParametersTransition.AncestorEditContextTransition.New.NotifyFieldChanged(args.FieldIdentifier);

        public void NotifyNestedFieldValidationRequested(EditModelNestedFieldValidationRequestedArgs args) =>
            // Align with default behavior: delegate to ancestor edit context and its validators, which may throw an exception.
            component.LastParametersTransition.AncestorEditContextTransition.New.NotifyFieldChanged(args.SubFieldIdentifier);
    }
}
