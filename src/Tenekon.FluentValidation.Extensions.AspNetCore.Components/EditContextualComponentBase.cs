using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public abstract class EditContextualComponentBase : ComponentBase, IEditContextualComponentTrait, IDisposable
{
    private class RootEditContextPropertyLookupKey;

    internal static readonly object s_rootEditContextPropertyLookupKey = new RootEditContextPropertyLookupKey();

    private static readonly SharedEditContextPropertyClassValueAccessor<EditContext> s_rootEditContextPropertyAccessor =
        new(s_rootEditContextPropertyLookupKey);

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

    void IDisposable.Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    EditContext? IEditContextualComponentTrait.OwnEditContext => null;

    protected override void OnParametersSet()
    {
        ConfigureFluentComponentBaseOnParametersSet();
        return;

        void ConfigureFluentComponentBaseOnParametersSet()
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
                if (s_rootEditContextPropertyAccessor.TryGetPropertyValue(superEditContext, out var rootEditContext2)) {
                    rootEditContext = rootEditContext2;
                } else {
                    rootEditContext = superEditContext;
                }
            }

            var currentRootEditContext = _rootEditContext;
            var rootEditContextChanged = !ReferenceEquals(currentRootEditContext, rootEditContext);
            if (rootEditContextChanged) {
                DeinitializeRootValidationMessageStore();
                DeinitializeRootEditContext();
                _rootEditContextValidationMessageStore = new ValidationMessageStore(rootEditContext);
                _rootEditContext = rootEditContext;
            }

            var currentSuperEditContext = _superEditContext;
            var superEditContextChanged = !ReferenceEquals(currentSuperEditContext, superEditContext);
            if (superEditContextChanged) {
                DeinitializeSuperEditContext();
                superEditContext.OnValidationRequested += OnValidateModel;
                _superEditContext = superEditContext;
            }

            var currentOwnEditContext = _ownEditContext;
            var ownEditContextChanged = !ReferenceEquals(currentOwnEditContext, ownEditContext);
            if (ownEditContextChanged) {
                DeinitializeOwnEditContextValidationMessageStore();
                DeinitializeOwnEditContext();
                ownEditContext.OnFieldChanged += OnValidateField;
                if (areOwnEditContextAndSuperEditContextNotEqual) {
                    ownEditContext.OnValidationRequested += BubbleUpOnValidationRequested;
                }
                _ownEditContextValidationMessageStore = new ValidationMessageStore(ownEditContext);
                _ownEditContext = ownEditContext;
            }

            _areOwnEditContextAndSuperEditContextEqual = areOwnEditContextAndSuperEditContextEqual;
            _areOwnEditContextAndSuperEditContextNotEqual = areOwnEditContextAndSuperEditContextNotEqual;

            _superEditContextChangedSinceLastParametersSet = superEditContextChanged;
            _ownEditContextChangedSinceLastParametersSet = ownEditContextChanged;

            if (rootEditContextChanged) {
                s_rootEditContextPropertyAccessor.OccupyProperty(ownEditContext, rootEditContext);

                OnRootEditContextChanged(
                    new EditContextChangedEventArgs {
                        Old = currentRootEditContext, New = rootEditContext
                    });
            }

            if (superEditContextChanged) {
                OnSuperEditContextChanged(
                    new EditContextChangedEventArgs {
                        Old = currentSuperEditContext, New = superEditContext
                    });
            }

            if (ownEditContextChanged) {
                OnOwnEditContextChanged(
                    new EditContextChangedEventArgs {
                        Old = currentOwnEditContext, New = ownEditContext
                    });
            }
        }
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
            // ISSUE: If two components are descendants of one edit context, then we cannot just remove the key.
            s_rootEditContextPropertyAccessor.DisoccupyProperty(_ownEditContext);
            _ownEditContext.OnValidationRequested -= BubbleUpOnValidationRequested;
        }

        _ownEditContext = null;
    }

    protected virtual void OnRootEditContextChanged(EditContextChangedEventArgs args)
    {
    }

    protected virtual void OnSuperEditContextChanged(EditContextChangedEventArgs args)
    {
    }

    protected virtual void OnOwnEditContextChanged(EditContextChangedEventArgs args)
    {
    }

    /// <summary>Called to dispose this instance.</summary>
    /// <param name="disposing"><see langword="true" /> if called within <see cref="IDisposable.Dispose" />.</param>
    protected virtual void Dispose(bool disposing)
    {
        DeinitializeRootValidationMessageStore();
        DeinitializeRootEditContext();
        DeinitializeSuperEditContext();
        DeinitializeOwnEditContextValidationMessageStore();
        DeinitializeOwnEditContext();
    }

    protected sealed class EditContextChangedEventArgs
    {
        /// <summary>
        /// The previous edit context.
        /// </summary>
        /// <remarks>
        /// Null if there was no prior context.
        /// </remarks>
        public required EditContext? Old { get; init; }

        /// <summary>
        /// The new edit context that replaced the prior one.
        /// </summary>
        /// <remarks>
        /// Not raised when the context is being disposed. Use Deinitialize* methods instead.
        /// </remarks>
        public required EditContext New { get; init; }
    }
}
