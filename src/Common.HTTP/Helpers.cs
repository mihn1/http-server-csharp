using System;
using System.Net;

namespace Common.HTTP
{
    internal static class Helpers
    {
        public static Version ParseVersion(ReadOnlySpan<char> version)
        {
            return version switch
            {
                "HTTP/1.1" => HttpVersion.Version11,
                "HTTP/1.0" => HttpVersion.Version10,
                "HTTP/2.0" => HttpVersion.Version20,
                "HTTP/3.0" => HttpVersion.Version30,
                _ => HttpVersion.Unknown,
            };
        }

        public static string ParseUrl(string? url)
        {

            ArgumentException.ThrowIfNullOrWhiteSpace(url);
            return url;
        }
    }
}
