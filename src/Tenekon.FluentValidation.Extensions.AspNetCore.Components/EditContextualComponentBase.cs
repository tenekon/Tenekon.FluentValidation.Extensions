using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public abstract class EditContextualComponentBase<T> : ComponentBase, IEditContextualComponentTrait, IDisposable
    where T : IHandlingParametersTransition
{
    static EditContextualComponentBase()
    {
        var transitioner1 = UpdateEditContextReferences;

        T.ParametersTransitionHandlerRegistry.RegisterHandler(
            transitioner1,
            HandlerAddingPosition.End
            // TOOD: maybe unique object with debug token?
        ); // $"{nameof(EditContextualComponentBase<>)}<{typeof(T)}>.{nameof(HandleParametersTransition)}"

        T.ParametersTransitionHandlerRegistry.RegisterHandler(
            UpdateOwnEditContextPropertyOccupationOfRootEditContextLookupKey,
            HandlerAddingPosition.End);

        return;

        static void UpdateEditContextReferences(ParametersTransition transition)
        {
            var component = (EditContextualComponentBase<T>)transition.EditContextualComponentBase;

            var root = transition.RootContextTransition;
            if (root.IsOldReferenceDifferentToNew) {
                component.DeinitializeRootValidationMessageStore();
                component.DeinitializeRootEditContext();

                if (root.IsNewNonNull) {
                    // TODO: Maybe we need to make it disable this in *Routes component
                    component._rootEditContextValidationMessageStore = new ValidationMessageStore(root.New);
                    component._rootEditContext = root.New;
                }
            }

            var super = transition.SuperContextTransition;
            if (super.IsOldReferenceDifferentToNew) {
                component.DeinitializeSuperEditContext();

                if (super.IsNewNonNull) {
                    super.New.OnValidationRequested += component.OnValidateModel;
                    component._superEditContext = super.New;
                }
            }

            var own = transition.OwnContextTransition;
            if (own.IsOldReferenceDifferentToNew) {
                component.DeinitializeOwnEditContextValidationMessageStore();
                component.DeinitializeOwnEditContext();

                if (own.IsNewNonNull) {
                    own.New.OnFieldChanged += component.OnValidateField;

                    if (super.IsNewNonNull) {
                        var isNewOwnReferenceNotEqualsToNewSuper = !ReferenceEquals(own.New, super.New);
                        if (isNewOwnReferenceNotEqualsToNewSuper) {
                            own.New.OnValidationRequested += component.BubbleUpOnValidationRequested;
                        }
                    }

                    // TODO: Maybe we need to make it disable this in *Routes component
                    component._ownEditContextValidationMessageStore = new ValidationMessageStore(own.New);
                    component._ownEditContext = own.New;
                }
            }
        }

        static void UpdateOwnEditContextPropertyOccupationOfRootEditContextLookupKey(ParametersTransition transition)
        {
            if (transition.OwnContextTransition.IsOldReferenceDifferentToNew ||
                transition.RootContextTransition.IsOldReferenceDifferentToNew) {
                // TODO: && IsFirstTransition: false?
                if (transition.OwnContextTransition is { IsOldNonNull: true }) {
                    RootEditContextPropertyAccessorHolder.s_accessor.DisoccupyProperty(transition.OwnContextTransition.Old);
                }

                if (transition.OwnContextTransition.IsNewNonNull && transition.RootContextTransition.IsNewNonNull) {
                    RootEditContextPropertyAccessorHolder.s_accessor.OccupyProperty(
                        transition.OwnContextTransition.New,
                        transition.RootContextTransition.New);
                }
            }
        }
    }

    private int _isDisposed;
    private bool _didParametersTransitionedOnce;

    internal bool _areOwnEditContextAndSuperEditContextEqual;
    internal bool _areOwnEditContextAndSuperEditContextNotEqual;
    internal EditContext? _ownEditContext;
    internal EditContext? _superEditContext;
    internal bool _ownEditContextChangedSinceLastParametersSet;
    internal bool _superEditContextChangedSinceLastParametersSet;

    internal EditContext? _rootEditContext;
    internal ValidationMessageStore? _rootEditContextValidationMessageStore;
    internal ValidationMessageStore? _ownEditContextValidationMessageStore;

    public EditContext OwnEditContext =>
        _ownEditContext ?? throw new InvalidOperationException(
            $"The {nameof(OwnEditContext)} property hos not been yet initialized. Typically initialized the first time during component initialization.");

    [CascadingParameter]
    protected EditContext? SuperEditContext { get; private set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    EditContext? IEditContextualComponentTrait.OwnEditContext => null;

    protected override void OnParametersSet()
    {
        /* We have three definitions of an edit context.
         * 1. The root edit context is the one originating from an edit form.
         * 2. The super edit context is the parent edit context. The super edit context can be the root edit context.
         * 3. The own edit context is the operating edit context of this validator.
         *    The own edit context can be either the root edit context, the super edit context or an new instance of an edit context.
         */
        var superEditContext = SuperEditContext;
        if (superEditContext is null) {
            throw new InvalidOperationException(
                $"{GetType()} requires a cascading parameter of type {nameof(EditContext)}. For example, you can use {GetType()} inside an EditForm component.");
        }

        var ownEditContext = ((IEditContextualComponentTrait)this).OwnEditContext;
        if (ownEditContext is null) {
            throw new InvalidOperationException($"{GetType()} requires property {nameof(OwnEditContext)} being overriden.");
        }

        EditContext rootEditContext;
        var areOwnEditContextAndSuperEditContextEqual = ReferenceEquals(ownEditContext, superEditContext);
        var areOwnEditContextAndSuperEditContextNotEqual = !areOwnEditContextAndSuperEditContextEqual;
        if (areOwnEditContextAndSuperEditContextEqual) {
            rootEditContext = superEditContext;
        } else {
            if (RootEditContextPropertyAccessorHolder.s_accessor.TryGetPropertyValue(superEditContext, out var rootEditContext2)) {
                rootEditContext = rootEditContext2;
            } else {
                rootEditContext = superEditContext;
            }
        }

        var isFirstTransition = !_didParametersTransitionedOnce;
        _didParametersTransitionedOnce = true;

        var parametersTransition = new ParametersTransition {
            EditContextualComponentBase = this,
            OwnContextTransition = new EditContextTransition() {
                Old = _ownEditContext,
                New = ownEditContext,
                IsFirstTransition = isFirstTransition
            },
            SuperContextTransition = new EditContextTransition {
                Old = _superEditContext,
                New = superEditContext,
                IsFirstTransition = isFirstTransition
            },
            RootContextTransition = new EditContextTransition() {
                Old = _rootEditContext,
                New = rootEditContext,
                IsFirstTransition = isFirstTransition
            },
        };

        foreach (var registrationItem in T.ParametersTransitionHandlerRegistry.GetRegistrationItems()) {
            registrationItem.Handler(parametersTransition);
        }

        // TODO: Replace below lines by parameters transition API

        _areOwnEditContextAndSuperEditContextEqual = areOwnEditContextAndSuperEditContextEqual;
        _areOwnEditContextAndSuperEditContextNotEqual = areOwnEditContextAndSuperEditContextNotEqual;

        _superEditContextChangedSinceLastParametersSet = parametersTransition.SuperContextTransition.IsOldReferenceEqualsToNew;
        _ownEditContextChangedSinceLastParametersSet = parametersTransition.OwnContextTransition.IsOldReferenceEqualsToNew;
    }

    protected virtual void OnValidateModel(object? sender, ValidationRequestedEventArgs e)
    {
        Debug.Assert(_ownEditContext is not null);
        _ownEditContext.NotifyValidationStateChanged();
    }

    protected virtual void OnValidateField(object? sender, FieldChangedEventArgs e)
    {
        Debug.Assert(_ownEditContext is not null);
        _ownEditContext.NotifyValidationStateChanged();
    }

    private void BubbleUpOnValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        Debug.Assert(_superEditContext is not null);
        _superEditContext.Validate();
    }

    protected void RenderEditContextualComponent(RenderTreeBuilder builder, RenderFragment? childContent)
    {
        Debug.Assert(_ownEditContext is not null);
        if (_areOwnEditContextAndSuperEditContextEqual) {
            builder.AddContent(sequence: 0, childContent);
            return;
        }

        builder.OpenComponent<CascadingValue<EditContext>>(sequence: 1);
        builder.AddComponentParameter(sequence: 2, "IsFixed", value: true);
        builder.AddComponentParameter(sequence: 3, "Value", _ownEditContext);
        builder.AddComponentParameter(sequence: 4, nameof(CascadingValue<>.ChildContent), childContent);
        builder.CloseComponent();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => RenderEditContextualComponent(builder, ChildContent);

    protected virtual void DeinitializeRootValidationMessageStore()
    {
        if (_rootEditContextValidationMessageStore is null) {
            return;
        }
        _rootEditContextValidationMessageStore.Clear();
        _rootEditContextValidationMessageStore = null;
    }

    protected virtual void DeinitializeRootEditContext() => _rootEditContext = null;

    protected virtual void DeinitializeSuperEditContext()
    {
        if (_superEditContext is null) {
            return;
        }
        _superEditContext.OnValidationRequested -= OnValidateModel;
        _superEditContext = null;
    }

    protected virtual void DeinitializeOwnEditContextValidationMessageStore()
    {
        if (_ownEditContextValidationMessageStore is null) {
            return;
        }
        _ownEditContextValidationMessageStore.Clear();
        _ownEditContextValidationMessageStore = null;
    }

    protected virtual void DeinitializeOwnEditContext()
    {
        if (_ownEditContext is null) {
            return;
        }

        _ownEditContext.OnFieldChanged -= OnValidateField;

        if (_areOwnEditContextAndSuperEditContextNotEqual) {
            _ownEditContext.OnValidationRequested -= BubbleUpOnValidationRequested;
        }

        _ownEditContext = null;
    }

    #region Dispose

    /// <summary>Called to dispose this instance.</summary>
    /// <param name="disposing"><see langword="true" /> if called within <see cref="IDisposable.Dispose" />.</param>
    protected virtual void Dispose(bool disposing)
    {
        var isFirstTransition = !_didParametersTransitionedOnce;

        var parametersTransition = new ParametersTransition {
            IsDisposing = true,
            EditContextualComponentBase = this,
            OwnContextTransition = new EditContextTransition() {
                Old = _ownEditContext,
                New = null,
                IsFirstTransition = isFirstTransition
            },
            SuperContextTransition = new EditContextTransition {
                Old = _superEditContext,
                New = null,
                IsFirstTransition = isFirstTransition
            },
            RootContextTransition = new EditContextTransition() {
                Old = _rootEditContext,
                New = null,
                IsFirstTransition = isFirstTransition
            },
        };

        foreach (var registrationItem in T.ParametersTransitionHandlerRegistry.GetRegistrationItems()) {
            registrationItem.Handler(parametersTransition);
        }
    }

    void IDisposable.Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 1) {
            return;
        }

        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
