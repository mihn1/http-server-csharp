using Common.HTTP.Contracts;
using System.Collections.Immutable;
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
            int bytesRead = 0;

            bytesRead += ReadRequestLine(stream, request);
            bytesRead += ReadHeaders(stream, request);
            bytesRead += ReadBody(stream, request);

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

        private static int ReadHeaders(NetworkStream stream, HttpRequestMessage message)
        {
            int bytesCount = 0;
            ReadOnlySpan<byte> buffer;

            while ((buffer = ReadNextLine(stream)).Length > 0) 
            {
                var parts = Encoding.ASCII.GetString(buffer).Split(':', 2);
                if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
                {
                    throw new Exception("Invalid header");
                }

                message.Headers.Add(parts[0].Trim(), parts[1].Trim());
                bytesCount += buffer.Length;
            }

            return bytesCount;
        }

        private static int ReadBody(NetworkStream stream, HttpRequestMessage message)
        {
            if (!stream.DataAvailable)
                return 0;

            var bytes = ReadNextLine(stream);
            message.Content = new ByteArrayContent(bytes.ToArray());
            return bytes.Length;
        }

        private static ReadOnlySpan<byte> ReadNextLine(Stream stream)
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
            }

            if (nextByte == -1)
            {
                throw new Exception("Connection closed by client");
            }

            return buffer.ToArray();
        }
    }
}
