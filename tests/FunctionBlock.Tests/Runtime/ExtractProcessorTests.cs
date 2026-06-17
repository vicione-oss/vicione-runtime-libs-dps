using System;
using AwesomeAssertions;
using NSubstitute;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using Xunit;

namespace ViciOne.Suite.System.Runtime;

public sealed class ExtractProcessor_
{
    [Fact]
    public void Acceptance()
    {
        var link = Substitute.For<IRuntimeLink>();
        DateTime dateTime = new(2023, 3, 23);
        int? value = null;
        DateTime? changedTime = null;
        Validity? validity = null;

        link.SetData(Arg.Do<object?>(_ => value = (int)_!), Arg.Do<DateTime>(_ => changedTime = _), Arg.Do<Validity>(_ => validity = _));

        ExtractProcessor extractor = new(obj => ((Test)obj).ValueOne, link);

        extractor.ProcessValue(dateTime, Validity.Valid, new Test { ValueOne = 100, });

        value.Should().Be(100);
        changedTime.Should().Be(dateTime);
        validity.Should().Be(Validity.Valid);
    }

    [Fact]
    public void Can_handle_null()
    {
        var link = Substitute.For<IRuntimeLink>();
        DateTime dateTime = new(2023, 3, 23);
        int? value = null;
        DateTime? changedTime = null;
        Validity? validity = null;

        link.SetData(Arg.Do<object?>(_ => value = (int?)_), Arg.Do<DateTime>(_ => changedTime = _), Arg.Do<Validity>(_ => validity = _));

        ExtractProcessor extractor = new(obj => ((Test)obj).ValueOne, link);

        extractor.ProcessValue(dateTime, Validity.Valid, null);

        value.Should().BeNull();
        changedTime.Should().Be(dateTime);
        validity.Should().Be(Validity.Valid);
    }

    internal sealed class Test
    {
        internal int ValueOne { get; set; }
    }
}
