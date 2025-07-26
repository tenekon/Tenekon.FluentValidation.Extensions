namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class ClassValueTransition<T> : ValueTransition<T> where T : class
{
    private bool? _isOldReferenceEqualsToNew;
    private bool? _isOldReferenceDifferentToNew;
    private bool? _isNewNullStateChanged;

    public override bool IsNewSame => _isOldReferenceEqualsToNew ??= ReferenceEquals(Old, NewOrNull);

    public override bool IsNewDifferent => _isOldReferenceDifferentToNew ??= !ReferenceEquals(Old, NewOrNull);

    public virtual bool IsNewNullStateChanged => _isNewNullStateChanged ??= IsNewNonNull != IsOldNonNull;

    protected override void InvalidateCacheCore()
    {
        _isOldReferenceEqualsToNew = null;
        _isOldReferenceDifferentToNew = null;
        _isNewNullStateChanged = null;
        base.InvalidateCacheCore();
    }
}
