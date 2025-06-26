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
// The component must be the owner of EditContext's event invocations, but does not want to be the owner of EditContext
public class ComponentValidatorRoutes : EditContextualComponentBase, IEditContextualComponentTrait, IComponentValidatorSubpathTrait
{
    private const string EditContextFieldStatesFieldName = "_fieldStates";

    private static readonly object s_modelSentinel = new();

    // We cannot use UnsafeAccessor and must work with reflection because part of the targeting signature is internal. :/
    [field: AllowNull]
    [field: MaybeNull]
    private static FieldInfo EditContextFieldStatesMemberAccessor =>
        field ??= typeof(EditContext).GetField(EditContextFieldStatesFieldName, BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new NotImplementedException(
                $"{nameof(EditContext)} does not implement the {EditContextFieldStatesFieldName} field anymore.");

    private Dictionary<ModelIdentifier, FieldIdentifier>? _modelFieldIdentifiers;
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

        InitializeModelFieldIdentifiers();

        Debug.Assert(_superEditContext is not null);
        if (_superEditContextChangedSinceLastParametersSet) {
            _superEditContextModelIdentifier = new ModelIdentifier(_superEditContext.Model);
        }

        if (_superEditContextChangedSinceLastParametersSet || _ownEditContextChangedSinceLastParametersSet) {
            Debug.Assert(_ownEditContext is not null);

            // Cascade EditContext._fieldStates
            var editContextFieldStatesMemberAccessor = EditContextFieldStatesMemberAccessor;
            var fieldStates = editContextFieldStatesMemberAccessor.GetValue(_superEditContext);
            editContextFieldStatesMemberAccessor.SetValue(_ownEditContext, fieldStates);

            // Cascade EditContext.Properties
            EditContextAccessor.GetProperties(_ownEditContext) = EditContextAccessor.GetProperties(_superEditContext);
        }

        return;

        void InitializeModelFieldIdentifiers()
        {
            if (_modelFieldIdentifiers is not { } routeModelFieldIdentifiers) {
                _modelFieldIdentifiers = routeModelFieldIdentifiers = [];
            } else {
                routeModelFieldIdentifiers.Clear();
            }

            Debug.Assert(Routes is not null);
            foreach (var route in Routes) {
                var modelIdentifier = ModelIdentifier.Create(route);
                var fieldIdentifier = FieldIdentifierExtension.WithPropertyPath(route);
                if (!routeModelFieldIdentifiers.TryAdd(modelIdentifier, fieldIdentifier)) {
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

        var modelIdentifier = new ModelIdentifier(e.FieldIdentifier.Model);
        if (modelIdentifier.Equals(_superEditContextModelIdentifier)) {
            componentValidator.ValidateDirectField(e.FieldIdentifier);
            goto notifyValidationStateChanged;
        }

        Debug.Assert(_modelFieldIdentifiers is not null);
        if (!_modelFieldIdentifiers.TryGetValue(modelIdentifier, out var modelFieldIdentifier)) {
            throw new InvalidOperationException(
                $"The model of type {modelIdentifier.Model.GetType()} is unrecognized. Is it registered as a potencial route?");
        }

        var concenatedFieldName = $"{modelFieldIdentifier.FieldName}.{e.FieldIdentifier.FieldName}";
        var concenatedFieldIdentifier = new FieldIdentifier(modelFieldIdentifier.Model, concenatedFieldName);
        componentValidator.ValidateNestedField(e.FieldIdentifier, concenatedFieldIdentifier);

        notifyValidationStateChanged:
        Debug.Assert(_ownEditContext is not null);
        _ownEditContext.NotifyValidationStateChanged();
    }
}
