using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using RuntimeHelpers = System.Runtime.CompilerServices.RuntimeHelpers;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public abstract class EditContextualComponentBase<T> : ComponentBase, IEditContextualComponentTrait, IDisposable, IAsyncDisposable
    where T : IHandlingParametersTransition
{
    static EditContextualComponentBase()
    {
        T.ParametersTransitionHandlerRegistry.RegisterHandler(
            SubscribeToRootEditContextOnValidationRequestedAction,
            HandlerAddingPosition.End);

        T.ParametersTransitionHandlerRegistry.RegisterHandler(
            UpdateEditContextReferences,
            HandlerAddingPosition.End
            // TOOD: maybe unique object with debug token?
        ); // $"{nameof(EditContextualComponentBase<>)}<{typeof(T)}>.{nameof(HandleParametersTransition)}"

        T.ParametersTransitionHandlerRegistry.RegisterHandler(
            UpdateActorEditContextPropertyOccupationOfRootEditContextLookupKey,
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
                    root.New.OnValidationRequested += component.OnValidateModel;
                    // TODO: Maybe we need to make it disable this in *Routes component
                    component._rootEditContextValidationMessageStore = new ValidationMessageStore(root.New);
                    component._rootEditContext = root.New;
                }
            }

            var ancestor = transition.AncestorContextTransition;
            if (ancestor.IsOldReferenceDifferentToNew) {
                component.DeinitializeAncestorEditContext();

                if (ancestor.IsNewNonNull) {
                    component._ancestorEditContext = ancestor.New;
                }
            }

            var actor = transition.ActorContextTransition;
            if (actor.IsOldReferenceDifferentToNew) {
                component.DeinitializeActorEditContext();

                if (actor.IsNewNonNull) {
                    actor.New.OnFieldChanged += component.OnValidateField;
                    component._actorEditContext = actor.New;
                }
            }

            if (actor.IsOldReferenceDifferentToNew || root.IsOldReferenceDifferentToNew) {
                component.DeinitializeActorEditContextValidationMessageStore();

                if (actor.IsNewNonNull && root.IsNewNonNull) {
                    var isNewActorReferenceDifferentToNewRoot = !ReferenceEquals(actor.New, root.New);
                    if (isNewActorReferenceDifferentToNewRoot) {
                        actor.New.OnValidationRequested += component.BubbleUpOnValidationRequested;
                        // TODO: Maybe we need to make it disable this in *Routes component
                        component._actorEditContextValidationMessageStore = new ValidationMessageStore(actor.New);
                    }
                }
            }
        }

        static void UpdateActorEditContextPropertyOccupationOfRootEditContextLookupKey(ParametersTransition transition)
        {
            if (transition.ActorContextTransition.IsOldReferenceDifferentToNew ||
                transition.RootContextTransition.IsOldReferenceDifferentToNew) {
                // TODO: && IsFirstTransition: false?
                if (transition.ActorContextTransition is { IsOldNonNull: true }) {
                    RootEditContextPropertyAccessorHolder.s_accessor.DisoccupyProperty(transition.ActorContextTransition.Old);
                }

                if (transition.ActorContextTransition.IsNewNonNull && transition.RootContextTransition.IsNewNonNull) {
                    RootEditContextPropertyAccessorHolder.s_accessor.OccupyProperty(
                        transition.ActorContextTransition.New,
                        transition.RootContextTransition.New);
                }
            }
        }
    }

    internal virtual ParametersTransitionHandlerRegistry ParametersTransitionHandlerRegistry => T.ParametersTransitionHandlerRegistry;

    internal static Action<ParametersTransition> SubscribeToRootEditContextOnValidationRequestedAction { get; } = static transition => {
        var component = (EditContextualComponentBase<T>)transition.EditContextualComponentBase;

        var root = transition.RootContextTransition;
        if (root.IsOldReferenceDifferentToNew) {
            if (root.IsOldNonNull) {
                root.Old.OnValidationRequested -= component.OnValidateModel;
            }

            if (root.IsNewNonNull) {
                root.New.OnValidationRequested += component.OnValidateModel;
            }
        }
    };

    private bool _didParametersTransitionedOnce;

    internal bool _areActorEditContextAndAncestorEditContextEqual;
    internal bool _areActorEditContextAndAncestorEditContextNotEqual;
    internal EditContext? _actorEditContext;
    internal EditContext? _ancestorEditContext;
    internal bool _ancestorEditContextChangedSinceLastParametersSet;

    internal EditContext? _rootEditContext;
    internal ValidationMessageStore? _rootEditContextValidationMessageStore;
    internal ValidationMessageStore? _actorEditContextValidationMessageStore;

    public EditContext ActorEditContext =>
        _actorEditContext ?? throw new InvalidOperationException(
            $"The {nameof(ActorEditContext)} property hos not been yet initialized. Typically initialized the first time during component initialization.");

    [CascadingParameter]
    protected EditContext? AncestorEditContext { get; private set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    EditContext? IEditContextualComponentTrait.ActorEditContext => null;

    protected override Task OnParametersSetAsync()
    {
        /* We have three definitions of an edit context.
         * 1. The root edit context is the one originating from an edit form.
         * 2. The ancestor edit context is the parent edit context. The ancestor edit context can be the root edit context.
         * 3. The actor edit context is the operating edit context of this validator.
         *    The actor edit context can be either the root edit context, the ancestor edit context or an new instance of an edit context.
         */
        var ancestorEditContext = AncestorEditContext;
        if (ancestorEditContext is null) {
            throw new InvalidOperationException(
                $"{GetType()} requires a cascading parameter of type {nameof(EditContext)}. For example, you can use {GetType()} inside an EditForm component.");
        }

        var actorEditContext = ((IEditContextualComponentTrait)this).ActorEditContext;
        if (actorEditContext is null) {
            throw new InvalidOperationException($"{GetType()} requires property {nameof(ActorEditContext)} being overriden.");
        }

        EditContext rootEditContext;
        var areActorEditContextAndAncestorEditContextEqual = ReferenceEquals(actorEditContext, ancestorEditContext);
        var areActorEditContextAndAncestorEditContextNotEqual = !areActorEditContextAndAncestorEditContextEqual;
        if (areActorEditContextAndAncestorEditContextEqual) {
            rootEditContext = ancestorEditContext;
        } else {
            if (RootEditContextPropertyAccessorHolder.s_accessor.TryGetPropertyValue(ancestorEditContext, out var rootEditContext2)) {
                rootEditContext = rootEditContext2;
            } else {
                rootEditContext = ancestorEditContext;
            }
        }

        var isFirstTransition = !_didParametersTransitionedOnce;
        _didParametersTransitionedOnce = true;

        var parametersTransition = new ParametersTransition {
            EditContextualComponentBase = this,
            ActorContextTransition = new EditContextTransition() {
                Old = _actorEditContext,
                New = actorEditContext,
                IsFirstTransition = isFirstTransition
            },
            AncestorContextTransition = new EditContextTransition {
                Old = _ancestorEditContext,
                New = ancestorEditContext,
                IsFirstTransition = isFirstTransition
            },
            RootContextTransition = new EditContextTransition() {
                Old = _rootEditContext,
                New = rootEditContext,
                IsFirstTransition = isFirstTransition
            },
        };

        foreach (var registrationItem in ParametersTransitionHandlerRegistry.GetRegistrationItems()) {
            registrationItem.Handler(parametersTransition);
        }

        // TODO: Replace below lines by parameters transition API

        _areActorEditContextAndAncestorEditContextEqual = areActorEditContextAndAncestorEditContextEqual;
        _areActorEditContextAndAncestorEditContextNotEqual = areActorEditContextAndAncestorEditContextNotEqual;
        _ancestorEditContextChangedSinceLastParametersSet = parametersTransition.AncestorContextTransition.IsOldReferenceEqualsToNew;
        return Task.CompletedTask;
    }

    protected virtual void OnValidateModel(object? sender, ValidationRequestedEventArgs args)
    {
    }

    protected virtual void OnValidateField(object? sender, FieldChangedEventArgs e)
    {
    }

    private void BubbleUpOnValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        Debug.Assert(_rootEditContext is not null);
        _rootEditContext.Validate();
    }

    protected void RenderEditContextualComponent(RenderTreeBuilder builder, RenderFragment? childContent)
    {
        // Because OnParametersSetAsync is suspending before assigning _actorEditContext, premature rendering may occur.  
        if (_actorEditContext is null) {
            return;
        }

        Debug.Assert(_actorEditContext is not null);
        if (_areActorEditContextAndAncestorEditContextEqual) {
            builder.AddContent(sequence: 0, childContent);
            return;
        }

        builder.OpenComponent<CascadingValue<EditContext>>(sequence: 1);
        // Because edit context instances can stay constant but its model not, we have to set a unique component identity 
        builder.SetKey(new CascadedValueKey(_actorEditContext, _actorEditContext.Model));
        builder.AddComponentParameter(sequence: 2, "IsFixed", value: true);
        builder.AddComponentParameter(sequence: 3, "Value", _actorEditContext);
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

    protected virtual void DeinitializeAncestorEditContext()
    {
        if (_ancestorEditContext is null) {
            return;
        }

        _ancestorEditContext.OnValidationRequested -= OnValidateModel;
        _ancestorEditContext = null;
    }

    protected virtual void DeinitializeActorEditContextValidationMessageStore()
    {
        if (_actorEditContextValidationMessageStore is null) {
            return;
        }
        _actorEditContextValidationMessageStore.Clear();
        _actorEditContextValidationMessageStore = null;
    }

    protected virtual void DeinitializeActorEditContext()
    {
        if (_actorEditContext is null) {
            return;
        }

        _actorEditContext.OnFieldChanged -= OnValidateField;

        if (_areActorEditContextAndAncestorEditContextNotEqual) {
            _actorEditContext.OnValidationRequested -= BubbleUpOnValidationRequested;
        }

        _actorEditContext = null;
    }

    #region Disposal Behaviour

    private int _disposalState;

    protected virtual void DisposeCommon()
    {
        if ((Interlocked.Or(ref _disposalState, (int)DisposalStates.CommonDisposed) & (int)DisposalStates.CommonDisposed) != 0) {
            return;
        }

        var isFirstTransition = !_didParametersTransitionedOnce;

        var parametersTransition = new ParametersTransition {
            IsDisposing = true,
            EditContextualComponentBase = this,
            ActorContextTransition = new EditContextTransition() {
                Old = _actorEditContext,
                New = null,
                IsFirstTransition = isFirstTransition
            },
            AncestorContextTransition = new EditContextTransition {
                Old = _ancestorEditContext,
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

    /// <summary>Called to dispose this instance.</summary>
    /// <param name="disposing"><see langword="true" /> if called within <see cref="IDisposable.Dispose" />.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    void IDisposable.Dispose()
    {
        if ((Interlocked.Or(ref _disposalState, (int)DisposalStates.SyncDisposed) & (int)DisposalStates.SyncDisposed) != 0) {
            return;
        }

        DisposeCommon();
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        if ((Interlocked.Or(ref _disposalState, (int)DisposalStates.AsyncDisposed) & (int)DisposalStates.AsyncDisposed) != 0) {
            return;
        }

        await DisposeAsyncCore();
        DisposeCommon();
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    [Flags]
    private enum DisposalStates
    {
        SyncDisposed = 1 << 0,
        AsyncDisposed = 1 << 1,
        CommonDisposed = 1 << 2
    }

    #endregion

    private class CascadedValueKey(EditContext editContext, object model)
    {
        private readonly EditContext _editContext = editContext;
        private readonly object _model = model;

        public override bool Equals(object? obj) =>
            obj is CascadedValueKey identity && ReferenceEquals(_editContext, identity._editContext) &&
            ReferenceEquals(_model, identity._model);

        public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(_editContext), RuntimeHelpers.GetHashCode(_model));
    }
}
