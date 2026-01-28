using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.Suite.DataPort;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via YAML deserializer (reflection).")]
public class NodeYaml
{
    public string Id { get; set; } = string.Empty;
    public string MappingId { get; set; } = string.Empty;
    public List<NodePropertyCategoryUsageYaml>? PropertyCategories { get; internal set; }
    public List<ChildNodeRefYaml>? ChildNodes { get; internal set; }
}

