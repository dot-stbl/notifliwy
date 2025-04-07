using System;
using Notifliwy.Extensions;
using System.Collections.Generic;
using Notifliwy.Models.Interfaces;

namespace Notifliwy.Contexts.Compilers;

internal static class AssignedTypeCompiler
{
    /// <summary>
    /// Add binding <typeparamref name="TNotification"/> to global <typeparamref name="TEvent"/>
    /// </summary>
    public static void AddBindings<TNotification, TEvent>(this IDictionary<Type, HashSet<Type>> bindingType)
        where TNotification : INotification
        where TEvent : IEvent
    {
        (Type EventType, Type NotificationType) typeTuple = (typeof(TEvent), typeof(TNotification));
        
        bindingType.AddOrUpdate(
            typeTuple.EventType,
            addValueFactory: _ => [ typeTuple.NotificationType ],
            updateValueFactory: (_, existList) =>
            {
                existList.Add(typeTuple.NotificationType);
                return existList;
            });
    }
}