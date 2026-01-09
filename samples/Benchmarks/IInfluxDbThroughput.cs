namespace Benchmarks;

internal interface IInfluxDbThroughput
{
    int Buckets { get; set; }
    int Measurements { get; set; }
    int Tags { get; set; }
    int Fields { get; set; }
    int RecordsPerField { get; set; }
}
