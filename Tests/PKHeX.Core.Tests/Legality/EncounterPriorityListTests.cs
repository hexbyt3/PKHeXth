using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace PKHeX.Core.Tests.Legality;

[CollectionDefinition(nameof(EncounterPriorityListTests), DisableParallelization = true)]
public sealed class EncounterPriorityListCollection;

/// <summary>
/// Fork: Auto-Legality persists <see cref="EncounterTypeGroup.None"/> in its encounter priority list;
/// the generator must skip it instead of throwing.
/// </summary>
[Collection(nameof(EncounterPriorityListTests))]
public class EncounterPriorityListTests
{
    [Fact]
    public void NoneInPriorityListIsSkipped()
    {
        var pk = new PK9 { Species = (ushort)Species.Pikachu, Gender = 0 };
        try
        {
            EncounterMovesetGenerator.PriorityList = [EncounterTypeGroup.Slot, EncounterTypeGroup.None];
            var act = () => EncounterMovesetGenerator.GenerateEncounters(pk, new SAV9SV(), ReadOnlyMemory<ushort>.Empty).ToList();
            act.Should().NotThrow().Which.Should().NotBeEmpty();
        }
        finally
        {
            EncounterMovesetGenerator.ResetFilters();
        }
    }
}
