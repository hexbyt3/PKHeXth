using System;
using FluentAssertions;
using Xunit;

namespace PKHeX.Core.Tests.PKM;

public class MetDateTests
{
    [Fact]
    public void MetDateNullWhenDateComponentsAreAllZero()
    {
        var pk = new PK7
        {
            MetDay = 0,
            MetMonth = 0,
            MetYear = 0,
        };

        pk.MetDate.HasValue.Should().BeFalse();
    }

    [Fact]
    public void MetDateReturnsCorrectDate()
    {
        var pk = new PK7
        {
            MetDay = 10,
            MetMonth = 8,
            MetYear = 16,
        };

        pk.MetDate.GetValueOrDefault().Should().Be(new DateOnly(2016, 8, 10));
    }

    [Fact]
    public void MetDateCalculatesYear0Correctly()
    {
        var pk = new PK7
        {
            MetDay = 1,
            MetMonth = 1,
            MetYear = 0,
        };

        pk.MetDate.GetValueOrDefault().Year.Should().Be(2000);
    }

    [Fact]
    public void SettingToNullZerosComponents()
    {
        var pk = new PK7
        {
            MetDay = 12,
            MetMonth = 12,
            MetYear = 12,
        };

        pk.MetDay.Should().Be(12);
        pk.MetMonth.Should().Be(12);
        pk.MetYear.Should().Be(12);

        pk.MetDate = null;

        pk.MetDay.Should().Be(0);
        pk.MetMonth.Should().Be(0);
        pk.MetYear.Should().Be(0);
    }

    [Fact]
    public void SettingMetDateSetsComponents()
    {
        var pk = new PK7
        {
            MetDay = 12,
            MetMonth = 12,
            MetYear = 12,
        };

        pk.MetDay.Should().Be(12);
        pk.MetMonth.Should().Be(12);
        pk.MetYear.Should().Be(12);

        pk.MetDate = new DateOnly(2005, 5, 5);

        pk.MetDay.Should().Be(5);
        pk.MetMonth.Should().Be(5);
        pk.MetYear.Should().Be(5);
    }
}

public class EggMetDateTests
{
    [Fact]
    public void EggMetDateNullWhenDateComponentsAreAllZero()
    {
        var pk = new PK7
        {
            EggDay = 0,
            EggMonth = 0,
            EggYear = 0,
        };

        pk.EggMetDate.HasValue.Should().BeFalse();
    }

    [Fact]
    public void EggMetDateReturnsCorrectDate()
    {
        var pk = new PK7
        {
            EggDay = 10,
            EggMonth = 8,
            EggYear = 16,
        };

        pk.EggMetDate.GetValueOrDefault().Should().Be(new DateOnly(2016, 8, 10));
    }

    [Fact]
    public void EggMetDateCalculatesYear0Correctly()
    {
        var pk = new PK7
        {
            EggDay = 1,
            EggMonth = 1,
            EggYear = 0,
        };

        pk.EggMetDate.GetValueOrDefault().Year.Should().Be(2000);
    }

    [Fact]
    public void SettingEggMetDateToNullZerosComponents()
    {
        var pk = new PK7
        {
            EggDay = 12,
            EggMonth = 12,
            EggYear = 12,
        };

        pk.EggDay.Should().Be(12);
        pk.EggMonth.Should().Be(12);
        pk.EggYear.Should().Be(12);

        pk.EggMetDate = null;

        pk.EggDay.Should().Be(0);
        pk.EggMonth.Should().Be(0);
        pk.EggYear.Should().Be(0);
    }

    [Fact]
    public void SettingEggMetDateSetsComponents()
    {
        var pk = new PK7
        {
            EggDay = 12,
            EggMonth = 12,
            EggYear = 12,
        };

        pk.EggDay.Should().Be(12);
        pk.EggMonth.Should().Be(12);
        pk.EggYear.Should().Be(12);

        pk.EggMetDate = new DateOnly(2005, 5, 5);

        pk.EggDay.Should().Be(5);
        pk.EggMonth.Should().Be(5);
        pk.EggYear.Should().Be(5);
    }
}

public class SetShinyTests
{
    [Fact]
    public void PastGenOriginStaysShinyUnderTheOldRule()
    {
        // A Gen 5 origin in a Gen 6+ format: xor 8-15 counts as shiny now but would have
        // had its PID top bit flipped on transfer, so EC = PID must come with xor < 8.
        var pk5 = new PK5 { Species = (int)Species.Pikachu, Version = GameVersion.B, TID16 = 3232, SID16 = 0, CurrentLevel = 50 };
        var pk6 = pk5.ConvertToPK6();
        for (int i = 0; i < 500; i++)
        {
            pk6.SetShiny();
            pk6.ShinyXor.Should().BeLessThan(8);
            pk6.EncryptionConstant.Should().Be(pk6.PID);
        }
    }

    [Fact]
    public void ModernOriginStillShiny()
    {
        var pk6 = new PK6 { Species = (int)Species.Pikachu, Version = GameVersion.X, TID16 = 3232, SID16 = 0, CurrentLevel = 50 };
        for (int i = 0; i < 100; i++)
        {
            pk6.SetShiny();
            pk6.IsShiny.Should().BeTrue();
        }
    }
}
