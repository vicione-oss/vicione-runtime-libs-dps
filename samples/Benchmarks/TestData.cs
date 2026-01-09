namespace Benchmarks;

public class TestData : IInfluxDbThroughput
{
    public int Buckets { get; set; }
    public int Measurements { get; set; }
    public int Tags { get; set; }
    public int Fields { get; set; }
    public int RecordsPerField { get; set; }
    public int BulkSize { get; set; }
    public int FieldsAndTags => Fields + Tags;
}
