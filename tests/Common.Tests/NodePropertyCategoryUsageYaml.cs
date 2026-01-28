using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.Suite.DataPort;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via YAML deserializer (reflection).")]
public class NodePropertyCategoryUsageYaml
{
    public string Id { get; set; } = string.Empty;
    public List<NodePropertyUsageYaml>? Properties { get; internal set; }
}

