﻿using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal class ParameterSetTransitionHandlerRegistry
{
    private readonly List<RegistrationItem> _handlerList = [];

    public void RegisterHandler(
        Action<EditContextualComponentBaseParameterSetTransition> handler,
        HandlerInsertPosition insertPosition,
        [CallerArgumentExpression(nameof(handler))] string? handlerArgumentExpression = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (insertPosition == HandlerInsertPosition.After) {
            _handlerList.Add(new RegistrationItem(handler, handlerArgumentExpression));
            return;
        }

        if (insertPosition == HandlerInsertPosition.Before) {
            _handlerList.Insert(index: 0, new RegistrationItem(handler, handlerArgumentExpression));
            return;
        }

        throw new ArgumentException($"Unknown handler insert position: {insertPosition}");
    }

    public void RegisterHandler(
        Action<EditContextualComponentBaseParameterSetTransition> handler,
        HandlerInsertPosition insertPosition,
        Action<EditContextualComponentBaseParameterSetTransition> relativeHandler,
        [CallerArgumentExpression(nameof(handler))] string? handlerArgumentExpression = null)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(relativeHandler);

        var index = _handlerList.FindLastIndex(x => ReferenceEquals(x.Handler, relativeHandler));
        if (index == -1) {
            throw new InvalidOperationException("The specified relative handler was not found in the handler list.");
        }

        var insertIndex = insertPosition switch {
            HandlerInsertPosition.After => index + 1,
            HandlerInsertPosition.Before => index,
            _ => throw new ArgumentException($"Unknown handler insert position: {insertPosition}")
        };

        _handlerList.Insert(insertIndex, new RegistrationItem(handler, handlerArgumentExpression));
    }

    internal IEnumerable<RegistrationItem> GetRegistrationItems() => _handlerList;

    public void RemoveHandler(Action<EditContextualComponentBaseParameterSetTransition> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var index = _handlerList.FindLastIndex(x => ReferenceEquals(x.Handler, handler));
        if (index == -1) {
            throw new InvalidOperationException("The specified handler was not found in the handler list.");
        }
        _handlerList.RemoveAt(index);
    }

    [DebuggerDisplay("{_handlerArgumentExpression}")]
    internal class RegistrationItem(Action<EditContextualComponentBaseParameterSetTransition> handler, string? handlerArgumentExpression)
    {
        private readonly string? _handlerArgumentExpression = handlerArgumentExpression;

        public Action<EditContextualComponentBaseParameterSetTransition> Handler { get; } = handler;
    }
}
