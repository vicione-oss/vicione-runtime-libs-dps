using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class PropertyEqualityComparer_Equals
{
    [Fact]
    public void Returns_true_when_properties_are_equal()
    {
        var person1 = new Person { Name = "John", Age = 30 };
        var person2 = new Person { Name = "John", Age = 30 };
        var comparer = new PropertyEqualityComparer<Person, string>(p => p.Name);

        var result = comparer.Equals(person1, person2);

        result.Should().BeTrue();
    }

    [Fact]
    public void Returns_false_when_properties_are_not_equal()
    {
        var person1 = new Person { Name = "John", Age = 30 };
        var person2 = new Person { Name = "Jane", Age = 30 };
        var comparer = new PropertyEqualityComparer<Person, string>(p => p.Name);

        var result = comparer.Equals(person1, person2);

        result.Should().BeFalse();
    }
}

public class PropertyEqualityComparer_GetHashCode
{
    [Fact]
    public void Returns_same_value_when_properties_are_equal()
    {
        var person1 = new Person { Name = "John", Age = 30 };
        var person2 = new Person { Name = "John", Age = 30 };
        var comparer = new PropertyEqualityComparer<Person, string>(p => p.Name);

        var hashCode1 = comparer.GetHashCode(person1);
        var hashCode2 = comparer.GetHashCode(person2);

        hashCode1.Should().Be(hashCode2);
    }

    [Fact]
    public void Returns_different_value_when_properties_are_not_equal()
    {
        var person1 = new Person { Name = "John", Age = 30 };
        var person2 = new Person { Name = "Jane", Age = 30 };
        var comparer = new PropertyEqualityComparer<Person, string>(p => p.Name);

        var hashCode1 = comparer.GetHashCode(person1);
        var hashCode2 = comparer.GetHashCode(person2);

        hashCode1.Should().NotBe(hashCode2);
    }
}
