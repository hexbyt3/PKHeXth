using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace PKHeX.Core.Tests.Legality;

/// <summary>
/// Fork: versions with no game data (e.g. <see cref="GameVersion.WI"/>/<see cref="GameVersion.WA"/>) are valid saved
/// version IDs, so they land in <see cref="GameUtil.GetVersionsWithinRange"/>; every generator entry point must skip them,
/// while <see cref="GameVersion.GO"/> (context Gen7b, re-searched as Gen8) is still searched.
/// </summary>
public class EncounterGeneratorVersionTests
{
    private static PK9 GetTemplate() => new() { Species = (ushort)Species.Pikachu, Gender = 0 };

    [Fact]
    public void TrainerOverloadWithDefaultVersionsDoesNotThrow()
    {
        var pk = GetTemplate();
        var tr = new SAV9SV();
        var act = () => EncounterMovesetGenerator.GenerateEncounters(pk, tr, ReadOnlyMemory<ushort>.Empty).ToList();
        act.Should().NotThrow().Which.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(GameVersion.WI)]
    [InlineData(GameVersion.WA)]
    public void UnsupportedVersionYieldsNothing(GameVersion version)
    {
        var pk = GetTemplate();
        EncounterMovesetGenerator.GenerateEncounters(pk, ReadOnlyMemory<ushort>.Empty, version).Should().BeEmpty();
        EncounterMovesetGenerator.GenerateEncounters(pk, new SAV9SV(), ReadOnlyMemory<ushort>.Empty, version).Should().BeEmpty();
    }

    [Fact]
    public void GoIsStillSearched()
    {
        var pk = GetTemplate();
        ReadOnlyMemory<GameVersion> versions = new[] { GameVersion.WI, GameVersion.GO };
        EncounterMovesetGenerator.GenerateEncounters(pk, ReadOnlyMemory<ushort>.Empty, versions)
            .Should().Contain(e => e is EncounterSlot8GO);
    }
}
