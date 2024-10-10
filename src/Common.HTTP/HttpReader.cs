using Common.HTTP.Contracts;
using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;

namespace Common.HTTP
{
    public class HttpReader : IHttpReader
    {
        public HttpRequestMessage ReadMessage(NetworkStream stream)
        {
            // TODO: read a message properly with ending character detection
            var request = new HttpRequestMessage();

            _ = ReadRequestLine(stream, request);
            var (_, contentHeaders) = ReadHeaders(stream, request);
            _ = ReadBody(stream, request, contentHeaders);

            return request;
        }

        private static int ReadRequestLine(NetworkStream stream, HttpRequestMessage message)
        {
            var bytes = ReadNextLine(stream);
            var parts = Encoding.ASCII.GetString(bytes).Split(HttpSemantics.SPACE);
            if (parts?.Length != 3)
            {
                throw new Exception("Invalid HTTP line");
            }

            message.Method = HttpMethod.Parse(parts[0]);
            message.RequestUri = new Uri(Helpers.ParseUrl(parts[1]), UriKind.RelativeOrAbsolute);
            message.Version = Helpers.ParseVersion(parts[2]);

            return bytes.Length;
        }

        private static (int, Dictionary<string, string>) ReadHeaders(NetworkStream stream, HttpRequestMessage message)
        {
            int bytesCount = 0;
            ReadOnlySpan<byte> buffer;
            var contentHeaders = new Dictionary<string, string>();

            while ((buffer = ReadNextLine(stream)).Length > 0) 
            {
                var parts = Encoding.ASCII.GetString(buffer).Split(':', 2);
                if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
                {
                    throw new Exception("Invalid header");
                }

                var name = parts[0]!.Trim().ToLower();
                var val = parts[1]!.Trim();
                if (name.StartsWith("content"))
                {
                    if (message.Content == null)
                    {
                        contentHeaders[name] = val;
                    }
                }
                message.Headers.TryAddWithoutValidation(name, val);
                bytesCount += buffer.Length;
            }

            return (bytesCount, contentHeaders);
        }

        private static int ReadBody(NetworkStream stream, HttpRequestMessage message, Dictionary<string, string> contentHeaders)
        {
            if (!stream.DataAvailable)
                return 0;

            int totalBytes = -1;
            if (contentHeaders.TryGetValue("content-length", out string? value) && int.TryParse(value, out int parsedValue))
                totalBytes = parsedValue;

            var bytes = ReadNextLine(stream, totalBytes);
            message.Content = new ByteArrayContent(bytes.ToArray());
            return bytes.Length;
        }

        private static ReadOnlySpan<byte> ReadNextLine(NetworkStream stream, int count = -1)
        {
            using MemoryStream buffer = new();
            int nextByte;
            int prevByte = 0;

            while ((nextByte = stream.ReadByte()) != -1)
            {
                if (nextByte == '\n')
                {
                    if (prevByte == '\r')
                    {
                        // Unwrite the \n byte which has written to the buffer stream
                        buffer.Position--; 
                        buffer.SetLength(buffer.Position);
                        break;
                    }
                }

                buffer.WriteByte((byte)nextByte);
                prevByte = nextByte;

                if (count > 0 && buffer.Position >= count)
                {
                    break;
                }
            }

            if (nextByte == -1)
            {
                throw new Exception("Connection closed by client");
            }

            return buffer.ToArray();
        }
    }
}
