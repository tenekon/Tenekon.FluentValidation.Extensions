namespace Tenekon.Extensions.FluentValidation.Blazor;

public sealed class Ancestor
{
    internal static Ancestor DirectAncestor { get; } = new() { IsDirectAncestor = true };

    internal bool IsDirectAncestor { get; set; }

    internal Ancestor() { }
}
