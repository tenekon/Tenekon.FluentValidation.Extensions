using Microsoft.AspNetCore.Components.Forms;
using Tenekon.Extensions.FluentValidation.Blazor.Interception.Attachment;
using Tenekon.Extensions.FluentValidation.Blazor.Interception.Diagnostics;

namespace Tenekon.Extensions.FluentValidation.Blazor.Interception.Mutators.Descendant;

internal sealed class DescendantMessageFieldIdentifierComparer(ValidationMessageStore validationMessageStore)
    : IEqualityComparer<FieldIdentifier>, IAccessLogCarrier
{
    public IAccessLogger? AccessLogger { get; set; }

    public bool Equals(FieldIdentifier x, FieldIdentifier y) => x.Equals(y);

    public int GetHashCode(FieldIdentifier obj)
    {
        AccessLogger?.LogValidationMessageStoreAccess(obj);
        MirroredFieldStateSynchronizer.TryDetachMirroredValidationMessageStore(obj, validationMessageStore);
        return obj.GetHashCode();
    }
}
