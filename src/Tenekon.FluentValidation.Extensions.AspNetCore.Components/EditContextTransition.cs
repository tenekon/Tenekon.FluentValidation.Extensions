using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal sealed class EditContextTransition
{
    private bool? _isOldReferenceEqualsToNew;
    private bool? _isOldReferenceDifferentToNew;
    private EditContext? _new;

    internal IRevisioner? Revisioner { get; set; }

    /// <summary>
    /// The previous edit context.
    /// </summary>
    /// <remarks>
    /// Null if there was no prior context.
    /// </remarks>
    public EditContext? Old {
        get;
        set {
            InvalidateCache();
            Revisioner?.IncrementRevision();
            field = value;
        }
    }

    [MemberNotNullWhen(returnValue: true, nameof(Old))]
    public bool IsOldNonNull => Old is not null;

    /// <summary>
    /// The new edit context that replaced the prior one.
    /// </summary>
    /// <remarks>
    /// Not raised when the context is being disposed. Use Deinitialize* methods instead.
    /// </remarks>
    [AllowNull]
    public EditContext New {
        get => _new ?? throw new InvalidOperationException("Member is null although it is a required member.");
        set {
            InvalidateCache();
            Revisioner?.IncrementRevision();
            _new = value;
        }
    }

    public EditContext? NewOrNull => _new;

    [MemberNotNullWhen(returnValue: true, nameof(_new))]
    public bool IsNewNonNull => _new is not null;

    public bool TryGetNew([NotNullWhen(returnValue: true)] out EditContext? editContext, bool invalidate = false)
    {
        if (!IsNewNonNull) {
            editContext = null;
            return false;
        }

        editContext = _new;
        _new = null;
        return true;
    }

    /// <summary>
    /// Indicates that <see cref="Old"/> must be null, because it is the first ever occuring transition.
    /// </summary>
    public required bool IsFirstTransition { get; init; }

    public bool IsOldReferenceEqualsToNew =>
        _isOldReferenceEqualsToNew ??= ReferenceEquals(Old, _new) && ReferenceEquals(Old?.Model, _new?.Model);

    public bool IsOldReferenceDifferentToNew =>
        _isOldReferenceDifferentToNew ??= !ReferenceEquals(Old, _new) || !ReferenceEquals(Old?.Model, _new?.Model);

    public void InvalidateCache()
    {
        _isOldReferenceEqualsToNew = null;
        _isOldReferenceDifferentToNew = null;
    }
}
