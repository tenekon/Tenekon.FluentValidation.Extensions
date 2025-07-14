using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public static class RootEditContextPropertyAccessorHolder
{
    internal static readonly object s_rootEditContextPropertyLookupKey = new RootEditContextPropertyLookupKey();

    internal static readonly CounterBasedEditContextPropertyClassValueAccessor<EditContext> s_accessor =
        new(s_rootEditContextPropertyLookupKey);

    private class RootEditContextPropertyLookupKey;
}
