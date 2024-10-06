using Common.HTTP.Contracts;
using System;
using System.IO;
using System.Net.Http;
using System.Text;

namespace Common.HTTP
{
    public class HttpReader : IHttpReader
    {
        public HttpRequest Read(Stream stream)
        {
            // TODO: read a message properly with ending character detection
            var message = new HttpRequest();

            ReadLine(stream, message);
            ReadHeaders(stream, message);
            ReadBody(stream, message);

            //int bytesRead;
            //while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) >= 0)
            //{
            //    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            //    return message;
            //}

            return message;
        }

        private int ReadLine(Stream stream, HttpRequest message)
        {
            var bytes = ReadNextLine(stream);
            var parts = Encoding.UTF8.GetString(bytes).Split(HttpSemantics.SPACE);
            if (parts?.Length != 3)
            {
                throw new Exception("Invalid HTTP line");
            }

            message.Method = HttpMethod.Parse(parts[0]);
            message.Url = Helpers.ParseUrl(parts[1]);
            message.Version = Helpers.ParseVersion(parts[2]);

            return bytes.Length;
        }

        private void ReadHeaders(Stream stream, HttpRequest httpRequest)
        {

        }

        private void ReadBody(Stream stream, HttpRequest message)
        {

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
