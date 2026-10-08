using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using static PKHeX.Core.LegalityCheckResultCode;

namespace PKHeX.Core.Tests.Legality;

public class MovePPReportTests
{
    static MovePPReportTests() => TestUtil.InitializeLegality();

    private static PA9 GetLegalPA9()
    {
        var tr = new SimpleTrainerInfo(GameVersion.ZA);
        var template = new PA9 { Species = (ushort)Species.Pikachu };
        var enc = EncounterMovesetGenerator.GenerateEncounters(template, tr, ReadOnlyMemory<ushort>.Empty, GameVersion.ZA).First();
        var pk = (PA9)enc.ConvertToPKM(tr);
        new LegalityAnalysis(pk).Valid.Should().BeTrue("the starting point must be legal so only the PP edit is flagged");
        return pk;
    }

    [Fact]
    public void ZeroPPReportsExpectedHealedWithBothValues()
    {
        var pk = GetLegalPA9();
        var healed = pk.GetMovePP(pk.Move1, pk.Move1_PPUps);
        pk.Move1_PP = pk.Move2_PP = pk.Move3_PP = pk.Move4_PP = 0;

        var la = new LegalityAnalysis(pk);
        la.Results.Should().Contain(z => z.Result == MovePPExpectHealed_01 && z.Argument == 1 && z.Argument2 == healed);

        var report = la.Report();
        report.Should().Contain($"Move 1 PP is below the amount expected ({healed}).");
    }

    [Fact]
    public void PPAboveMaxReportsTooHighWithBothValues()
    {
        var pk = GetLegalPA9();
        pk.Move1_PP = 99;

        var la = new LegalityAnalysis(pk);
        la.Report().Should().Contain("Move 1 PP is above the amount allowed (99).");
    }

    [Fact]
    public void PPUpsReportsTooHighWithBothValues()
    {
        var pk = GetLegalPA9();
        pk.Move1_PPUps = 3;

        var la = new LegalityAnalysis(pk);
        la.Report().Should().Contain("Move 1 PP Ups is above the amount allowed (3).");
    }

    [Fact]
    public void EveryArgumentCodeFormatsInEveryLanguage()
    {
        var la = new LegalityAnalysis(GetLegalPA9());
        foreach (var language in GameLanguage.AllSupportedLanguages)
        {
            var context = LegalityLocalizationContext.Create(la, language);
            for (var code = FirstWithArgument; code < FirstWithMove; code++)
            {
                var chk = CheckResult.Get(Severity.Invalid, CheckIdentifier.CurrentMove, code, 1 | (2u << 16));
                string? error = null;
                try { _ = context.Humanize(chk); }
                catch (FormatException ex) { error = ex.Message; }
                error.Should().BeNull($"{code} ({language}) must format with the arguments its range provides");
            }
        }
    }
}
