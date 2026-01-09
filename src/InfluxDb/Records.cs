using System;
using System.Collections.Generic;
using InfluxDB.Client.Writes;

namespace ViciOne.Suite.DataPort;

internal record Bucket(string Name);

internal record MeasurementInfo(string Bucket, string Measurement);
internal record RecordInfo(MeasurementInfo MeasurementInfo, string Name, RecordType RecordType, bool IsJsonStructure, bool IsUnknownType);
internal record DataRecord(MeasurementInfo MeasurementInfo, Dictionary<RecordInfo, object?> Records, DateTime TimeStamp);
internal record PointToSend(PointData PointData, string Bucket);
