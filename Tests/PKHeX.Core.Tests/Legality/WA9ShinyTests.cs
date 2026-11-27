using System.Linq;
using FluentAssertions;
using Xunit;

namespace PKHeX.Core.Tests.Legality;

public class WA9ShinyTests
{
    [Fact]
    public void HomeVolcanionIsShiny()
    {
        var card = EncounterEvent.MGDB_G9A.Single(g => g.CardID == 9034);
        card.Species.Should().Be((ushort)Species.Volcanion);
        card.IsShiny.Should().BeTrue();
        card.ConvertToPKM(new SimpleTrainerInfo(GameVersion.ZA)).IsShiny.Should().BeTrue();
    }

    [Fact]
    public void IsShinyMatchesWhatEveryCardMakes()
    {
        foreach (var card in EncounterEvent.MGDB_G9A.Where(g => g.IsEntity))
        {
            foreach (var (tid, sid) in new (ushort, ushort)[] { (12345, 54321), (777, 4242) })
            {
                var pk = card.ConvertToPKM(new SimpleTrainerInfo(GameVersion.ZA) { TID16 = tid, SID16 = sid });
                pk.IsShiny.Should().Be(card.IsShiny, $"card {card.CardID} {(Species)card.Species}");
            }
        }
    }
}
