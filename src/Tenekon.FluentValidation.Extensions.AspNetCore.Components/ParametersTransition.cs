namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class ParametersTransition : IRevisioner
{
    public static ParametersTransition FirstRender { get; } = new() {
        EditContextualComponentBase = new object(),
        RootEditContextTransition = new EditContextTransition {
            IsFirstTransition = false
        },
        AncestorEditContextTransition = new EditContextTransition {
            IsFirstTransition = false
        },
        ActorEditContextTransition = new EditContextTransition {
            IsFirstTransition = false
        }
    };

    private int _revision;
    private int _cacheInvalidationState;

    internal int Revision => Volatile.Read(ref _revision);

    public bool IsDisposing { get; init; }

    public virtual required object EditContextualComponentBase {
        get {
            if (field is null) {
                throw new InvalidOperationException(
                    $"You are accessing the parameters transition intended for first render before OnParametersSet[Async], thus the field member is not yet available: {nameof(EditContextualComponentBase)}");
            }

            return field;
        }
        set;
    }

    public required EditContextTransition RootEditContextTransition {
        get;
        init {
            value.Revisioner = this;
            field = value;
        }
    }

    public required EditContextTransition AncestorEditContextTransition {
        get;
        init {
            value.Revisioner = this;
            field = value;
        }
    }

    public required EditContextTransition ActorEditContextTransition {
        get;
        init {
            value.Revisioner = this;
            field = value;
        }
    }

    public bool IsNewEditContextOfActorAndRootNonNullAndDifferent {
        get {
            if (Interlocked.Or(
                    ref _cacheInvalidationState,
                    (int)CacheInvalidationStates.IsNewEditContextOfActorAndRootNonNullAndDifferent) == 0) {
                field = ActorEditContextTransition.IsNewNonNull && RootEditContextTransition.IsNewNonNull && !ReferenceEquals(
                    ActorEditContextTransition.New,
                    RootEditContextTransition.New);
            }

            return field;
        }
    }

    public bool IsNewEditContextOfActorAndAncestorNonNullAndSame {
        get {
            if (Interlocked.Or(
                    ref _cacheInvalidationState,
                    (int)CacheInvalidationStates.IsNewEditContextOfActorAndAncestorNonNullAndSame) == 0) {
                field = ActorEditContextTransition.IsNewNonNull && AncestorEditContextTransition.IsNewNonNull && ReferenceEquals(
                    ActorEditContextTransition.New,
                    AncestorEditContextTransition.New);
            }

            return field;
        }
    }

    public bool IsNewEditContextOfActorAndAncestorNonNullAndDifferent {
        get {
            if (Interlocked.Or(
                    ref _cacheInvalidationState,
                    (int)CacheInvalidationStates.IsNewEditContextOfActorAndAncestorNonNullAndDifferent) == 0) {
                field = ActorEditContextTransition.IsNewNonNull && AncestorEditContextTransition.IsNewNonNull && !ReferenceEquals(
                    ActorEditContextTransition.New,
                    AncestorEditContextTransition.New);
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
