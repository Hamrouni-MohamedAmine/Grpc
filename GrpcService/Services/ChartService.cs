using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcService;
using System.Diagnostics.Metrics;

namespace GrpcService.Services
{
    public class ChartService : RunChart.RunChartBase
    {
        // 1. Simple RPC
        public override Task<DataPoint> GetSnapshot(SnapshotRequest request, ServerCallContext context)
        {
            var dp = new DataPoint
            {
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                PartNumber = request.PartNumber,
                SpecNominal = 10.0,
                SpecTolerance = 0.5,
                Measurement = 9.8
            };
            return Task.FromResult(dp);
        }

        // 2. Client-streaming RPC
        public override async Task<Summary> SendMeasurements(
            IAsyncStreamReader<DataPoint> requestStream,
            ServerCallContext context)
        {
            int count = 0;
            double sum = 0;

            await foreach (var dp in requestStream.ReadAllAsync())
            {
                count++;
                sum += dp.Measurement;
                Console.WriteLine($"Received: {dp.PartNumber} -> {dp.Measurement}");
            }

            return new Summary { Count = count, Average = count > 0 ? sum / count : 0 };
        }

        // 3. Server-streaming RPC
        public override async Task Monitor(SnapshotRequest request,
    IServerStreamWriter<DataPoint> responseStream,
    ServerCallContext context)
        {
            var rand = new Random();
            for (int i = 0; i < 100000; i++)
            {
                await responseStream.WriteAsync(new DataPoint
                {
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                    PartNumber = request.PartNumber,
                    SpecNominal = 10.0,
                    SpecTolerance = 0.5,
                    Measurement = 9.5 + rand.NextDouble()
                });
            }
        }

        // 4. Bi-directional streaming RPC
        public override async Task SendAndMonitor(
            IAsyncStreamReader<DataPoint> requestStream,
            IServerStreamWriter<DataPoint> responseStream,
            ServerCallContext context)
        {
            await foreach (var dp in requestStream.ReadAllAsync())
            {
                Console.WriteLine($"Received: {dp.PartNumber} -> {dp.Measurement}");

                // Echo back with simulated adjustment
                var response = new DataPoint
                {
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                    PartNumber = dp.PartNumber,
                    SpecNominal = dp.SpecNominal,
                    SpecTolerance = dp.SpecTolerance,
                    Measurement = dp.Measurement + 0.1 // simulate feedback
                };

                await responseStream.WriteAsync(response);
            }
        }
    }
}
