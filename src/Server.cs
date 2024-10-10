using Common.HTTP;
using Common.HTTP.Contracts;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;


public class HttpServerOptions
{
    public string? Directory { get; set; }
    public int Port { get; set; }
    public string[] SupportedEncodings { get; set; }
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

        var encoding = message.Headers.AcceptEncoding.Select(x => x.Value).Intersect(options.SupportedEncodings).FirstOrDefault();
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
            EncodeStringResponse(res, echo.ToString(), encoding);
        }
        else if (uri == "/user-agent")
        {
            var uAgent = message.Headers.GetValues("User-Agent").FirstOrDefault();
            res.StatusCode = HttpStatusCode.OK;
            EncodeStringResponse(res, uAgent, encoding);
        }
        else if (uri.StartsWith("/files"))
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
                logger.LogInformation("Reading from file {File} - {Exists}", filepath, File.Exists(filepath));
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
                    Directory.CreateDirectory(options.Directory);
                    File.WriteAllBytes(filepath, message.Content?.ReadAsByteArrayAsync()?.Result ?? []);
                    res.StatusCode = HttpStatusCode.Created;
                    res.Content.Headers.ContentLength = 0;
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

    private void EncodeStringResponse(HttpResponseMessage res, string? content, string? encoding)
    {
        if (encoding == "gzip" && content != null)
        {
            // transform content
            res.Content.Headers.Add("Content-Encoding", encoding);
            using MemoryStream memoryStream = new();
            using GZipStream gzipStream = new(memoryStream, CompressionMode.Compress, true);
            byte[] responseBytes = Encoding.UTF8.GetBytes(content);
            gzipStream.Write(responseBytes, 0, responseBytes.Length);

            // Write the compressed data to the response stream.
            byte[] compressedBytes = memoryStream.ToArray();
            content = Convert.ToHexString(compressedBytes);

        }

        if (!string.IsNullOrWhiteSpace(content))
        {
            res.Content = new StringContent(content, new MediaTypeHeaderValue("text/plain"));
            res.Content.Headers.ContentLength = content.Length;
        }
        else
        {
            res.Content.Headers.ContentLength = 0;
        }
    }
}