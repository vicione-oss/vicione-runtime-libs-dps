using ViciOne.Suite.DataPort.Extensions.Model.TypedNodes.Mapping.Properties;

#pragma warning disable CA1515 // Make types internal - test class must be public for xunit test discovery

namespace ViciOne.Suite.DataPort.Extension.Tests.TypedNodes.Mapping;

public class PropertyValueConverterTests
{
    /// <summary>
    /// Custom enum for testing PropertyValueConverter without depending on external types.
    /// </summary>
    public enum TestAreaType
    {
        DB = 0,
        I = 1,
        Q = 2,
        M = 3,
        C = 4,
        T = 5
    }

    [Theory]
    [InlineData((byte)0, TestAreaType.DB)]
    [InlineData((byte)1, TestAreaType.I)]
    [InlineData((byte)2, TestAreaType.Q)]
    [InlineData((byte)3, TestAreaType.M)]
    [InlineData((byte)4, TestAreaType.C)]
    [InlineData((byte)5, TestAreaType.T)]
    public void ToEnum_WithValidByteValue_ReturnsCorrectEnumValue(byte byteValue, TestAreaType expected)
    {
        // Act
        var result = PropertyValueConverter.ToEnum<TestAreaType>(byteValue, "TestProperty");

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("DB", TestAreaType.DB)]
    [InlineData("I", TestAreaType.I)]
    [InlineData("Q", TestAreaType.Q)]
    [InlineData("M", TestAreaType.M)]
    [InlineData("C", TestAreaType.C)]
    [InlineData("T", TestAreaType.T)]
    public void ToEnum_WithValidStringValue_ReturnsCorrectEnumValue(string stringValue, TestAreaType expected)
    {
        // Act
        var result = PropertyValueConverter.ToEnum<TestAreaType>(stringValue, "TestProperty");

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("db", TestAreaType.DB)]
    [InlineData("i", TestAreaType.I)]
    [InlineData("q", TestAreaType.Q)]
    [InlineData("m", TestAreaType.M)]
    [InlineData("c", TestAreaType.C)]
    [InlineData("t", TestAreaType.T)]
    public void ToEnum_WithValidStringValueLowerCase_ReturnsCorrectEnumValue(string stringValue, TestAreaType expected)
    {
        // Act
        var result = PropertyValueConverter.ToEnum<TestAreaType>(stringValue, "TestProperty");

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData((byte)8)]
    [InlineData((byte)255)]
    [InlineData((byte)100)]
    public void ToEnum_WithInvalidByteValue_ThrowsInvalidPropertyException(byte invalidValue)
    {
        // Act
        var act = () => PropertyValueConverter.ToEnum<TestAreaType>(invalidValue, "TestProperty");

        // Assert
        act.Should().Throw<InvalidPropertyException>()
            .WithMessage("*TestProperty*invalid format*");
    }

    [Theory]
    [InlineData("InvalidValue")]
    [InlineData("X")]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("DataBlock")]
    public void ToEnum_WithInvalidStringValue_ThrowsInvalidPropertyException(string invalidValue)
    {
        // Act
        var act = () => PropertyValueConverter.ToEnum<TestAreaType>(invalidValue, "TestProperty");

        // Assert
        act.Should().Throw<InvalidPropertyException>()
            .WithMessage("*TestProperty*invalid format*");
    }

    [Fact]
    public void ToEnum_WithByteValue_ShouldIncludePropertyNameInException()
    {
        // Arrange
        byte invalidValue = 99;
        var propertyName = "AreaTypeProperty";

        // Act
        var act = () => PropertyValueConverter.ToEnum<TestAreaType>(invalidValue, propertyName);

        // Assert
        act.Should().Throw<InvalidPropertyException>()
            .Which.PropertyName.Should().Be(propertyName);
    }

    [Fact]
    public void ToEnum_WithStringValue_ShouldIncludePropertyNameInException()
    {
        // Arrange
        var invalidValue = "InvalidAreaType";
        var propertyName = "AreaTypeProperty";

        // Act
        var act = () => PropertyValueConverter.ToEnum<TestAreaType>(invalidValue, propertyName);

        // Assert
        act.Should().Throw<InvalidPropertyException>()
            .Which.PropertyName.Should().Be(propertyName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ToEnum_WithIntValue_ConvertsToString_ReturnsCorrectEnumValue(int intValue)
    {
        // Arrange
        var expected = (TestAreaType)intValue;

        // Act
        var result = PropertyValueConverter.ToEnum<TestAreaType>(intValue, "TestProperty");

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, TestAreaType.DB)]
    [InlineData(1, TestAreaType.I)]
    [InlineData(2, TestAreaType.Q)]
    [InlineData(3, TestAreaType.M)]
    [InlineData(4, TestAreaType.C)]
    [InlineData(5, TestAreaType.T)]
    public void ToEnum_WithValidIntValue_ReturnsCorrectEnumValue(int intValue, TestAreaType expected)
    {
        // Act
        var result = PropertyValueConverter.ToEnum<TestAreaType>(intValue, "TestProperty");

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(255)]
    [InlineData(100)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void ToEnum_WithInvalidIntValue_ThrowsInvalidPropertyException(int invalidValue)
    {
        // Act
        var act = () => PropertyValueConverter.ToEnum<TestAreaType>(invalidValue, "TestProperty");

        // Assert
        act.Should().Throw<InvalidPropertyException>()
            .WithMessage("*TestProperty*invalid format*");
    }

    [Fact]
    public void ToEnum_WithIntValue_ShouldIncludePropertyNameInException()
    {
        // Arrange
        var invalidValue = 99;
        var propertyName = "AreaTypeProperty";

        // Act
        var act = () => PropertyValueConverter.ToEnum<TestAreaType>(invalidValue, propertyName);

        // Assert
        act.Should().Throw<InvalidPropertyException>()
            .Which.PropertyName.Should().Be(propertyName);
    }

    [Fact]
    public void ToEnum_WithIntZero_ReturnsFirstEnumValue()
    {
        // Act
        var result = PropertyValueConverter.ToEnum<TestAreaType>(0, "TestProperty");

        // Assert
        result.Should().Be(TestAreaType.DB);
    }
}
