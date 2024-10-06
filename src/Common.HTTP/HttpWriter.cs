using Common.HTTP.Contracts;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Common.HTTP
{
    public class HttpWriter : IHttpWriter
    {
        public void WriteAll(Stream stream, HttpResponseMessage message)
        {
            stream.Write(GetResponseLineBytes(message));
            stream.Write(GetHeaderBytes(message));
            stream.Write(GetBodyBytes(message));
        }

        private static ReadOnlySpan<byte> GetResponseLineBytes(HttpResponseMessage message)
        {
            var line = $"HTTP/{message.Version}{HttpSemantics.SPACE}" +
                $"{(int)message.StatusCode}{HttpSemantics.SPACE}{HttpSemantics.GetStatusCodeName(message.StatusCode)}" +
                $"{HttpSemantics.NEW_LINE}";
            var bytes = Encoding.ASCII.GetBytes(line);
            return bytes.AsSpan();
        }

        private static ReadOnlySpan<byte> GetHeaderBytes(HttpResponseMessage message)
        {
            StringBuilder sb = new();
            foreach (var header in message.Content.Headers)
            {
                sb.Append(header.Key);
                sb.Append(": ");
                sb.Append(string.Join(",", header.Value));
                sb.Append(HttpSemantics.NEW_LINE);
            }
            sb.Append(HttpSemantics.NEW_LINE);
            var bytes = Encoding.ASCII.GetBytes(sb.ToString());
            return bytes.AsSpan();
        }

        private static ReadOnlySpan<byte> GetBodyBytes(HttpResponseMessage message)
        {
            return message.Content.ReadAsByteArrayAsync().Result;
        }
    }
}
