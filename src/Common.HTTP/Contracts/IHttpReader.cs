using System.IO;

namespace Common.HTTP.Contracts
{
    public interface IHttpReader
    {
        HttpRequest Read(Stream stream);
    }
}
