using Common.HTTP;
using Common.HTTP.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


public class HttpServerOptions
{
    public string? Directory { get; set; }
    public int Port { get; set; }
}

public class HttpServer
{
    private TcpListener server;
    private HttpServerOptions options;
    private readonly ILogger<HttpServer> logger;
    private readonly IHttpReader reader;
    private readonly IHttpWriter writer;
    private bool isRunning;

    public HttpServer(HttpServerOptions options, ILogger<HttpServer> logger)
    {
        // TODO: validate input
        this.logger = logger;
        this.options = options;
        server = new TcpListener(IPAddress.Any, options.Port);
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
                var message = reader.ReadMessage(stream);
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
        logger.LogDebug("Handling message: {Message}", message.Content);
        if (message.RequestUri == null)
            throw new Exception("Request Uri cannot be null");

        var uri = message.RequestUri!.ToString();
        var res = new HttpResponseMessage();
        if (uri == "/")
        {
            res.StatusCode = HttpStatusCode.OK;
            res.Content.Headers.ContentLength = 0;
        }
        else if (uri.StartsWith("/echo"))
        {
            var echo = uri[(uri.IndexOf("/echo") + 6)..];
            res.StatusCode = HttpStatusCode.OK;
            res.Content = new StringContent(echo.ToString(), new MediaTypeHeaderValue("text/plain"));
            res.Content.Headers.ContentLength = echo.Length;
        }
        else if (uri == "/user-agent")
        {
            var uAgent = message.Headers.GetValues("User-Agent").FirstOrDefault();
            res.StatusCode = HttpStatusCode.OK;
            if (!string.IsNullOrWhiteSpace(uAgent))
                res.Content = new StringContent(uAgent, new MediaTypeHeaderValue("text/plain"));
            res.Content.Headers.ContentLength = uAgent!.Length;
        }
        else if (uri == "/files")
        {

            var filename = uri[(uri.IndexOf("/files") + 7)..];
            if (string.IsNullOrEmpty(filename))
            {
                res.StatusCode = HttpStatusCode.BadRequest;
            }
            else if (string.IsNullOrEmpty(options.Directory))
            {
                res.StatusCode = HttpStatusCode.NotImplemented;
            }
            else
            {
                var filepath = Path.Combine(options.Directory!, filename);
                
                if (message.Method == HttpMethod.Get)
                {
                    if (!File.Exists(filepath))
                    {
                        res.StatusCode = HttpStatusCode.NotFound;
                    }
                    else
                    {
                        var fileContent = File.ReadAllText(filepath); // Read the whole file at once for now
                        res.Content = new StringContent(fileContent, new MediaTypeHeaderValue("application/octet-stream"));
                        res.Content.Headers.ContentLength = fileContent.Length;
                        res.StatusCode = HttpStatusCode.OK;
                    }
                }
                else if (message.Method == HttpMethod.Post)
                {
                    if (File.Exists(filepath))
                    {
                        File.Delete(filepath);
                    }
                    File.WriteAllText(filepath, message.Content?.ToString());
                    res.StatusCode = HttpStatusCode.Created;
                }
                else
                {
                    res.StatusCode = HttpStatusCode.BadRequest;
                }
            }
        }
        else
        {
            res.StatusCode = HttpStatusCode.NotFound;
        }

        if ((int)res.StatusCode >= 400 && res.Content.Headers.ContentLength is null)
        {
            res.Content.Headers.ContentLength = 0;
        }

        writer.WriteAll(stream, res);
    }
}