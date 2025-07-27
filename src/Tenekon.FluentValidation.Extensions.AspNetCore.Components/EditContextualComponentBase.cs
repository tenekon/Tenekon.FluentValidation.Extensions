using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using RuntimeHelpers = System.Runtime.CompilerServices.RuntimeHelpers;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

// ReSharper disable StaticMemberInGenericType
public abstract class EditContextualComponentBase<TDerived> : ComponentBase, IEditContextualComponent, IEditContextualComponentTrait,
    ILastParameterSetTransitionTrait, IDisposable,
    IAsyncDisposable where TDerived : EditContextualComponentBase<TDerived>, IParameterSetTransitionHandlerRegistryProvider
{
    static EditContextualComponentBase()
    {
        TDerived.ParameterSetTransitionHandlerRegistry.RegisterHandler(SetProvidedEditContexts, HandlerInsertPosition.After);
        TDerived.ParameterSetTransitionHandlerRegistry.RegisterHandler(SetDerivedEditContexts, HandlerInsertPosition.After);

        TDerived.ParameterSetTransitionHandlerRegistry.RegisterHandler(
            SubscribeToRootEditContextOnValidationRequestedAction,
            HandlerInsertPosition.After);

        TDerived.ParameterSetTransitionHandlerRegistry.RegisterHandler(UpdateEditContextReferences, HandlerInsertPosition.After);

        TDerived.ParameterSetTransitionHandlerRegistry.RegisterHandler(
            UpdateActorEditContextPropertyOccupationOfRootEditContextLookupKey,
            HandlerInsertPosition.After);

        return;

        static void UpdateEditContextReferences(EditContextualComponentBaseParameterSetTransition transition)
        {
            var component = (EditContextualComponentBase<TDerived>)transition.Component;

            var root = transition.RootEditContext;
            if (root.IsNewDifferent) {
                component.DeinitializeRootValidationMessageStore();
                component.DeinitializeRootEditContext();

                if (root.IsNewNonNull) {
                    root.New.OnValidationRequested += component.OnValidateModel;
                    // TODO: Maybe we need to make it disable this in *Routes component
                    component._rootEditContextValidationMessageStore = new ValidationMessageStore(root.New);
                    // component._rootEditContext = root.New;
                }
            }

            var ancestor = transition.AncestorEditContext;
            if (ancestor.IsNewDifferent) {
                component.DeinitializeAncestorEditContext();

                if (ancestor.IsNewNonNull) {
                    // component._ancestorEditContext = ancestor.New;
                }
            }

            var actor = transition.ActorEditContext;
            if (actor.IsNewDifferent) {
                component.DeinitializeActorEditContext();

                if (actor.IsNewNonNull) {
                    actor.New.OnFieldChanged += component.OnValidateField;
                    // component._actorEditContext = actor.New;
                }
            }

            if (actor.IsNewDifferent || root.IsNewDifferent) {
                component.DeinitializeActorEditContextValidationMessageStore();

                if (transition.IsNewEditContextOfActorAndRootNonNullAndDifferent) {
                    actor.New.OnValidationRequested += component.BubbleUpOnValidationRequested;
                    // TODO: Maybe we need to make it disable this in *Routes component
                    component._actorEditContextValidationMessageStore = new ValidationMessageStore(actor.New);
                }
            }
        }

        static void UpdateActorEditContextPropertyOccupationOfRootEditContextLookupKey(
            EditContextualComponentBaseParameterSetTransition transition)
        {
            if (transition.ActorEditContext.IsNewDifferent || transition.RootEditContext.IsNewDifferent) {
                // TODO: && IsFirstTransition: false?
                if (transition.ActorEditContext is { IsOldNonNull: true }) {
                    EditContextPropertyAccessor.s_rootEditContext.DisoccupyProperty(transition.ActorEditContext.Old);
                }

                if (transition.ActorEditContext.IsNewNonNull && transition.RootEditContext.IsNewNonNull) {
                    EditContextPropertyAccessor.s_rootEditContext.OccupyProperty(
                        transition.ActorEditContext.New,
                        transition.RootEditContext.New);
                }
            }
        }
    }

    internal virtual ParameterSetTransitionHandlerRegistry ParameterSetTransitionHandlerRegistry =>
        TDerived.ParameterSetTransitionHandlerRegistry;


    internal static Action<EditContextualComponentBaseParameterSetTransition> SetProvidedEditContexts { get; } = static transition => {
        if (transition.IsDisposing) {
            return;
        }

        var component = Unsafe.As<EditContextualComponentBase<TDerived>>(transition.Component);

        transition.AncestorEditContext.New = component.AncestorEditContext;
        transition.ActorEditContext.New = ((IEditContextualComponentTrait)component).ActorEditContext;
    };

    internal static Action<EditContextualComponentBaseParameterSetTransition> SetDerivedEditContexts { get; } = static transition => {
        if (transition.IsDisposing) {
            return;
        }

        var component = transition.Component;

        /* We have three definitions of an edit context.
         * 1. The root edit context is the one originating from an edit form.
         * 2. The ancestor edit context is the parent edit context. The ancestor edit context can be the root edit context.
         * 3. The actor edit context is the operating edit context of this validator.
         *    The actor edit context can be either the root edit context, the ancestor edit context or an new instance of an edit context.
         */

        var ancestorEditContext = transition.AncestorEditContext.New;
        if (ancestorEditContext is null) {
            throw new InvalidOperationException(
                $"{component.GetType()} requires a cascading parameter of type {nameof(EditContext)}. For example, you can use {component.GetType()} inside an EditForm component.");
        }

        var actorEditContext = transition.ActorEditContext.NewOrNull;
        if (actorEditContext is null) {
            throw new InvalidOperationException($"{component.GetType()} requires property {nameof(ActorEditContext)} being overriden.");
        }

        EditContext rootEditContext;
        var areActorEditContextAndAncestorEditContextEqual = ReferenceEquals(actorEditContext, ancestorEditContext);
        if (areActorEditContextAndAncestorEditContextEqual) {
            rootEditContext = ancestorEditContext;
        } else {
            if (EditContextPropertyAccessor.s_rootEditContext.TryGetPropertyValue(ancestorEditContext, out var rootEditContext2)) {
                rootEditContext = rootEditContext2;
            } else {
                rootEditContext = ancestorEditContext;
            }
        }

        transition.RootEditContext.New = rootEditContext;
    };

    internal static Action<EditContextualComponentBaseParameterSetTransition>
        SubscribeToRootEditContextOnValidationRequestedAction { get; } = static transition => {
        var component = (EditContextualComponentBase<TDerived>)transition.Component;

        var root = transition.RootEditContext;
        if (root.IsNewDifferent) {
            if (root.IsOldNonNull) {
                root.Old.OnValidationRequested -= component.OnValidateModel;
            }

            if (root.IsNewNonNull) {
                root.New.OnValidationRequested += component.OnValidateModel;
            }
        }
    };

    private bool _didParametersTransitionedOnce;

    internal ValidationMessageStore? _rootEditContextValidationMessageStore;
    internal ValidationMessageStore? _actorEditContextValidationMessageStore;

    [field: AllowNull]
    [field: MaybeNull]
    EditContextualComponentBaseParameterSetTransition ILastParameterSetTransitionTrait.LastParameterSetTransition {
        get => field ??= CreateParameterSetTransition();
        set;
    }

    internal virtual EditContextualComponentBaseParameterSetTransition LastParameterSetTransition =>
        Unsafe.As<ILastParameterSetTransitionTrait>(this).LastParameterSetTransition;

    IEditContextualComponentState IEditContextualComponent.ComponentState => LastParameterSetTransition;

    EditContext? IEditContextualComponentTrait.ActorEditContext => null;

    internal virtual EditContext ActorEditContext =>
        LastParameterSetTransition.ActorEditContext.NewOrNull ?? throw new InvalidOperationException(
            $"The {nameof(ActorEditContext)} property hos not been yet initialized. Typically initialized the first time during component initialization.");


    [CascadingParameter]
    internal EditContext? AncestorEditContext { get; set; }

    EditContext IEditContextualComponent.EditContext => LastParameterSetTransition.ActorEditContext.New;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override Task OnParametersSetAsync() => OnParametersTransitioningAsync();

    internal virtual Task OnParametersTransitioningAsync()
    {
        var parametersTransition = CreateParameterSetTransition();
        ConfigureParameterSetTransition(parametersTransition);
        _didParametersTransitionedOnce = true;

        foreach (var registrationItem in ParameterSetTransitionHandlerRegistry.GetRegistrationItems()) {
            registrationItem.Handler(parametersTransition);
        }

        Unsafe.As<ILastParameterSetTransitionTrait>(this).LastParameterSetTransition = parametersTransition;
        return Task.CompletedTask;
    }

    internal virtual EditContextualComponentBaseParameterSetTransition CreateParameterSetTransition() => new();

    internal virtual void ConfigureParameterSetTransition(EditContextualComponentBaseParameterSetTransition transition)
    {
        transition.Component = this;

        var isFirstTransition = !_didParametersTransitionedOnce;
        transition.IsFirstTransition = isFirstTransition;

        transition.ActorEditContext.Old = LastParameterSetTransition.ActorEditContext.NewOrNull;
        transition.AncestorEditContext.Old = LastParameterSetTransition.AncestorEditContext.NewOrNull;
        transition.RootEditContext.Old = LastParameterSetTransition.RootEditContext.NewOrNull;
        transition.ChildContent.Old = LastParameterSetTransition.ChildContent.NewOrNull;

        if (!transition.IsDisposing) {
            transition.ChildContent.New = ChildContent;
        }
    }

    internal virtual void ConfigureDisposalParameterSetTransition(EditContextualComponentBaseParameterSetTransition transition)
    {
        transition.IsDisposing = true;
        ConfigureParameterSetTransition(transition);
    }

    internal virtual void OnValidateModel(object? sender, ValidationRequestedEventArgs args)
    {
    }

    internal virtual void OnValidateField(object? sender, FieldChangedEventArgs e)
    {
    }

    private void BubbleUpOnValidationRequested(object? sender, ValidationRequestedEventArgs e) =>
        LastParameterSetTransition.RootEditContext.New.Validate();

    protected void RenderEditContextualComponent(RenderTreeBuilder builder, RenderFragment? childContent)
    {
        if (LastParameterSetTransition.IsNewEditContextOfActorAndAncestorNonNullAndSame) {
            builder.AddContent(sequence: 0, childContent);
            return;
        }

        var actorEditContext = LastParameterSetTransition.ActorEditContext.NewOrNull;

        // Because OnParametersSetAsync is suspending before assigning actor edit context, premature rendering may occur.  
        if (actorEditContext is null) {
            return;
        }

        builder.OpenComponent<CascadingValue<EditContext>>(sequence: 1);
        // Because edit context instances can stay constant but its model not, we have to set a unique component identity 
        builder.SetKey(new EditContextIdentitySnapshot(actorEditContext, actorEditContext.Model));
        builder.AddComponentParameter(sequence: 2, "IsFixed", value: true);
        builder.AddComponentParameter(sequence: 3, "Value", actorEditContext);
        builder.AddComponentParameter(sequence: 4, nameof(CascadingValue<>.ChildContent), childContent);
        builder.CloseComponent();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => RenderEditContextualComponent(builder, ChildContent);

    private void DeinitializeRootValidationMessageStore()
    {
        if (_rootEditContextValidationMessageStore is null) {
            return;
        }
        _rootEditContextValidationMessageStore.Clear();
        _rootEditContextValidationMessageStore = null;
    }

    private void DeinitializeRootEditContext() => LastParameterSetTransition.RootEditContext.New = null;

    private void DeinitializeAncestorEditContext()
    {
        if (!LastParameterSetTransition.AncestorEditContext.TryGetNew(out var editContext, invalidate: true)) {
            return;
        }

        editContext.OnValidationRequested -= OnValidateModel;
    }

    private void DeinitializeActorEditContextValidationMessageStore()
    {
        if (_actorEditContextValidationMessageStore is null) {
            return;
        }
        _actorEditContextValidationMessageStore.Clear();
        _actorEditContextValidationMessageStore = null;
    }

    private void DeinitializeActorEditContext()
    {
        var editContextTransition = LastParameterSetTransition.ActorEditContext;
        if (!editContextTransition.TryGetNew(out var editContext)) {
            return;
        }

        editContext.OnFieldChanged -= OnValidateField;

        if (LastParameterSetTransition.IsNewEditContextOfActorAndAncestorNonNullAndDifferent) {
            editContext.OnValidationRequested -= BubbleUpOnValidationRequested;
        }

        editContextTransition.New = null;
    }

    #region Disposal Behaviour

    private int _disposalState;

    protected virtual void DisposeCommon()
    {
        if ((Interlocked.Or(ref _disposalState, (int)DisposalStates.CommonDisposed) & (int)DisposalStates.CommonDisposed) != 0) {
            return;
        }

        var parametersTransition = CreateParameterSetTransition();
        ConfigureDisposalParameterSetTransition(parametersTransition);

        foreach (var registrationItem in TDerived.ParameterSetTransitionHandlerRegistry.GetRegistrationItems()) {
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

    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

    async ValueTask IAsyncDisposable.DisposeAsync()
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

    // We want to detect not only changes to the EditContext reference, but also to its associated Model reference.
    private class EditContextIdentitySnapshot(EditContext editContext, object model)
    {
        private readonly EditContext _editContext = editContext;
        private readonly object _model = model;

        public override bool Equals(object? obj) =>
            obj is EditContextIdentitySnapshot identity && ReferenceEquals(_editContext, identity._editContext) &&
            ReferenceEquals(_model, identity._model);

        public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(_editContext), RuntimeHelpers.GetHashCode(_model));
    }
}
