using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace PlayBlazor.Rendering;

/// <summary>Creates EventCallback/EventCallback&lt;T&gt; instances that forward every invocation to a handler.</summary>
public static class EventCallbackInterceptor
{
    private static readonly object Receiver = new();

    private static readonly MethodInfo CreateTypedMethod = typeof(EventCallbackInterceptor)
        .GetMethod(nameof(CreateTyped), BindingFlags.NonPublic | BindingFlags.Static)!;

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
