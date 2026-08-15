using System;
using Notifliwy.Related;

namespace Notifliwy.Exceptions;

/// <summary>
/// Is <see cref="MultiplyServiceInstance{TInstance}"/> have zero instance count
/// </summary>
public class EmptyInstanceBranchException(Type instanceType)
        : InvalidOperationException($"Multiply container have 0 instances of {instanceType}");