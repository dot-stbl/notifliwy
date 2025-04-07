using System.Threading.Channels;

namespace Notifliwy.Pipes.InMemory.Options;

/// <summary>
/// In memory exchange options
/// </summary>
public class InMemoryExchangeOptions
{
    /// <summary>
    /// Create channel by this options
    /// </summary>
    public ChannelOptions? ChannelOptions { get; set; } = null;
}