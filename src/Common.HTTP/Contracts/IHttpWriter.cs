using System.Net.Sockets;

namespace Common.HTTP.Contracts
{
    public interface IHttpWriter
    {
        void WriteAll(NetworkStream stream, HttpResponseMessage message);
    }
}
