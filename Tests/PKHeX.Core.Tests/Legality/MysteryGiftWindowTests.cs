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
