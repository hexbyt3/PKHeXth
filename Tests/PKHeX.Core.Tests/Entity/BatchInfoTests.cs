using FluentAssertions;
using Xunit;
namespace PKHeX.Core.Tests;

public class BatchInfoTests
{
    [Fact]
    public void LegalitySeesEditsMadeBeforeItIsFirstUsed()
    {
        // Bot sets list ".CurrentLevel=50" before "$suggest" instructions; the suggestions
        // must analyze the edited entity, not the one the batch started with.
        var pk = new PK9 { Species = (ushort)Species.Pikachu, CurrentLevel = 100, Version = GameVersion.SL };
        var info = new BatchInfo(pk);
        pk.CurrentLevel = 50;

        info.Legality.Info.EvoChainsAllGens.Gen9[0].LevelMax.Should().Be(50);
    }
}
