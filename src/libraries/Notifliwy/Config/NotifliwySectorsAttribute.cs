using System;

namespace Notifliwy.Config;

/// <summary>
/// Marks an assembly for Notifliwy sector source generation. When the
/// <c>Notifliwy.Generators</c> source generator sees this attribute it emits a
/// <c>NotifliwySectorsRegistration.AddNotifliwySectors(...)</c> extension that
/// registers every sector config class in the assembly with zero runtime reflection.
/// </summary>
/// <remarks>
/// <para>
/// Apply once per assembly containing sector configuration classes:
/// <code>
/// [assembly: NotifliwySectors]
/// </code>
/// </para>
/// <para>
/// The generator picks up classes implementing
/// <see cref="Interfaces.INotificationSectorConfig{TNotification,TEvent}"/> that are
/// concrete, closed and visible to generated code (<see langword="public"/> or
/// <see langword="internal"/>, not nested inside inaccessible types). Abstract,
/// open-generic and private classes are skipped. Config classes may take constructor
/// dependencies registered in DI.
/// </para>
/// <para>
/// The opt-in reflection fallback
/// <see cref="Builders.NotificationServerBuilder.AddSectorsFromAssembly"/> discovers
/// only <see langword="public"/> config classes and logs a startup warning; prefer
/// this attribute and the generated registration.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class NotifliwySectorsAttribute : Attribute;
