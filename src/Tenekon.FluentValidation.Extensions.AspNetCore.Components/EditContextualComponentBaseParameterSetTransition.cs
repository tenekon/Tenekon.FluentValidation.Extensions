using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class EditContextualComponentBaseParameterSetTransition : IRevisioner
{
    public static EditContextualComponentBaseParameterSetTransition Unbound { get; } = new() {
        Component = new object(),
    };

    private int _revision;
    private int _cacheInvalidationState;

    internal int Revision => Volatile.Read(ref _revision);

    public bool IsDisposing { get; set; }

    [field: AllowNull]
    [field: MaybeNull]
    public virtual object Component {
        get {
            if (field is null) {
                throw new InvalidOperationException(
                    $"You are accessing the parameters transition intended for first render before OnParametersSet[Async], thus the field member is not yet available: {nameof(Component)}");
            }

            return field;
        }
        set;
    }

    /// <summary>
    /// Indicates that <see cref="Old"/> must be null, because it is the first ever occuring transition.
    /// </summary>
    public bool IsFirstTransition {
        get;
        set;
    }

    [field: AllowNull]
    [field: MaybeNull]
    public virtual EditContextTransition RootEditContext => field ??= new EditContextTransition() { Revisioner = this };

    [field: AllowNull]
    [field: MaybeNull]
    public virtual EditContextTransition AncestorEditContext => field ??= new EditContextTransition() { Revisioner = this };

    [field: AllowNull]
    [field: MaybeNull]
    public virtual EditContextTransition ActorEditContext => field ??= new EditContextTransition() { Revisioner = this };

    [field: AllowNull]
    [field: MaybeNull]
    public virtual ClassValueTransition<RenderFragment> ChildContent =>
        field ??= new ClassValueTransition<RenderFragment>() { Revisioner = this };

    [field: AllowNull]
    [field: MaybeNull]
    public virtual ClassValueTransition<Expression<Func<object>>[]> Routes =>
        field ??= new ClassValueTransition<Expression<Func<object>>[]>() { Revisioner = this };

    public bool IsNewEditContextOfActorAndRootNonNullAndDifferent {
        get {
            if (Interlocked.Or(
                    ref _cacheInvalidationState,
                    (int)CacheInvalidationStates.IsNewEditContextOfActorAndRootNonNullAndDifferent) == 0) {
                field = ActorEditContext.IsNewNonNull && RootEditContext.IsNewNonNull && !ReferenceEquals(
                    ActorEditContext.New,
                    RootEditContext.New);
            }

            return field;
        }
    }

    public bool IsNewEditContextOfActorAndAncestorNonNullAndSame {
        get {
            if (Interlocked.Or(
                    ref _cacheInvalidationState,
                    (int)CacheInvalidationStates.IsNewEditContextOfActorAndAncestorNonNullAndSame) == 0) {
                field = ActorEditContext.IsNewNonNull && AncestorEditContext.IsNewNonNull && ReferenceEquals(
                    ActorEditContext.New,
                    AncestorEditContext.New);
            }

            return field;
        }
    }

    public bool IsNewEditContextOfActorAndAncestorNonNullAndDifferent {
        get {
            if (Interlocked.Or(
                    ref _cacheInvalidationState,
                    (int)CacheInvalidationStates.IsNewEditContextOfActorAndAncestorNonNullAndDifferent) == 0) {
                field = ActorEditContext.IsNewNonNull && AncestorEditContext.IsNewNonNull && !ReferenceEquals(
                    ActorEditContext.New,
                    AncestorEditContext.New);
            }

            return field;
        }
    }

    void IRevisioner.IncrementRevision()
    {
        Interlocked.Increment(ref _revision); // Bump revision
        Interlocked.Exchange(ref _cacheInvalidationState, value: 0); // Invalidate cache
    }

    [Flags]
    private enum CacheInvalidationStates
    {
        IsNewEditContextOfActorAndRootNonNullAndDifferent = 1 << 0,
        IsNewEditContextOfActorAndAncestorNonNullAndSame = 1 << 1,
        IsNewEditContextOfActorAndAncestorNonNullAndDifferent = 1 << 2
    }
}

internal interface IRevisioner
{
    void IncrementRevision();
}
