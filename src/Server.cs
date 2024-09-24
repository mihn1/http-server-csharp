using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class HttpServer
{
    private TcpListener? server;
    private readonly int port;
    private readonly ILogger logger;
    private bool isRunning;

    public HttpServer(int port, ILogger logger)
    {
        // TODO: validate input
        this.port = port;
        this.logger = logger;
    }

    public void Start()
    {
        if (isRunning)
        {
            logger.LogInformation("Server is already running");
            isRunning = true;
        }
        server = new TcpListener(IPAddress.Any, port);
        server.Start();
        logger.LogInformation("Listening from {Port}", port);
        this.isRunning = true;
        StartListening();
    }

    private void StartListening()
    {
        while (isRunning)
        {
            TcpClient? client = null;
            try
            {
                client = server!.AcceptTcpClient();
                var clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
            catch (Exception ex)
            {
                logger.LogWarning("Error handling client: {Message}", ex.Message);
                throw;
            }
            finally
            {
                client?.Close();
            }
        }
    }

    private void HandleClient(TcpClient client)
    {
        var stream = client.GetStream();
        var buffer = new byte[1024];
        try
        {
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                // Handle message
                HandleMessage(stream, message);
            }
        }
        catch
        {

        }
    }

    private const string NEW_LINE = "\r\n";
    private const char SPACE = ' ';

    private void HandleMessage(NetworkStream stream, string message)
    {
        logger.LogDebug("Handling message: {Message}", message);
        // Hard code response message for now
        var resMessage = new StringBuilder();
        // append headers
        resMessage.Append("HTTP/1.1");
        resMessage.Append(SPACE);
        resMessage.Append("200");
        resMessage.Append(SPACE);
        resMessage.Append("OK");
        resMessage.Append(NEW_LINE);
        resMessage.Append(NEW_LINE);

        var resBytes = Encoding.ASCII.GetBytes(resMessage.ToString());
        stream.Write(resBytes);
    }
}