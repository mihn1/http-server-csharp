using Common.HTTP;
using Common.HTTP.Contracts;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class HttpServer
{
    private TcpListener server;
    private readonly ILogger<HttpServer> logger;
    private readonly IHttpReader reader;
    private readonly IHttpWriter writer;
    private bool isRunning;

    public HttpServer(int port, ILogger<HttpServer> logger)
    {
        // TODO: validate input
        this.logger = logger;
        server = new TcpListener(IPAddress.Any, port);
        reader = new HttpReader();
        writer = new HttpWriter();
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
                var message = reader.Read(stream);
                HandleRequest(stream, message);
            }
        }
        catch (Exception ex)
        {
            logger.LogInformation("Error handling from client: {ex}", ex);
        }
        finally
        {
            client.Dispose();
        }

    }

    private void HandleRequest(NetworkStream stream, HttpRequestMessage message)
    {
        logger.LogDebug("Handling message: {Message}", JsonSerializer.Serialize(message));
        if (message.RequestUri == null)
            throw new Exception("Request Uri cannot be null");

        var uri = message.RequestUri!.ToString().AsSpan();
        var res = new HttpResponseMessage();
        if (uri == "/")
        {
            res.StatusCode = HttpStatusCode.OK;
        }
        else if (uri.StartsWith("/echo"))
        {
            var path = uri[(uri.IndexOf("/echo") + 6)..];
            res.StatusCode = HttpStatusCode.OK;
            res.Content = new StringContent(path.ToString(), new MediaTypeHeaderValue("text/plain"));
        }
        else if (uri == "/user-agent")
        {
            string uAgent = message.Headers.GetValues("User-Agent").FirstOrDefault() ?? "";
            res.StatusCode = HttpStatusCode.OK;
            res.Content = new StringContent(uAgent, new MediaTypeHeaderValue("text/plain"));
        }
        else 
        {
            res.StatusCode = HttpStatusCode.NotFound;
        }

        writer.WriteAll(stream, res);
    }
}