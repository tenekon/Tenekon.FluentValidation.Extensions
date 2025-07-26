using System.Diagnostics.CodeAnalysis;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal abstract class ValueTransition<T> : IValueState<T>
{
    private T? _new;
    private int _cacheInvalidationState;

    internal IRevisioner? Revisioner { get; set; }

    /// <summary>
    /// The previous edit context.
    /// </summary>
    /// <remarks>
    /// Null if there was no prior context.
    /// </remarks>
    public T? Old {
        get;
        set {
            InvalidateCache();
            field = value;
        }
    }

    [MemberNotNullWhen(returnValue: false, nameof(Old))]
    public bool IsOldNull => Old is null;

    [MemberNotNullWhen(returnValue: true, nameof(Old))]
    public bool IsOldNonNull => Old is not null;

    /// <summary>
    /// The new edit context that replaced the prior one.
    /// </summary>
    /// <remarks>
    /// Not raised when the context is being disposed. Use Deinitialize* methods instead.
    /// </remarks>
    [AllowNull]
    public T New {
        get => _new ?? throw new InvalidOperationException("Member is null although it is a required member.");
        set {
            InvalidateCache();
            _new = value;
        }
    }

    T IValueState<T>.Value => New;

    public T? NewOrNull => _new;

    [MemberNotNullWhen(returnValue: false, nameof(_new))]
    public bool IsNewNull => _new is null;

    [MemberNotNullWhen(returnValue: true, nameof(_new))]
    public bool IsNewNonNull => _new is not null;

    public bool TryGetNew([NotNullWhen(returnValue: true)] out T? editContext, bool invalidate = false)
    {
        if (!IsNewNonNull) {
            editContext = default;
            return false;
        }

        editContext = _new;
        _new = default;
        return true;
    }

    // ReSharper disable once UnusedMemberInSuper.Global
    public abstract bool IsNewSame { get; }

    // ReSharper disable once UnusedMemberInSuper.Global
    public abstract bool IsNewDifferent { get; }

    public virtual bool IsUnitialized {
        get {
            if (Interlocked.Or(ref _cacheInvalidationState, (int)CacheInvalidationStates.IsUninitialized) == 0) {
                field = IsOldNull && IsNewNull;
            }

            return field;
        }
    }

    protected virtual void InvalidateCacheCore() => _cacheInvalidationState = 0;

    public void InvalidateCache()
    {
        InvalidateCacheCore();
        Revisioner?.IncrementRevision();
    }

    [Flags]
    private enum CacheInvalidationStates
    {
        IsUninitialized = 1 << 0
    }
}
