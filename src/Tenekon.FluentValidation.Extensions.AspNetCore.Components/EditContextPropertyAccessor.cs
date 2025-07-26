using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public static class EditContextPropertyAccessor
{
    internal static readonly object s_rootEditContextLookupKey = new RootEditContextLookupKey();

    internal static readonly CounterBasedEditContextPropertyClassValueAccessor<EditContext> s_rootEditContext = new(
        s_rootEditContextLookupKey);

    public static bool TryGetRootEditContext(EditContext editContext, [NotNullWhen(returnValue: true)] out EditContext? value) =>
        s_rootEditContext.TryGetPropertyValue(editContext, out value);

    private class RootEditContextLookupKey;
}
