namespace Common.HTTP
{
    public class HttpRequest
    {
        public HttpMethod? Method;

        public string? Url;

        public Version? Version;

        public Dictionary<string, string>? Headers;

        public string? Body;

        public int BytesCount;
    }
}
