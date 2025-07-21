namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class ParametersTransitionHandlerRegistry
{
    private readonly List<RegistrationItem> _handlerList = [];

    public void RegisterHandler(Action<ParametersTransition> handler, HandlerAddingPosition addingPosition)
    {
        if (addingPosition == HandlerAddingPosition.End) {
            _handlerList.Add(new RegistrationItem(handler));
            return;
        }

        if (addingPosition == HandlerAddingPosition.Start) {
            _handlerList.Insert(0, new RegistrationItem(handler));
            return;
        }

        throw new ArgumentException($"Unknown handler adding position: {addingPosition}");
    }

    internal IEnumerable<RegistrationItem> GetRegistrationItems() => _handlerList;

    public void RemoveHandler(Action<ParametersTransition> handler)
    {
        var index = _handlerList.FindLastIndex(x => ReferenceEquals(x.Handler, handler));
        if (index == -1) {
            throw new InvalidOperationException("The specified handler was not found in the handler list.");
        }
        _handlerList.RemoveAt(index);
    }

    internal record RegistrationItem(Action<ParametersTransition> Handler);
}
