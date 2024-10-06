using Common.HTTP;
using Common.HTTP.Contracts;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class HttpServer
{
    private TcpListener server;
    private readonly ILogger logger;
    private readonly IHttpReader reader;
    private bool isRunning;

    public HttpServer(int port, ILogger logger)
    {
        // TODO: validate input
        this.logger = logger;
        server = new TcpListener(IPAddress.Any, port);
        reader = new HttpReader();
    }

    public void Start()
    {
        if (isRunning)
        {
            logger.LogInformation("Server is already running");
            isRunning = true;
        }
        server.Start();
        logger.LogInformation("Listening from {Port}", server.LocalEndpoint.ToString());
        isRunning = true;

        while (isRunning)
        {
            try
            {
                var client = server.AcceptTcpClient();
                var clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
            catch (Exception ex)
            {
                logger.LogError("Error starting new thread for client: {Message}", ex.Message);
            }
        }
    }

    private void HandleClient(TcpClient client)
    {
        try
        {
            var stream = client.GetStream();
            while (true)
            {
                var request = reader.Read(stream);
                HandleRequest(stream, request);
            }
        }
        catch (Exception ex)
        {
            logger.LogInformation("Error handling from client: {Message}", ex.Message);
        }
        finally
        {
            client.Dispose();
        }

    }

    private void HandleRequest(NetworkStream stream, HttpRequest request)
    {
        logger.LogDebug("Handling message: {Message}", JsonSerializer.Serialize(request));

        // Hard code response message for now
        var resMessage = new StringBuilder();
        // append headers
        resMessage.Append("HTTP/1.1");
        resMessage.Append(HttpSemantics.SPACE);

        if (request.Url == "/")
        {
            resMessage.Append((int)HttpStatusCode.OK);
            resMessage.Append(HttpSemantics.SPACE);
            resMessage.Append(HttpStatusCode.OK.ToString());
        }
        else
        {
            resMessage.Append((int)HttpStatusCode.NotFound);
            resMessage.Append(HttpSemantics.SPACE);
            resMessage.Append(HttpStatusCode.NotFound.ToString());
        }

        resMessage.Append(HttpSemantics.NEW_LINE);
        resMessage.Append(HttpSemantics.NEW_LINE);

        var resBytes = Encoding.ASCII.GetBytes(resMessage.ToString());
        stream.Write(resBytes);
    }
}