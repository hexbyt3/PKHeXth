using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace PKHeX.Core.Tests.Legality;

/// <summary>
/// Fork guard for event data refreshes. A Pokémon card with no distribution window fails every
/// copy with DateOutsideDistributionWindow, and rebuilding a pickle from EventsGallery alone
/// drops the cards upstream added by hand.
/// </summary>
public class MysteryGiftWindowTests
{
    static MysteryGiftWindowTests() => TestUtil.InitializeLegality();

    private static string[] Missing<T>(T[] cards, Func<T, bool> hasWindow) where T : DataMysteryGift => cards
        .Where(z => z is { IsEntity: true } and IEncounterServerDate { IsDateRestricted: true } && !hasWindow(z))
        .Select(z => $"{typeof(T).Name} {z.CardID:0000} {(Species)z.Species}")
        .ToArray();

    [Fact]
    public void EveryDateRestrictedCardHasWindow()
    {
        string[] missing =
        [
            .. Missing(EncounterEvent.MGDB_G7GG, z => z.GetDistributionWindow(out _)),
            .. Missing(EncounterEvent.MGDB_G8, z => z.GetDistributionWindow(out _)),
            .. Missing(EncounterEvent.MGDB_G8A, z => z.GetDistributionWindow(out _)),
            .. Missing(EncounterEvent.MGDB_G8B, z => z.GetDistributionWindow(out _)),
            .. Missing(EncounterEvent.MGDB_G9, z => z.GetDistributionWindow(out _)),
            .. Missing(EncounterEvent.MGDB_G9A, z => z.GetDistributionWindow(out _)),
        ];
        missing.Should().BeEmpty();
    }

    [Fact]
    public void CardsMissingFromEventsGalleryStayInPickles()
    {
        EncounterEvent.MGDB_G9A.Count(z => z.CardID == 1615).Should().Be(16); // Spewpa, one per pattern
        EncounterEvent.MGDB_G8.Count(z => z.CardID == 9036).Should().Be(1); // HOME Celebi
    }

    [Theory]
    [InlineData(0105, 2026, 10, 31, 2028, 02, 01)] // PokéCenter Audino
    [InlineData(1616, 2026, 11, 05, 2027, 02, 26)] // Poké Ball Pattern Vivillon
    [InlineData(1613, 2026, 11, 11, 2027, 01, 08)] // Shiny Xerneas
    public void WA9GiftIsLegalOnlyInsideWindow(int cardID, int y0, int m0, int d0, int y1, int m1, int d1)
    {
        var card = EncounterEvent.MGDB_G9A.First(z => z.CardID == cardID);
        var tr = new SimpleTrainerInfo(GameVersion.ZA) { Language = (byte)LanguageID.Japanese };
        CheckWindow(card.ConvertToPKM(tr), new(y0, m0, d0), new(y1, m1, d1));
    }

    [Fact]
    public void WC9BirthdayFidoughIsLegalOnlyInsideWindow()
    {
        var card = EncounterEvent.MGDB_G9.First(z => z.CardID == 0072);
        var tr = new SimpleTrainerInfo(GameVersion.SL) { Language = (byte)LanguageID.Japanese };
        CheckWindow(card.ConvertToPKM(tr), new(2026, 10, 31), new(2028, 02, 01));
    }

    // PID type 2 means "always shiny": the game rolls the PID until it is shiny, so a real one can be
    // star or square (a dumped 11/13/2026 copy is square). Only a non-shiny PID is wrong.
    [Fact]
    public void WA9ShinyXerneasIsAnyShiny()
    {
        var card = EncounterEvent.MGDB_G9A.First(z => z.CardID == 1613);
        card.Shiny.Should().Be(Shiny.Always);
        var tr = new SimpleTrainerInfo(GameVersion.ZA) { Language = (byte)LanguageID.English };
        var pk = card.ConvertToPKM(tr);
        pk.IsShiny.Should().BeTrue();
        var la = new LegalityAnalysis(pk);
        la.Valid.Should().BeTrue(la.Report());

        var star = pk.Clone();
        star.PID = (star.PID & 0xFFFF) | ((1u ^ (star.PID & 0xFFFF) ^ star.TID16 ^ star.SID16) << 16);
        new LegalityAnalysis(star).Valid.Should().BeTrue();

        var square = pk.Clone();
        square.PID = (square.PID & 0xFFFF) | ((0u ^ (square.PID & 0xFFFF) ^ square.TID16 ^ square.SID16) << 16);
        new LegalityAnalysis(square).Valid.Should().BeTrue();

        var plain = pk.Clone();
        plain.PID ^= 0x1000_0000;
        plain.IsShiny.Should().BeFalse();
        new LegalityAnalysis(plain).Valid.Should().BeFalse();
    }

    private static void CheckWindow(PKM pk, DateOnly first, DateOnly last)
    {
        pk.MetDate = first;
        var la = new LegalityAnalysis(pk);
        la.Valid.Should().BeTrue(la.Report());

        pk.MetDate = last;
        new LegalityAnalysis(pk).Valid.Should().BeTrue();

        pk.MetDate = first.AddDays(-1);
        new LegalityAnalysis(pk).Valid.Should().BeFalse();

        pk.MetDate = last.AddDays(1);
        new LegalityAnalysis(pk).Valid.Should().BeFalse();
    }
}
