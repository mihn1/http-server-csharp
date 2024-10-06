using System.Net;

namespace Common.HTTP
{
    public static class HttpSemantics
    {
        public const string NEW_LINE = "\r\n";
        private static readonly byte[] newLineBytes = "\r\n"u8.ToArray();
        public static byte[] NEW_LINE_BYTES => newLineBytes;
        public const char SPACE = ' ';

        public static string GetStatusCodeName(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.NotFound => "Not Found",
                _ => statusCode.ToString(),
            };
        }

    }
}
