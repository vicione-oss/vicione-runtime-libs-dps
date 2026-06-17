using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using NSubstitute;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using Xunit;

namespace ViciOne.Suite.System.Runtime;

public sealed class ComposeProcessor_
{
    [Fact]
    public void Acceptance()
    {
        var connector1 = Guid.NewGuid();
        var connector2 = Guid.NewGuid();
        Dictionary<Guid, Action<object, object?>> setters = new()
        {
            { connector1, (obj, v) => ((Test)obj!).ValueOne = (int)v! },
            { connector2, (obj, v) => ((Test)obj!).ValueTwo = (int)v! },
        };
        var link = Substitute.For<IRuntimeLink>();
        DateTime dateTimeNewer = new(2023, 3, 23);
        DateTime dateTimeOlder = new(2023, 3, 22);

        Test? value = null;
        DateTime? changedTime = null;
        Validity? validity = null;
        link.SetData(Arg.Do<object?>(_ => value = (Test)_!), Arg.Do<DateTime>(_ => changedTime = _), Arg.Do<Validity>(_ => validity = _));

        ComposeProcessor composer = new(typeof(Test), setters, link);

        composer.Add(connector1, dateTimeNewer, Validity.Valid, 100);
        composer.Add(connector2, dateTimeOlder, Validity.Invalid, 200);

        composer.Process();

        value!.ValueOne.Should().Be(100);
        value.ValueTwo.Should().Be(200);
        changedTime.Should().Be(dateTimeNewer);
        validity.Should().Be(Validity.Invalid);
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class Test
    {
        internal int ValueOne { get; set; }
        internal int ValueTwo { get; set; }
    }
}
