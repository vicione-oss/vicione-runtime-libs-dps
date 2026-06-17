using System.Text.Json.Serialization;

namespace ViciOne.Suite.System.Contracts;

[JsonDerivedType(typeof(OnChangedTransferMode), nameof(OnChangedTransferMode))]
[JsonDerivedType(typeof(CyclicTransferMode), nameof(CyclicTransferMode))]
[JsonDerivedType(typeof(PeriodicTransferMode), nameof(PeriodicTransferMode))]
public interface ITransferMode { }
