using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public static class RootEditContextPropertyAccessorHolder
{
    internal static readonly object s_lookupKey = new LookupKey();

    internal static readonly CounterBasedEditContextPropertyClassValueAccessor<EditContext> s_accessor =
        new(s_lookupKey);

    private class LookupKey;
}
