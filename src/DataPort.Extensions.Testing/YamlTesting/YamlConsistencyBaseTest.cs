#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;
using ViciOne.TreeBuilder.NodeTypes;
using ViciOne.TreeBuilder.Rules;
using ViciOne.TreeBuilder.Rules.Yaml;
using ViciOne.TreeBuilder.Validation;
using Xunit;

namespace ViciOne.Suite.DataPort.Extensions.Testing.YamlTesting;

/// <summary>
/// Base class for YAML configuration consistency tests.
/// Provides common test infrastructure and abstract properties for configuration.
/// </summary>
[SuppressMessage("Usage", "xUnit1044:Avoid using TheoryData type arguments that are not serializable")]
[SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable")]
[SuppressMessage("Design", "CA1034:Nested types should not be visible")]
public abstract class YamlConsistencyBaseTest<TCommunicationType, TYamlConfig, TRootConfigurationNode>
    where TCommunicationType : DataPortCommunication
    where TYamlConfig : IYamlTestingConfig<TCommunicationType, TRootConfigurationNode>
    where TRootConfigurationNode : IRootConfigurationNode<TCommunicationType>
{
    // Load the ruleset only once per subclass.
    // ReSharper disable once StaticMemberInGenericType
    private static readonly Lazy<Ruleset> s_lazyRuleset =
        new(() => RulesDeserializer.Deserialize(TYamlConfig.YamlFileName));

    [Fact]
    public void IsYamlFileValid()
    {
        // Arrange
        var ruleset = s_lazyRuleset.Value;

        // Act
        var result = ValidationManager.Validate(ruleset);

        // Assert
        using var _ = new AssertionScope();

        result.Messages.Should().BeEmpty();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CommunicationAttributeMatchesYamlRootId()
    {
        // Arrange
        var expectedId = s_lazyRuleset.Value.GetRootId();

        // Act
        var actualId = DataPortCommunicationExtensions.GetCommunicationId<TCommunicationType>();

        // Assert
        actualId.Should().Be(expectedId,
            "The [Communication] attribute ID must match YAML Root.Id");
    }


    /// <summary>
    /// Dynamically generates test cases for each device node in YAML.
    /// </summary>
    public static TheoryData<DataPortNodeType> GetDataPortNodeTestCases() =>
        [.. s_lazyRuleset.Value.GetRootNodes()];

    [Theory]
    [MemberData(nameof(GetDataPortNodeTestCases))]
    public void YamlDataPortNodePropertiesExistInCommunicationClass(DataPortNodeType dataPortNode)
    {
        // Arrange
        var csharpProperties = DataPortCommunicationExtensions.GetCommunicationPropertyNames<TCommunicationType>();

        // Act
        var yamlProperties = dataPortNode.GetPropertyIds();

        // Assert
        using var scope = new AssertionScope();
        foreach (var yamlProp in yamlProperties)
        {
            csharpProperties.Should().Contain(yamlProp,
                $"Yaml device '{dataPortNode}' contains property '{yamlProp}' which should exist in '{typeof(TCommunicationType).Name}'.");
        }
    }

    public static TheoryData<string> GetCommunicationPropertyTestCases() =>
    [
        .. DataPortCommunicationExtensions.GetCommunicationPropertyNames<TCommunicationType>()
            .Except(TYamlConfig.ExcludedCommunicationProperties)
    ];

    [Theory]
    [MemberData(nameof(GetCommunicationPropertyTestCases))]
    public void CommunicationPropertyExistsInYamlDeviceNodes(string propertyName)
    {
        // Arrange
        var ruleset = s_lazyRuleset.Value;

        // Act
        var allRootProperties = ruleset.GetRootNodes()
            .SelectMany(node => node.GetPropertyIds())
            .ToList();

        // Assert
        allRootProperties.Should().Contain(propertyName,
            $"'{typeof(TCommunicationType).Name}' contains property '{propertyName}' which should exist in yaml device nodes.");
    }


    public static TheoryData<IBranchConfigurationNodeMapper<IBranchConfigurationNode>>
        GetConfigurationMapperTestCases() =>
    [
        .. TYamlConfig.Mapper.ConfigurationNodeMappers
            .Select(static m => new TheoryDataRow<IBranchConfigurationNodeMapper<IBranchConfigurationNode>>(m))
    ];

    public static TheoryData<string> GetConfigurationNodeMapperTestCases() =>
    [
        .. s_lazyRuleset.Value.GetConfigurationNodesMappingIds()
    ];

    [Theory]
    [MemberData(nameof(GetConfigurationMapperTestCases))]
    public void ConfigurationNodeMapperHasCorrespondingYamlConfigurationNode(
        IBranchConfigurationNodeMapper<IBranchConfigurationNode> mapper)
    {
        // Arrange
        var ruleset = s_lazyRuleset.Value;

        // Act
        var configurationNodes = ruleset.GetConfigurationNodesMappingIds();

        // Assert
        configurationNodes.Should().Contain(mapper.TargetLinkedNodeTypeId,
            $"'{mapper.GetType().Name}' is targeting '{mapper.TargetLinkedNodeTypeId}' in the yaml but this configuration node does not exist.");
    }

    [Theory]
    [MemberData(nameof(GetConfigurationNodeMapperTestCases))]
    public void YamlConfigurationNodeHasCorrespondingConfigurationNodeMapper(string nodeId)
    {
        // Arrange
        var mappers = TYamlConfig.Mapper.ConfigurationNodeMappers;

        // Act
        var configurationMapperIds = mappers.Select(mapper => mapper.TargetLinkedNodeTypeId);

        // Assert
        configurationMapperIds.Should().Contain(nodeId,
            $"Yaml configuration node '{nodeId}' exists but no mapper is registered for it.");
    }

    /// <summary>
    /// Test case for validating a configuration mapper's property exists in the YAML node.
    /// </summary>
    /// <param name="Mapper">The configuration node mapper.</param>
    /// <param name="PropertyName">The property name defined in the mapper.</param>
    public record struct ConfigurationMapperPropertyTestCase(
        IBranchConfigurationNodeMapper<IBranchConfigurationNode> Mapper,
        string PropertyName);

    public static TheoryData<ConfigurationMapperPropertyTestCase> GetConfigurationMapperPropertyTestCases()
    {
        var excluded = TYamlConfig.ExcludedConfigurationNodeProperties;
        var testCases = TYamlConfig.Mapper.ConfigurationNodeMappers
            .SelectMany(mapper => mapper.PropertyNamesDefinedInTargetNode()
                    .Where(p => !excluded.TryGetValue(mapper.NodeType, out var props) || !props.Contains(p)),
                (mapper, propertyName) => new ConfigurationMapperPropertyTestCase(mapper, propertyName));
        return [.. testCases];
    }

    [Theory]
    [MemberData(nameof(GetConfigurationMapperPropertyTestCases))]
    public void ConfigurationMapperPropertyExistsInYamlNode(ConfigurationMapperPropertyTestCase testCase)
    {
        // Arrange
        var mapper = testCase.Mapper;
        var propertyName = testCase.PropertyName;
        var ruleset = s_lazyRuleset.Value;

        // Act
        var yamlNode = ruleset.GetConfigurationNodeByMappingId(mapper.TargetLinkedNodeTypeId);
        var yamlPropertyIds = yamlNode?.GetPropertyIds();

        // Assert
        yamlNode.Should()
            .NotBeNull(
                $"IBranchConfigurationNodeMapper '{mapper.GetType().Name}' has no matching yaml-configuration-node for targeted id '{mapper.TargetLinkedNodeTypeId}'");
        yamlPropertyIds.Should().Contain(propertyName,
            $"'{mapper.GetType().Name}' is targeting '{mapper.TargetLinkedNodeTypeId}' in the yaml and defines property '{propertyName}' which should exist in the yaml node.");
    }

    /// <summary>
    /// Test case representing a YAML node and one of its properties for theory-based validation.
    /// </summary>
    /// <param name="MappingId">The unique mapping identifier of the YAML node.</param>
    /// <param name="PropertyId">The property identifier within the node.</param>
    public record struct YamlNodePropertyTestCase(string MappingId, string PropertyId);

    public static TheoryData<YamlNodePropertyTestCase> GetYamlConfigurationNodePropertyTestCases()
    {
        var ruleset = s_lazyRuleset.Value;
        var testCases = ruleset.GetConfigurationNodes()
            .SelectMany(node => node.GetPropertyIds(),
                (node, propertyId) => new YamlNodePropertyTestCase(node.MappingId, propertyId));
        return [.. testCases];
    }

    [Theory]
    [MemberData(nameof(GetYamlConfigurationNodePropertyTestCases))]
    public void YamlConfigurationNodePropertyExistsInConfigurationMapper(YamlNodePropertyTestCase testCase)
    {
        // Arrange
        var mappingId = testCase.MappingId;
        var propertyId = testCase.PropertyId;

        // Act
        var configMapper = TYamlConfig.Mapper.ConfigurationNodeMappers.FirstOrDefault(m =>
            m.TargetLinkedNodeTypeId == mappingId);
        var existingProperties = configMapper?.PropertyNamesDefinedInTargetNode();

        // Assert
        configMapper.Should().NotBeNull($"No mapper found for YAML node '{mappingId}'");
        existingProperties.Should().Contain(propertyId,
            $"'{configMapper.GetType().Name}' is targeting '{mappingId}' in the yaml which contains property '{propertyId}'.");
    }

    public static TheoryData<IDataPointNodeMapper<IDataPointNode>> GetDataPointMapperTestCases() =>
    [
        .. TYamlConfig.Mapper.DataPointNodeMappers
            .Select(static m => new TheoryDataRow<IDataPointNodeMapper<IDataPointNode>>(m))
    ];

    public static TheoryData<string> GetDataPointNodeMapperTestCases() =>
    [
        .. s_lazyRuleset.Value.GetDataPointNodesMappingIds()
    ];

    [Theory]
    [MemberData(nameof(GetDataPointMapperTestCases))]
    public void DataPointNodeMapperHasCorrespondingYamlDataPointNode(IDataPointNodeMapper<IDataPointNode> mapper)
    {
        // Arrange
        var ruleset = s_lazyRuleset.Value;

        // Act
        var dataPointNodes = ruleset.GetDataPointNodesMappingIds();

        // Assert
        dataPointNodes.Should().Contain(mapper.TargetLinkedNodeTypeId,
            $"'{mapper.GetType().Name}' is targeting '{mapper.TargetLinkedNodeTypeId}' in the yaml but this data point node does not exist.");
    }

    [Theory]
    [MemberData(nameof(GetDataPointNodeMapperTestCases))]
    public void YamlDataPointNodeHasCorrespondingDataPointNodeMapper(string nodeId)
    {
        // Arrange
        var mappers = TYamlConfig.Mapper.DataPointNodeMappers;

        // Act
        var dataPointMapperIds = mappers.Select(mapper => mapper.TargetLinkedNodeTypeId);

        // Assert
        dataPointMapperIds.Should().Contain(nodeId,
            $"Yaml data point node '{nodeId}' exists but no mapper is registered for it.");
    }

    /// <summary>
    /// Test case for validating a data point mapper's property exists in the YAML node.
    /// </summary>
    /// <param name="Mapper">The data point node mapper.</param>
    /// <param name="PropertyName">The property name defined in the mapper.</param>
    public record struct DataPointMapperPropertyTestCase(
        IDataPointNodeMapper<IDataPointNode> Mapper,
        string PropertyName);

    public static TheoryData<DataPointMapperPropertyTestCase> GetDataPointMapperPropertyTestCases()
    {
        var excluded = TYamlConfig.ExcludedDataPointNodeProperties;
        var testCases = TYamlConfig.Mapper.DataPointNodeMappers
            .SelectMany(mapper => mapper.PropertyNamesDefinedInTargetNode()
                    .Where(p => !excluded.TryGetValue(mapper.NodeType, out var props) || !props.Contains(p)),
                (mapper, propertyName) => new DataPointMapperPropertyTestCase(mapper, propertyName));
        return [.. testCases];
    }

    [Theory]
    [MemberData(nameof(GetDataPointMapperPropertyTestCases))]
    public void DataPointMapperPropertyExistsInYamlNode(DataPointMapperPropertyTestCase testCase)
    {
        // Arrange
        var mapper = testCase.Mapper;
        var propertyName = testCase.PropertyName;
        var ruleset = s_lazyRuleset.Value;

        // Act
        var yamlNode = ruleset.GetDataPointNodeByMappingId(mapper.TargetLinkedNodeTypeId);
        var propertyIds = yamlNode?.GetPropertyIds();

        // Assert
        yamlNode.Should().NotBeNull();
        propertyIds.Should().Contain(propertyName,
            $"'{mapper.GetType().Name}' is targeting '{mapper.TargetLinkedNodeTypeId}' in the yaml and defines property '{propertyName}' which should exist in the yaml node.");
    }

    public static TheoryData<YamlNodePropertyTestCase> YamlDataPointNodePropertyTestCases()
    {
        var ruleset = s_lazyRuleset.Value;
        var mapperMappingIds = TYamlConfig.Mapper.DataPointNodeMappers
            .Select(m => m.TargetLinkedNodeTypeId)
            .ToHashSet();

        var testCases = ruleset.GetDataPointNodes()
            .Where(node => mapperMappingIds.Contains(node.MappingId))
            .SelectMany(node => node.GetPropertyIds(),
                (node, propertyId) => new YamlNodePropertyTestCase(node.MappingId, propertyId));

        return [.. testCases];
    }

    [Theory]
    [MemberData(nameof(YamlDataPointNodePropertyTestCases))]
    public void YamlDataPointNodePropertyExistsInDataPointMapper(YamlNodePropertyTestCase testCase)
    {
        // Arrange
        var mappingId = testCase.MappingId;
        var propertyId = testCase.PropertyId;

        // Act
        var mapper = TYamlConfig.Mapper.DataPointNodeMappers.FirstOrDefault(m => m.TargetLinkedNodeTypeId == mappingId);
        var mapperProperties = mapper?.PropertyNamesDefinedInTargetNode();

        // Assert
        mapper.Should().NotBeNull($"No mapper found for YAML node '{mappingId}'");
        mapperProperties.Should().Contain(propertyId,
            $"'{mapper.GetType().Name}' is targeting '{mappingId}' in the yaml which contains property '{propertyId}'.");
    }
}
