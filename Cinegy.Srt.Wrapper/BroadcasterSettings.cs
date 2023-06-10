namespace Cinegy.Srt.Wrapper;

public sealed record BroadcasterSettings
{
    public int ClientsPerThread { get; set; } = 20;
}
