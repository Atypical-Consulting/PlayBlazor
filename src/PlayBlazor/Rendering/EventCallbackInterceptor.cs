using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace PlayBlazor.Rendering;

/// <summary>Creates EventCallback/EventCallback&lt;T&gt; instances that forward every invocation to a handler.</summary>
public static class EventCallbackInterceptor
{
    private static readonly object Receiver = new();

    private static readonly MethodInfo CreateTypedMethod = typeof(EventCallbackInterceptor)
        .GetMethod(nameof(CreateTyped), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>Builds a callback of the requested type that forwards every invocation to <paramref name="handler" />.</summary>
    /// <param name="callbackType">
    /// <see cref="EventCallback" /> or a closed <see cref="EventCallback{TValue}" />; the value is boxed
    /// so it can be placed in a parameter dictionary.
    /// </param>
    /// <param name="handler">Receives the callback argument, or <c>null</c> for the non-generic form.</param>
    /// <returns>The boxed callback, ready to assign to the component parameter.</returns>
    public static object Create(Type callbackType, Action<object?> handler)
    {
        if (callbackType == typeof(EventCallback))
        {
            return EventCallback.Factory.Create(Receiver, () => handler(null));
        }

        var argumentType = callbackType.GetGenericArguments()[0];
        return CreateTypedMethod.MakeGenericMethod(argumentType).Invoke(null, [handler])!;
    }

    private static EventCallback<TValue> CreateTyped<TValue>(Action<object?> handler)
        => EventCallback.Factory.Create<TValue>(Receiver, (TValue value) => handler(value));
}
