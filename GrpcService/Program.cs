using Google.Protobuf.WellKnownTypes;
using GrpcService;
using GrpcService.Records;
using GrpcService.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    // Allow both HTTP/1.1 (for REST) and HTTP/2 (for gRPC)
    options.ListenLocalhost(7110, o =>
    {
        o.Protocols = HttpProtocols.Http2; // gRPC requires HTTP/2
        o.UseHttps();                      // gRPC requires TLS by default
    });

    options.ListenLocalhost(5110, o =>
    {
        o.Protocols = HttpProtocols.Http1; // REST can use HTTP/1.1
    });
});

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapGrpcService<GrpcService.Services.ChartService>(); // <-- register your service
app.MapGet("/", () => "Use a gRPC client to communicate with gRPC endpoints.");

// REST equivalents

// 1. Simple RPC -> GET /snapshot
app.MapGet("/snapshot/{partNumber}", (string partNumber) =>
{
    return new
    {
        Timestamp = DateTime.UtcNow,
        PartNumber = partNumber,
        SpecNominal = 10.0,
        SpecTolerance = 0.5,
        Measurement = 9.8
    };
});


// 2. Client-streaming RPC -> POST /measurements
// Accepts a list of DataPoint JSON objects
app.MapPost("/measurements", (List<DataPointDto> points) =>
{
    int count = points.Count;
    double avg = count > 0 ? points.Average(p => p.Measurement) : 0;
    return new SummaryDto(count, avg);
});

// 3. Server-streaming RPC -> GET /monitor
// Returns a stream of DataPoint objects (IAsyncEnumerable)
app.MapGet("/monitor/{partNumber}", async (HttpResponse response, string partNumber) =>
{
    response.ContentType = "application/x-ndjson";
    var rand = new Random();

    for (int i = 0; i < 100000; i++)
    {
        var dp = new DataPointDto(DateTime.UtcNow, partNumber, 10.0, 0.5, 9.5 + rand.NextDouble());
        var json = System.Text.Json.JsonSerializer.Serialize(dp);
        await response.WriteAsync(json + "\n");
        await response.Body.FlushAsync();
    }
});




// 4. Bi-directional streaming RPC -> POST /sendandmonitor
// Accepts a list of DataPoint and returns modified list
app.MapPost("/sendandmonitor", async (List<DataPointDto> points) =>
{
    var rand = new Random();

    // Stream back results one by one
    async IAsyncEnumerable<DataPointDto> Stream()
    {
        foreach (var p in points)
        {
            // simulate processing delay
            await Task.Delay(500);

            yield return new DataPointDto(
                DateTime.UtcNow,
                p.PartNumber,
                p.SpecNominal,
                p.SpecTolerance,
                p.Measurement + rand.NextDouble() * 0.1
            );
        }
    }

    return Stream();
});


app.Run();
