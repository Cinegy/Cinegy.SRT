using System.Net;

namespace Cinegy.Srt.Wrapper;

public interface IConnectedClient
{
    IPEndPoint Address { get; }
}
