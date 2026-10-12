using FluentAssertions;
using Xunit;
namespace PKHeX.Core.Tests;

public class HomeFRLGTests
{
    private static PK3 GetFireRed(ushort species)
    {
        var pk = new PK3 { Species = species, Version = GameVersion.FR, Language = 2, Ball = (byte)Ball.Poke, OriginalTrainerGender = 1 };
        pk.EXP = Experience.GetEXP(20, pk.PersonalInfo.EXPGrowth);
        return pk;
    }

    [Fact]
    public void FireRedKeepsLevelGenderAndMetLocation()
    {
        var home = PKH.ConvertFromPKM(GetFireRed((ushort)Species.Bulbasaur));
        home.DataPK8.Should().NotBeNull();

        var pk9 = home.ConvertToPK9();
        pk9.Should().NotBeNull();
        pk9!.CurrentLevel.Should().Be(20);
        pk9.OriginalTrainerGender.Should().Be(1);
        pk9.MetLocation.Should().Be(LocationsHOME.VirtualConsole3);
    }

    [Fact]
    public void UnconvertibleEntityGetsNoSideData()
    {
        var home = PKH.ConvertFromPKM(GetFireRed(0));
        home.DataPK8.Should().BeNull();
    }
}
