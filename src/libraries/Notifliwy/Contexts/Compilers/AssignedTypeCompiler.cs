using System;
using Notifliwy.Extensions;
using System.Collections.Generic;

namespace Notifliwy.Contexts.Compilers;

internal static class AssignedTypeCompiler
{
    /// <summary>
    /// Add binding <typeparamref name="TNotification"/> to global <typeparamref name="TEvent"/>
    /// </summary>
    public static void AddBindings<TNotification, TEvent>(this IDictionary<Type, HashSet<Type>> bindingType)
    {
        (Type EventType, Type NotificationType) typeTuple = (typeof(TEvent), typeof(TNotification));

        bindingType.AddOrUpdate(
            typeTuple.EventType,
            _ => [typeTuple.NotificationType],
            (_, existList) =>
            {
                existList.Add(typeTuple.NotificationType);
                return existList;
            });
    }
}