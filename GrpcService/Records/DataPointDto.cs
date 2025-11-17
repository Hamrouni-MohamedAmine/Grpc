namespace GrpcService.Records
{
    public record DataPointDto(DateTime Timestamp, string PartNumber, double SpecNominal, double SpecTolerance, double Measurement);
}
