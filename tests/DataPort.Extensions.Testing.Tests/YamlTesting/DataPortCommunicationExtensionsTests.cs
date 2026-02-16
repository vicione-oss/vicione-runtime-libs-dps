#pragma warning disable CA1812 // Avoid uninstantiated internal classes - Used via generic type parameters
// ReSharper disable UnusedMember.Local
// ReSharper disable ClassNeverInstantiated.Local

using System;
using System.Linq;
using System.Reflection;
using ViciOne.ManagedEngine.Communication;
using ViciOne.Suite.DataPort.Extensions.Testing.YamlTesting;

namespace ViciOne.Suite.DataPort.Extensions.Testing.Tests.YamlTesting;

public class DataPortCommunicationExtensionsTests
{
    [Fact]
    public void GetCommunicationPropertyNames_ReturnsOnlyDerivedProperties()
    {
        // Arrange

        // Act
        var result = DataPortCommunicationExtensions.GetCommunicationPropertyNames<TestCommunication>();

        // Assert
        result.Should().Contain("CustomProperty1");
        result.Should().Contain("CustomProperty2");
        result.Should().NotContain("Id"); // Base property from DataPortCommunication
        result.Should().NotContain("Name"); // Base property from DataPortCommunication
    }

    [Fact]
    public void GetCommunicationPropertyNames_ReturnsEmptyList_WhenNoCustomProperties()
    {
        // Arrange

        // Act
        var result = DataPortCommunicationExtensions.GetCommunicationPropertyNames<MinimalTestCommunication>();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCommunicationPropertyNames_ExcludesInheritedProperties()
    {
        // Arrange
        var baseProperties = typeof(DataPortCommunication)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToList();

        // Act
        var result = DataPortCommunicationExtensions.GetCommunicationPropertyNames<TestCommunication>();

        // Assert
        foreach (var baseProp in baseProperties)
        {
            result.Should().NotContain(baseProp, $"base property '{baseProp}' should be excluded");
        }
    }

    [Fact]
    public void GetCommunicationId_ReturnsIdFromAttribute()
    {
        // Arrange

        // Act
        var result = DataPortCommunicationExtensions.GetCommunicationId<TestCommunication>();

        // Assert
        result.Should().Be("test-communication-id");
    }

    [Fact]
    public void GetCommunicationId_ThrowsException_WhenAttributeIsMissing()
    {
        // Arrange

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            DataPortCommunicationExtensions.GetCommunicationId<CommunicationWithoutAttribute>
        );

        // Assert
        exception.Message.Should().Contain("CommunicationWithoutAttribute");
        exception.Message.Should().Contain("does not have a CommunicationAttribute");
    }

    [Fact]
    public void GetCommunicationId_WorksWithDifferentIds()
    {
        // Arrange

        // Act
        var result = DataPortCommunicationExtensions.GetCommunicationId<AnotherTestCommunication>();

        // Assert
        result.Should().Be("another-communication-id");
    }

    [Communication("test-communication-id")]
    private record TestCommunication : DataPortCommunication
    {
        public string? CustomProperty1 { get; set; }
        public int CustomProperty2 { get; set; }
    }

    [Communication("minimal-communication-id")]
    private record MinimalTestCommunication : DataPortCommunication
    {
        // No custom properties
    }

    [Communication("another-communication-id")]
    private record AnotherTestCommunication : DataPortCommunication
    {
        public bool AnotherProperty { get; set; }
    }

    private record CommunicationWithoutAttribute : DataPortCommunication
    {
        // No [Communication] attribute
        public string? SomeProperty { get; set; }
    }
}
