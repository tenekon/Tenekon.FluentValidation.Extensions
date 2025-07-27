using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal sealed class EditContextTransition : ClassValueTransition<EditContext>
{
    private bool? _isOldReferenceEqualsToNew;
    private bool? _isOldReferenceDifferentToNew;

    public override bool IsNewSame =>
        _isOldReferenceEqualsToNew ??= ReferenceEquals(Old, NewOrNull) && ReferenceEquals(Old?.Model, NewOrNull?.Model);

    public override bool IsNewDifferent =>
        _isOldReferenceDifferentToNew ??= !ReferenceEquals(Old, NewOrNull) || !ReferenceEquals(Old?.Model, NewOrNull?.Model);

    protected override void InvalidateCacheCore()
    {
        _isOldReferenceEqualsToNew = null;
        _isOldReferenceDifferentToNew = null;
        base.InvalidateCacheCore();
    }
}
