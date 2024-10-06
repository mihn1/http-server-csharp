﻿using System.IO;
using System.Net.Sockets;

namespace Common.HTTP.Contracts
{
    public interface IHttpReader
    {
        HttpRequestMessage ReadMessage(NetworkStream stream);
    }
}
