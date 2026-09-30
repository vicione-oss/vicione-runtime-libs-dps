#pragma warning disable CA1515 // Test class must be public for xUnit discovery (test methods are inherited)

using System;
using System.Collections.Generic;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.DataPoints;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Device;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Folders;
using ViciOne.Suite.DataPort.Extension.Tests.Stubs.Model.Nodes.Mapper;
using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping;
using ViciOne.Suite.DataPort.Extensions.Testing.YamlTesting;

namespace ViciOne.Suite.DataPort.Extension.Tests.YamlConsistency;

public class YamlConsistencyTest :
    YamlConsistencyBaseTest<TestCommunication, YamlConsistencyTest, TestDeviceNode>,
    IYamlTestingConfig<TestCommunication, TestDeviceNode>
{
    public static string YamlFileName => "TestDataPort.yaml";

    public static TypedNodeMapper<TestCommunication, TestDeviceNode> Mapper { get; } = TestNodeMapper.Instance();

    public static HashSet<string> ExcludedCommunicationProperties
        => [nameof(TestCommunication.ConnectionString)];

    public static Dictionary<Type, HashSet<string>> ExcludedConfigurationNodeProperties => new()
    {
        [typeof(IHasFullPath)] = [nameof(IHasFullPath.FullPath)],
    };

    public static Dictionary<Type, HashSet<string>> ExcludedDataPointNodeProperties => new()
    {
        [typeof(DataPointNodeBase)] = [nameof(DataPointNodeBase.Description)],
    };
}
