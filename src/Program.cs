using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Setup a logger
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Debug);
});


// You can use print statements as follows for debugging, they'll be visible when running tests.

var options = new HttpServerOptions { Port = 4221 };

Console.WriteLine("args: " + string.Join(",", args));
if (args.Length > 0)
{
    if (args[0] == "--directory")
    {
        if (args.Length < 2)
            throw new ArgumentException("Not enough arguments");
        Console.WriteLine("Setting directory: " + args[1]);
        options.Directory = args[1];
    }
}

#if DEBUG
options.Directory = "/temp/";
#endif

var logger = loggerFactory.CreateLogger<HttpServer>();
var server = new HttpServer(options, logger);
server.Start();


