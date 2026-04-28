# DataPort.Extensions.Testing

Testing utilities for ViciOne DataPort.Extensions providing specialized test helpers for asynchronous assertions and YAML configuration consistency validation.

## Overview

This package provides two main testing capabilities:

1. **Eventual Assertions** - Test helpers for asserting conditions that should eventually become true within a timeout period
2. **YAML Consistency Testing** - Base test infrastructure for validating consistency between YAML configuration files and C# DataPort implementations

## Installation

```bash
dotnet add package ViciOne.Suite.DataPort.Extensions.Testing
```

## Features

### 1. Eventual Assertions

Helper methods for testing asynchronous or eventual conditions. Instead of using `Thread.Sleep()` or manual polling loops, use these assertions to wait for conditions to become true within a configurable timeout.

**Key Components:**
- `EventualAssertions` - Static helper methods for creating eventual value wrappers
- `EventualValue<T>` - Wrapper that defers value evaluation until assertion time
- `EventualValueAssertions<T>` - Fluent assertion API for eventual values
- `EventualBooleanAssertions` - Specialized assertions for boolean conditions

**Default Timeout:** 5000ms (5 seconds)

> **Note:** This implementation may be replaced in the future with the [please-wait](https://github.com/MartynNevers/please-wait) library, which provides similar functionality with additional features.

**Usage Examples:**

```csharp
using ViciOne.Suite.DataPort.Extensions.Testing.Assertions;

// Assert a value eventually equals expected value
Eventually(() => callCount).Should().Be(21, "retries should complete");

// Assert eventual boolean condition
Eventually(() => isComplete).Should().BeTrue("operation should finish");

// Use custom timeout (10 seconds)
Eventually(() => items.Count, timeoutMs: 10000)
    .Should().Be(5, "items should be added");

// Assert custom condition
Eventually(() => status)
    .Should().Match(s => s.StartsWith("success"), "status should indicate success");
```

### 2. YAML Consistency Testing

Base test infrastructure for validating that YAML configuration files remain in sync with their corresponding C# DataPort implementation classes. This helps catch configuration drift during development.

> **Important:** This testing framework requires the use of the typed-node system from DataPort.Extensions. It will not work with DataPort implementations that don't use the typed nodes, mappers, and YAML configuration structure.

**Key Components:**

- `YamlConsistencyBaseTest<TCommunicationType, TYamlConfig, TRootConfigurationNode>` - Abstract base class providing test methods
- `IYamlTestingConfig<TCommunication, TRootConfigurationNode>` - Configuration interface for specifying YAML file and mappers
- `DataPortCommunicationExtensions` - Reflection helpers for analyzing Communication classes
- `RulesetExtensions` - Query helpers for analyzing YAML rulesets

**What Gets Validated:**

1. YAML file validity
2. Communication attribute ID matches YAML root ID
3. YAML DataPort node properties exist in Communication class
4. Communication properties exist in YAML device nodes
5. Configuration node mappers have corresponding YAML nodes
6. YAML configuration nodes have corresponding mappers
7. Mapper properties exist in YAML nodes
8. YAML node properties exist in mappers
9. DataPoint node mappers have corresponding YAML nodes
10. YAML data point nodes have corresponding mappers
11. DataPoint mapper properties exist in YAML nodes
12. YAML data point properties exist in mappers

**Setup Example:**

```csharp
using ViciOne.Suite.DataPort.Extensions.Testing.YamlTesting;
using ViciOne.Suite.DataPort.Extensions.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.TypedNodes.Mapping;

// Define your YAML testing configuration
public class MyYamlTestConfig : IYamlTestingConfig<MyCommunication, MyRootConfigurationNode>
{
    public static string YamlFileName => "my-device.yaml";

    public static TypedNodeMapper<MyCommunication, MyRootConfigurationNode> Mapper =>
        MyTypedNodeMapper.Instance;

    public static HashSet<string> ExcludedCommunicationProperties =>
        new() { "InternalComputedProperty" };
}

// Create your test class
public class MyYamlConsistencyTests
    : YamlConsistencyBaseTest<MyCommunication, MyYamlTestConfig, MyRootConfigurationNode>
{
    // All test methods are inherited from base class
    // Tests run automatically using xUnit Theory with dynamically generated test cases
}
```

No additional test methods are needed. The base class provides all twelve checks as xUnit `[Theory]` test cases with dynamically generated test case data.

### Excluding node properties from mapper ↔ YAML checks

The YAML consistency tests compare properties declared in YAML nodes against the properties reported by each mapper's `PropertyNamesDefinedInTargetNode()`. If a typed node has C# properties that are not backed by a YAML property (e.g. computed or runtime-only properties), the consistency check will fail because it expects a 1:1 match.

To handle this, use `ExcludedConfigurationNodeProperties` or `ExcludedDataPointNodeProperties` on the test config. They map the typed node `Type` to a set of property names to exclude from the check.

```csharp
public class MyYamlTestConfig : IYamlTestingConfig<MyCommunication, MyRootConfigurationNode>
{
    public static string YamlFileName => "my-device.yaml";

    public static TypedNodeMapper<MyCommunication, MyRootConfigurationNode> Mapper =>
        MyTypedNodeMapper.Instance;

    public static HashSet<string> ExcludedCommunicationProperties => [];

    public static Dictionary<Type, HashSet<string>> ExcludedConfigurationNodeProperties => new()
    {
        [typeof(MyConfigNode)] = [nameof(MyConfigNode.ComputedFullPath)],
    };

    public static Dictionary<Type, HashSet<string>> ExcludedDataPointNodeProperties => new()
    {
        [typeof(MyDataPointNode)] = [nameof(MyDataPointNode.RuntimeTimestamp)],
    };
}
```

This is similar to `ExcludedCommunicationProperties` (which excludes properties on the `DataPortCommunication` subclass) but applies to individual typed nodes, keyed by their node type.

### Test methods

The base class provides the following xUnit test methods:

- `IsYamlFileValid()` - Validates YAML syntax and structure
- `CommunicationAttributeMatchesYamlRootId()` - Ensures Communication attribute matches YAML
- `YamlDataPortNodePropertiesExistInCommunicationClass(DataPortNodeType)` - Theory test for each YAML node
- `CommunicationPropertyExistsInYamlDeviceNodes(string)` - Theory test for each Communication property
- `ConfigurationNodeMapperHasCorrespondingYamlConfigurationNode(IBranchConfigurationNodeMapper)` - Theory test for each mapper
- `YamlConfigurationNodeHasCorrespondingConfigurationNodeMapper(string)` - Theory test for each YAML config node
- `ConfigurationMapperPropertyExistsInYamlNode(ConfigurationMapperPropertyTestCase)` - Theory test for each mapper property
- `YamlConfigurationNodePropertyExistsInConfigurationMapper(YamlNodePropertyTestCase)` - Theory test for each YAML property
- `DataPointNodeMapperHasCorrespondingYamlDataPointNode(IDataPointNodeMapper)` - Theory test for each data point mapper
- `YamlDataPointNodeHasCorrespondingDataPointNodeMapper(string)` - Theory test for each YAML data point
- `DataPointMapperPropertyExistsInYamlNode(DataPointMapperPropertyTestCase)` - Theory test for each data point property
- `YamlDataPointNodePropertyExistsInDataPointMapper(YamlNodePropertyTestCase)` - Theory test for each YAML data point property

### Extension Methods

> **Note:** These extension methods could potentially be moved directly to their source classes (`Ruleset`, `NodeType`, `DataPortCommunication`) in future refactoring for better discoverability and maintainability.

**RulesetExtensions:**
Helper methods for querying YAML rulesets in tests.

```csharp
var ruleset = RulesDeserializer.Deserialize("my-config.yaml");

// Get root information
string rootId = ruleset.GetRootId();
List<DataPortNodeType> rootNodes = ruleset.GetRootNodes();

// Get configuration nodes (nodes with children)
List<DataPortTreeNodeType> configNodes = ruleset.GetConfigurationNodes();
List<string> configMappingIds = ruleset.GetConfigurationNodesMappingIds();
DataPortTreeNodeType? configNode = ruleset.GetConfigurationNodeByMappingId("my-mapping-id");

// Get data point nodes (leaf nodes without children)
List<DataPortTreeNodeType> dataPointNodes = ruleset.GetDataPointNodes();
List<string> dataPointMappingIds = ruleset.GetDataPointNodesMappingIds();
DataPortTreeNodeType? dataPointNode = ruleset.GetDataPointNodeByMappingId("my-data-point");

// Get properties from any node
List<string> propertyIds = node.GetPropertyIds();
```

**DataPortCommunicationExtensions:**
Reflection helpers for analyzing Communication types.

```csharp
// Get public property names (excluding base class properties)
List<string> properties = DataPortCommunicationExtensions
    .GetCommunicationPropertyNames<MyCommunication>();

// Get Communication ID from [Communication] attribute
string communicationId = DataPortCommunicationExtensions
    .GetCommunicationId<MyCommunication>();
```

## Dependencies

This package depends on:
- `AwesomeAssertions` - Fluent assertion library
- `ViciOne.TreeBuilder` - YAML ruleset and node type definitions
- `xunit.v3.extensibility.core` - xUnit testing framework
- `DataPort` and `DataPort.Extensions` - Core DataPort functionality

## Package Features

This package is published with:
- XML documentation for IntelliSense
- Source code embedded in symbols (.snupkg)
- Portable PDB debug symbols included in main package

## Best Practices

### Eventual Assertions

- Use the default 5-second timeout for most cases
- Only increase timeout for legitimately slow operations
- Prefer `Eventually()` over manual `Thread.Sleep()` or polling loops
- Use descriptive assertion messages to aid debugging

### YAML Consistency Testing

- Run these tests as part of your CI/CD pipeline
- Update tests whenever you modify YAML configuration or Communication classes
- Use `ExcludedCommunicationProperties` for computed or internal properties
- Keep YAML and C# in sync to catch configuration drift early
- Each test failure provides specific guidance on what's inconsistent

## Related Documentation

For more information about DataPort.Extensions and typed nodes, see:
- DataPort.Extensions documentation
- TypedNodes documentation
- Mapping documentation
