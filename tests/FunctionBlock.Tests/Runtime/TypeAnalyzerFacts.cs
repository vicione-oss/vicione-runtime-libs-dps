using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.System.Runtime;

public sealed class TypeAnalyzer_AnalyzeType
{
    [Theory]
    [SuppressMessage("Usage", "xUnit1016:MemberData must reference a public member")]
    [MemberData(nameof(GetAcceptanceTestData))]
    internal void Acceptance(Type type, TypeInfo[] typeInfos)
    {
        using TypeAnalyzer analyzer = new(null);

        var result = analyzer.AnalyzeType(type);

        result.Should().BeEquivalentTo(typeInfos, o => o
            .WithoutStrictOrdering()
            .Excluding(m => m.Name)
            .Excluding(m => m.PropertyGetter)
            .Excluding(m => m.PropertySetter)
            .Excluding(m => m.Comment));
    }

    [Fact]
    public void Ignores_private_setter()
    {
        using TypeAnalyzer analyzer = new(null);

        var result = analyzer.AnalyzeType<PrivateSetterPropertyTest>();

        result.Should().ContainSingle().Which.PropertySetter.Should().BeNull();
    }

    [Fact]
    public void Ignores_private_getter()
    {
        using TypeAnalyzer analyzer = new(null);

        var result = analyzer.AnalyzeType<PrivateGetterPropertyTest>();

        result.Should().ContainSingle().Which.PropertyGetter.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(GetReturnValuesTestData))]
    [SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable",
        Justification = "Xunit Serialisierung könnte zu testende Serialisierung beeinflussen.")]
    public void Returns_value(object obj, Dictionary<string, object?> values)
    {
        using TypeAnalyzer analyzer = new(null);

        var result = analyzer.AnalyzeType(obj.GetType());

        var resultValues = result.ToDictionary(e => e.Path, e => e.PropertyGetter?.Invoke(obj));
        resultValues.Should().BeEquivalentTo(values, o => o.WithoutStrictOrdering());
    }

    [Theory]
    [MemberData(nameof(GetSetValuesTestData))]
    [SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable",
        Justification = "Xunit Serialisierung könnte zu testende Serialisierung beeinflussen.")]
    public void Sets_value(Type type, Dictionary<string, object?> values, object? expectedInstance)
    {
        using TypeAnalyzer analyzer = new(null);
        var instance = Activator.CreateInstance(type) ?? throw new InvalidOperationException();

        var result = analyzer.AnalyzeType(type).ToDictionary(e => e.Path);

        foreach (var (path, value) in values)
            result[path].PropertySetter!.Invoke(instance, value);

        instance.Should().BeEquivalentTo(expectedInstance);
    }

    internal static TheoryData<Type, TypeInfo[]> GetAcceptanceTestData()
        => new()
        {
            {
                typeof(int),
                new TypeInfo[] { new() { SimpleType = typeof(int), Path = string.Empty, }, }
            },
            {
                typeof(Test),
                new TypeInfo[]
                {
                    new() { SimpleType = typeof(int), Path = nameof(Test.SimpleProperty), IsNullable = false, },
                    new() { SimpleType = typeof(int?), Path = nameof(Test.NullableProperty), IsNullable = true, },
                    new() { SimpleType = typeof(TaskStatus), Path = nameof(Test.Enum), IsNullable = false, },
                    new() { SimpleType = typeof(List<int>), Path = nameof(Test.ListProperty), IsNullable = false, },
                }
            },
        };

    public static TheoryData<object, Dictionary<string, object?>> GetReturnValuesTestData()
        => new()
        {
            { 1, new Dictionary<string, object?> { { string.Empty, null } } },
            {
                new Test
                {
                    SimpleProperty = 1,
                    NullableProperty = null,
                    Enum = TaskStatus.Running,
                    ListProperty = { 23, },
                },
                new Dictionary<string, object?>
                {
                    { nameof(Test.SimpleProperty), 1 },
                    { nameof(Test.NullableProperty), null },
                    { nameof(Test.Enum), TaskStatus.Running },
                    { nameof(Test.ListProperty), new List<int> { 23, } },
                }
            },
        };

    public static TheoryData<Type, Dictionary<string, object?>, object?> GetSetValuesTestData()
        => new()
        {
            {
                typeof(Test),
                new Dictionary<string, object?>
                {
                    { nameof(Test.SimpleProperty), 23 },
                    { nameof(Test.NullableProperty), 13 },
                    { nameof(Test.Enum), TaskStatus.Running },
                    { nameof(Test.ListProperty), new List<int> { 23, } },
                },
                new Test
                {
                    SimpleProperty = 23,
                    NullableProperty = 13,
                    Enum = TaskStatus.Running,
                    ListProperty = { 23, },
                }
            },
        };

    internal sealed class Test
    {
        public int SimpleProperty { get; init; }
        public int? NullableProperty { get; init; }
        public TaskStatus Enum { get; init; }
        public List<int> ListProperty { get; init; } = [];
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used via reflection")]
    internal sealed class PrivateSetterPropertyTest
    {
        public int Property { get; private set; }
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used via reflection")]
    internal sealed class PrivateGetterPropertyTest
    {
        [SuppressMessage("Design", "CA1044:Eigenschaften dürfen nicht lesegeschützt sein")]
        public int Property { private get; set; }
    }
}

public sealed class TypeAnalyzer_GenerateComment
{
    [Fact]
    public void Can_handle_type_with_no_description()
    {
        var comment = TypeAnalyzer.GenerateComment(typeof(int));

        comment.Should().MatchEquivalentOf("*System.Int32*");
    }

    [Fact]
    public void Can_handle_type_with_description()
    {
        var comment = TypeAnalyzer.GenerateComment(typeof(Test));

        comment.Should().Be("My comment");
    }

    [Fact]
    public void Can_handle_enum()
    {
        var comment = TypeAnalyzer.GenerateComment(typeof(State));

        comment.Should().MatchEquivalentOf("0 = Start*1 = End");
    }

    [Fact]
    public void Can_handle_enum_with_description()
    {
        var comment = TypeAnalyzer.GenerateComment(typeof(StateWithDescription));

        comment.Should().MatchEquivalentOf("0 = it is the beginning*1 = it is the end");
    }

    [Description("My comment")]
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used via reflection")]
    internal sealed class Test { }

    internal enum State
    {
        Start,
        End
    }

    internal enum StateWithDescription
    {
        [Description("it is the beginning")]
        Start,
        [Description("it is the end")]
        End
    }
}
