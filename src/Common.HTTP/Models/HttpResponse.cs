using System.Net;

namespace Common.HTTP.Models
{
    public class HttpResponse(string version, HttpStatusCode statusCode)
    {
        public string Version { get; set; } = version;
        public HttpStatusCode StatusCode { get; set; } = statusCode;
        public Dictionary<string, string>? Headers { get; private set; }
        public string? Body { get; private set; }
    }
}
