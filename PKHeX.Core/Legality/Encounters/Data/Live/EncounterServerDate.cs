using System;
using System.Collections.Generic;
using static PKHeX.Core.EncounterServerDateCheck;

namespace PKHeX.Core;

/// <summary>
/// Logic to check if a date obtained is within the date of availability.
/// </summary>
public static class EncounterServerDate
{
    private static EncounterServerDateCheck Result(bool result) => result ? Valid : Invalid;

    /// <summary>
    /// Checks if the date obtained is within the date of availability for the given <see cref="enc"/>.
    /// </summary>
    /// <param name="enc">Encounter to check.</param>
    /// <param name="obtained">Date obtained.</param>
    /// <returns>True if the date obtained is within the date of availability for the given <see cref="enc"/>.</returns>
    public static EncounterServerDateCheck IsWithinDistributionWindow(this IEncounterServerDate enc, DateOnly obtained) => enc switch
    {
        WB7 wb7 => Result(wb7.IsWithinDistributionWindow(obtained)),
        WC8 wc8 => Result(wc8.IsWithinDistributionWindow(obtained)),
        WA8 wa8 => Result(wa8.IsWithinDistributionWindow(obtained)),
        WB8 wb8 => Result(wb8.IsWithinDistributionWindow(obtained)),
        WC9 wc9 => Result(wc9.IsWithinDistributionWindow(obtained)),
        WA9 wa9 => Result(wa9.IsWithinDistributionWindow(obtained)),
        EncounterSlot7GO g7 => Result(g7.IsWithinDistributionWindow(obtained)),
        EncounterSlot8GO g8 => Result(g8.IsWithinDistributionWindow(obtained)),
        _ => throw new ArgumentOutOfRangeException(nameof(enc)),
    };

    /// <inheritdoc cref="IsWithinDistributionWindow(IEncounterServerDate,DateOnly)"/>
    public static bool IsWithinDistributionWindow(this WB7 card, DateOnly obtained) => card.GetDistributionWindow(out var window) && window.Contains(obtained);

    /// <inheritdoc cref="IsWithinDistributionWindow(IEncounterServerDate,DateOnly)"/>
    public static bool IsWithinDistributionWindow(this WC8 card, DateOnly obtained) => card.GetDistributionWindow(out var window) && window.Contains(obtained);

    /// <inheritdoc cref="IsWithinDistributionWindow(IEncounterServerDate,DateOnly)"/>
    public static bool IsWithinDistributionWindow(this WA8 card, DateOnly obtained) => card.GetDistributionWindow(out var window) && window.Contains(obtained);

    /// <inheritdoc cref="IsWithinDistributionWindow(IEncounterServerDate,DateOnly)"/>
    public static bool IsWithinDistributionWindow(this WB8 card, DateOnly obtained) => card.GetDistributionWindow(out var window) && window.Contains(obtained);

    /// <inheritdoc cref="IsWithinDistributionWindow(IEncounterServerDate,DateOnly)"/>
    public static bool IsWithinDistributionWindow(this WC9 card, DateOnly obtained) => card.GetDistributionWindow(out var window) && window.Contains(obtained);

    /// <inheritdoc cref="IsWithinDistributionWindow(IEncounterServerDate,DateOnly)"/>
    public static bool IsWithinDistributionWindow(this WA9 card, DateOnly obtained) => card.GetDistributionWindow(out var window) && window.Contains(obtained);

    public static bool GetDistributionWindow(this WB7 card, out DistributionWindow window) => WB7Gifts.TryGetValue(card.CardID, out window);
    public static bool GetDistributionWindow(this WC8 card, out DistributionWindow window) => WC8Gifts.TryGetValue(card.CardID, out window) || WC8GiftsChk.TryGetValue(card.Checksum, out window);
    public static bool GetDistributionWindow(this WA8 card, out DistributionWindow window) => WA8Gifts.TryGetValue(card.CardID, out window);
    public static bool GetDistributionWindow(this WB8 card, out DistributionWindow window) => WB8Gifts.TryGetValue(card.CardID, out window);
    public static bool GetDistributionWindow(this WC9 card, out DistributionWindow window) => WC9Gifts.TryGetValue(card.CardID, out window) || WC9GiftsChk.TryGetValue(card.Checksum, out window);
    public static bool GetDistributionWindow(this WA9 card, out DistributionWindow window) => WA9Gifts.TryGetValue(card.CardID, out window);

    /// <summary>
    /// Initial introduction of HOME support for SW/SH; gift availability (generating) was revised in 3.0.0.
    /// </summary>
    private static readonly DistributionWindow HOME1 = new(2020, 02, 12, 2023, 05, 29);

    /// <summary>
    /// Revision of HOME support for SW/SH; gift availability (generating) was revised in 3.0.0.
    /// </summary>
    private static readonly DistributionWindow HOME3 = new(2023, 05, 30);

    /// <summary>
    /// Introduction of BD/SP and PLA support; gift availability time window for these games.
    /// </summary>
    private static readonly DistributionWindow HOME2_AB = new(2022, 05, 18);

    /// <summary>
    /// Introduction of S/V support; gift availability time window for these games.
    /// </summary>
    private static readonly DistributionWindow HOME3_ML = new(2023, 05, 30);

    /// <summary>
    /// Minimum date the gift can be received.
    /// </summary>
    private static readonly Dictionary<int, DistributionWindow> WB7Gifts = new()
    {
        {0001, new(2018, 11, 16, 2020, 02, 29)}, // ポケセン Birthday Chansey
        {0002, new(2019, 04, 12, 2019, 09, 30)}, // はかせ Mewtwo - Movie pre-order
        {0004, new(2019, 10, 04, 2020, 02, 03)}, // ひみつ Shiny Krabby - Secret Club
        {0501, new(2019, 05, 11, 2019, 08, 29)}, // Bullseye Shiny Eevee/Pikachu - Target USA
        {0502, new(2019, 08, 09, 2020, 04, 30)}, // Giovanni Mewtwo - Europe retail
        {0503, new(2019, 11, 04, 2020, 03, 05)}, // Giovanni Mewtwo - Best Buy USA
        {1001, new(2019, 05, 01, 2019, 07, 31)}, // 베스트프렌드 Shiny Eevee/Pikachu - Korea
        {1006, new(2020, 09, 30, 2020, 11, 30)}, // 박사 Mewtwo - Korean Movie screening
        {1501, new(2024, 10, 26, 2025, 04, 30)}, // Meltan - China password
        {1502, new(2025, 02, 27, 2025, 04, 30)}, // Melmetal - China password
        {1503, new(2025, 01, 29, 2025, 02, 12)}, // 新年快乐 Shiny Arbok - China New Year
        {2001, new(2018, 11, 16)},               // Mew - Poké Ball Plus

        {9028, new(2025, 02, 11)}, // Shiny Meltan
    };

    /// <summary>
    /// Minimum date the gift can be received.
    /// </summary>
    private static readonly Dictionary<int, DistributionWindow> WC8GiftsChk = new()
    {
        // JAP Birthday Milcery
        {0x9D48, new(2020, 11, 01, 2022, 01, 31)}, // Birthday Milcery (Ribbon Sweet) v1
        {0x5DF4, new(2020, 11, 01, 2022, 01, 31)}, // Birthday Milcery (Star Sweet) v1

        // Shiny Galarian Birds (JP Newsletter)
        {0x4081, new(2022, 02, 21)}, // Shiny Galarian Articuno
        {0x4A04, new(2022, 03, 14)}, // Shiny Galarian Zapdos
        {0x3A11, new(2022, 04, 18)}, // Shiny Galarian Moltres

        // Singapore Birthday Milcery
        {0x33ED, new(2020, 11, 06, 2022, 01, 31)}, // Birthday Milcery (Ribbon Sweet) v2
        {0xF351, new(2020, 11, 06, 2022, 01, 31)}, // Birthday Milcery (Star Sweet) v2

        // Shiny Galarian Birds (SG/alt Newsletter)
        {0xDD77, new(2022, 02, 21)}, // Shiny Galarian Articuno
        {0xD7F2, new(2022, 03, 14)}, // Shiny Galarian Zapdos
        {0xA7E7, new(2022, 04, 18)}, // Shiny Galarian Moltres

        // HOME 1.0.0 to 2.0.2 - PID, EC, Height, Weight = 0 (rev 1)
        {0xFBBE, HOME1}, // Bulbasaur
        {0x48F5, HOME1}, // Charmander
        {0x47DB, HOME1}, // Squirtle
        {0x671A, HOME1}, // Pikachu
        {0x81A2, HOME1}, // Original Color Magearna
        {0x4CC7, HOME1}, // Eevee
        {0x1A0B, HOME1}, // Rotom
        {0x1C26, HOME1}, // Pichu

        // HOME 3.0.0 onward - PID, EC, Height, Weight = random (rev 2)
        {0x7124, HOME3}, // Bulbasaur
        {0xC26F, HOME3}, // Charmander
        {0xCD41, HOME3}, // Squirtle
        {0xED80, HOME3}, // Pikachu
        {0x0B38, HOME3}, // Original Color Magearna
        {0xC65D, HOME3}, // Eevee
        {0x9091, HOME3}, // Rotom
        {0x96BC, HOME3}, // Pichu
    };

    /// <summary>
    /// Minimum date the gift can be received.
    /// </summary>
    private static readonly Dictionary<int, DistributionWindow> WC8Gifts = new()
    {
        {1601, new(2019, 11, 15, 2020, 01, 15)}, // GMax Meowth - Early purchase bonus
        {2001, new(2019, 11, 15)},               // Mew - Poké Ball Plus
        {0101, new(2019, 11, 15, 2021, 01, 31)}, // ポケセン Birthday Pikachu/Eevee
        {0107, new(2019, 12, 21, 2020, 01, 31)}, // GMax Snorlax - Jump Festa '20
        {0115, new(2020, 01, 19, 2020, 02, 29)}, // GMax Coalossal - World Hobby Fair
        {0116, new(2020, 01, 19, 2020, 02, 29)}, // GMax Lapras - World Hobby Fair
        {1616, new(2020, 05, 21, 2020, 05, 28)}, // Galarian Mr. Mime HA
        {1618, new(2020, 05, 29, 2020, 06, 04)}, // Galarian Ponyta HA
        {1620, new(2020, 06, 05, 2020, 06, 11)}, // Galarian Corsola HA
        {1622, new(2020, 06, 12, 2020, 06, 16)}, // Galarian Meowth HA
        {0108, new(2020, 08, 07, 2020, 12, 24)}, // Jungle Shiny Celebi (JP)
        {0109, new(2020, 08, 07, 2020, 12, 24)}, // Jungle Zarude (JP)
        {1008, new(2020, 08, 09, 2020, 08, 10)}, // 백종윤 Shiny Amoonguss
        {0505, new(2020, 08, 22)},               // VGC20 Porygon2
        {1007, new(2020, 09, 30, 2020, 10, 15)}, // 지우 Charizard (Korean Movie)
        {1627, new(2020, 10, 30, 2020, 11, 30)}, // Ash Pikachu (Original Cap)
        {1628, new(2020, 10, 30, 2020, 11, 30)}, // Ash Pikachu (Hoenn Cap)
        {1629, new(2020, 10, 30, 2020, 11, 30)}, // Ash Pikachu (Sinnoh Cap)
        {1630, new(2020, 10, 30, 2020, 11, 30)}, // Ash Pikachu (Unova Cap)
        {1631, new(2020, 10, 30, 2020, 11, 30)}, // Ash Pikachu (Kalos Cap)
        {1632, new(2020, 10, 30, 2020, 11, 30)}, // Ash Pikachu (Alola Cap)
        {1633, new(2020, 10, 30, 2020, 11, 30)}, // Ash Pikachu (Partner Cap)
        {1634, new(2020, 10, 30, 2020, 11, 30)}, // Ash Pikachu (World Cap)
        {0121, new(2020, 10, 31, 2020, 11, 30)}, // カ・エール Gastrodon
        {0118, new(2020, 11, 06, 2021, 11, 05)}, // PCSG Birthday Pikachu/Eevee
        {0125, new(2020, 11, 06, 2021, 11, 05)}, // PCSG Birthday Milcery
        {0127, new(2020, 11, 20, 2021, 03, 31)}, // ゲッチャレ Genesect
        {0128, new(2020, 11, 20, 2021, 03, 31)}, // ゲッチャレ Volcanion
        {0129, new(2020, 11, 20, 2021, 03, 31)}, // ゲッチャレ Marshadow
        {0124, new(2020, 12, 19, 2021, 01, 11)}, // ミスド Chansey - Mister Donut
        {0507, new(2020, 12, 17, 2021, 02, 28)}, // Jungle Zarude (Western Newsletter)
        {0110, new(2020, 12, 25, 2021, 02, 28)}, // Jungle Zarude Dada (JP Movie)
        {1635, new(2020, 12, 31, 2021, 01, 15)}, // KIBO Pikachu
        {0509, new(2021, 02, 19, 2021, 03, 19)}, // Shiny Toxtricity Rock Star
        {1636, new(2021, 02, 25, 2021, 03, 25)}, // GF Pikachu - P25 Sing
        {1012, new(2021, 04, 26, 2021, 10, 31)}, // 어드벤처 Genesect (Korea)
        {1013, new(2021, 04, 26, 2021, 10, 31)}, // 어드벤처 Volcanion (Korea)
        {1014, new(2021, 04, 26, 2021, 10, 31)}, // 어드벤처 Marshadow (Korea)
        {1641, new(2021, 06, 17, 2021, 06, 30)}, // HOME GMax Bulbasaur
        {1642, new(2021, 06, 17, 2021, 06, 30)}, // HOME GMax Squirtle
        {0137, new(2021, 07, 18, 2021, 07, 19)}, // クララ Torkoal - PJCS Champion
        {0514, new(2021, 08, 13, 2021, 08, 15)}, // Wolfe GMax Coalossal
        {1023, new(2021, 08, 21, 2021, 08, 22)}, // 정상윤 Porygon-Z
        {0520, new(2021, 10, 07, 2021, 11, 30)}, // Jungle Shiny Celebi (Western)
        {0521, new(2021, 10, 07, 2021, 11, 30)}, // Jungle Zarude Dada (Western)
        {0522, new(2021, 10, 31, 2021, 11, 03)}, // Leonardo GMax Charizard - Global Exhibition 2021
        {1024, new(2021, 10, 14, 2021, 12, 31)}, // Jungle Shiny Celebi (Korea)
        {1025, new(2021, 10, 14, 2021, 12, 31)}, // Jungle Zarude (Korea)
        {1026, new(2021, 10, 14, 2021, 12, 31)}, // Jungle Zarude Dada (Korea)
        {1639, new(2021, 10, 22, 2022, 02, 28)}, // Shiny Zacian (Lancer) - Multi-region
        {1640, new(2021, 10, 22, 2022, 02, 28)}, // Shiny Zamazenta (Arthur) - Multi-region
        {0138, new(2021, 11, 01, 2022, 10, 31)}, // Poké Center Birthday Happiny
        {0523, new(2022, 04, 22, 2022, 04, 25)}, // Marco Dracovish - EUIC
        {0140, new(2022, 06, 11, 2022, 06, 12)}, // コウ Sableye - Japan Championships
        {1027, new(2022, 06, 11, 2022, 06, 12)}, // 정원석 Grimmsnarl - Trainers Cup
        {0141, new(2022, 06, 18, 2022, 06, 19)}, // Jirawiwat Clefairy - Asia Championships
        {0524, new(2022, 06, 24, 2022, 06, 27)}, // Eric Gastrodon - NAIC
        {0142, new(2022, 08, 11, 2022, 09, 30)}, // アルトマーレ Latias
        {0145, new(2022, 08, 11, 2022, 09, 30)}, // サトシ Pikachu (World Cap)
        {0146, new(2022, 08, 12, 2022, 08, 25)}, // サトシ Dracovish
        {1644, new(2022, 08, 18, 2022, 08, 22)}, // Victory Victini - WCS 2022
        {0525, new(2022, 08, 18, 2022, 08, 21)}, // WCS22 Sinistea
        {0143, new(2022, 08, 19, 2022, 09, 30)}, // ネガイボシ Jirachi
        {0144, new(2022, 08, 26, 2022, 09, 30)}, // アラモス Dialga/Palkia
        {0147, new(2022, 08, 26, 2022, 09, 08)}, // サトシ Dragonite
        {0148, new(2022, 09, 02, 2022, 09, 15)}, // サトシ GMax Gengar
        {0151, new(2022, 09, 03, 2022, 09, 30)}, // おつきみ２２ Clefairy
        {0526, new(2022, 09, 09, 2023, 01, 01)}, // Mythical22 Genesect
        {0527, new(2022, 09, 09, 2023, 01, 01)}, // Mythical22 Volcanion
        {0528, new(2022, 09, 09, 2023, 01, 01)}, // Mythical22 Marshadow
        {0149, new(2022, 09, 09, 2022, 09, 22)}, // サトシ Sirfetch'd
        {1643, new(2022, 09, 16, 2023, 01, 01)}, // Shiny Eternatus (Galar)
        {0150, new(2022, 09, 16, 2022, 09, 29)}, // サトシ Lucario

        {9008, new(2020, 06, 02)}, // Hidden Ability Grookey
        {9009, new(2020, 06, 02)}, // Hidden Ability Scorbunny
        {9010, new(2020, 06, 02)}, // Hidden Ability Sobble
        {9011, new(2020, 06, 30)}, // Shiny Zeraora
        {9012, new(2020, 11, 10)}, // Gigantamax Melmetal
        {9013, new(2021, 06, 17)}, // Gigantamax Bulbasaur
        {9014, new(2021, 06, 17)}, // Gigantamax Squirtle
        {9029, new(2025, 02, 11)}, // Shiny Keldeo
    };

    /// <summary>
    /// Minimum date the gift can be received.
    /// </summary>
    private static readonly Dictionary<int, DistributionWindow> WA8Gifts = new()
    {
        {0138, new(2022, 01, 27, 2023, 02, 01)}, // Poké Center Happiny
        {0301, new(2022, 02, 04, 2023, 03, 01)}, // プロポチャ Piplup
        {0801, new(2022, 02, 25, 2022, 06, 01)}, // Teresa Roca Hisuian Growlithe
        {1201, new(2022, 05, 31, 2022, 08, 01)}, // 전이마을 Regigigas
        {1202, new(2022, 05, 31, 2022, 08, 01)}, // 빛나's Piplup
        {1203, new(2022, 08, 18, 2022, 11, 01)}, // Arceus Chronicles Hisuian Growlithe
        {0151, new(2022, 09, 03, 2022, 10, 01)}, // Otsukimi Festival 2022 Clefairy

        {9018, HOME2_AB}, // Hidden Ability Rowlet
        {9019, HOME2_AB}, // Hidden Ability Cyndaquil
        {9020, HOME2_AB}, // Hidden Ability Oshawott
        {9027, new(2025, 01, 27)}, // Shiny Enamorus
    };

    /// <summary>
    /// Minimum date the gift can be received.
    /// </summary>
    private static readonly Dictionary<int, DistributionWindow> WB8Gifts = new()
    {
        {1701, new(2021, 11, 19, 2022, 02, 21)}, // Manaphy Egg - Early purchase bonus
        {0138, new(2021, 11, 19, 2022, 10, 31)}, // Poké Center Birthday Happiny
        {0203, new(2022, 02, 05, 2023, 03, 01)}, // プロポチャ Piplup - Pocha Marche
        {0151, new(2022, 09, 03, 2022, 09, 30)}, // おつきみ２２ Clefairy - Otsukimi
        {1201, new(2022, 06, 01, 2022, 07, 31)}, // 전이마을 Regigigas (Korea)
        {1202, new(2022, 06, 01, 2022, 07, 31)}, // 빛나 Piplup (Korea)

        {9015, HOME2_AB}, // Hidden Ability Turtwig
        {9016, HOME2_AB}, // Hidden Ability Chimchar
        {9017, HOME2_AB}, // Hidden Ability Piplup
        {9026, new(2025, 01, 27)}, // Shiny Manaphy
    };

    /// <summary>
    /// Minimum date the gift can be received.
    /// </summary>
    private static readonly Dictionary<int, DistributionWindow> WC9GiftsChk = new()
    {
        {0xE5EB, new(2022, 11, 17, 2023, 02, 03)}, // Fly Pikachu - rev 1 (male 128 height/weight)
        {0x908B, new(2023, 02, 02, 2023, 03, 01)}, // Fly Pikachu - rev 2 (both 0 height/weight)
    };

    /// <summary>
    /// Minimum date the gift can be received.
    /// </summary>
    private static readonly Dictionary<int, DistributionWindow> WC9Gifts = new()
    {
        {0001, new(2022, 11, 17)}, // PokéCenter Birthday Flabébé
        {0006, new(2022, 12, 16, 2023, 02, 01)}, // Jump Festa Gyarados
        {0501, new(2023, 02, 16, 2023, 02, 21)}, // Jiseok's Garganacl
        {1513, new(2023, 02, 27, 2024, 03, 01)}, // Hisuian Zoroark DLC Purchase Gift
        {0502, new(2023, 03, 31, 2023, 07, 01)}, // TCG Flying Lechonk
        {0503, new(2023, 04, 13, 2023, 04, 18)}, // Gavin's Palafin (-1 start date tolerance for GMT-10 regions)
        {0025, new(2023, 04, 21, 2023, 08, 01)}, // Pokémon Center Pikachu (Mini & Jumbo)
        {1003, new(2023, 05, 29, 2023, 08, 01)}, // Arceus and the Jewel of Life Distribution - Pokémon Store Tie-In Bronzong
        {1002, new(2023, 05, 31, 2023, 08, 01)}, // Arceus and the Jewel of Life Distribution Pichu
        {0028, new(2023, 06, 09, 2023, 06, 12)}, // そらみつ's Bronzong (-1 start date tolerance for GMT-10 regions)
        {1005, new(2023, 06, 16, 2023, 06, 20)}, // 정원석's Gastrodon (-1 start date tolerance for GMT-10 regions)
        {0504, new(2023, 06, 30, 2023, 07, 04)}, // Paul's Shiny Arcanine
        {1522, new(2023, 07, 21, 2023, 09, 01)}, // Dark Tera Type Charizard
        {0024, new(2023, 07, 26, 2023, 08, 19)}, // Nontaro's Shiny Grimmsnarl
        {0505, new(2023, 08, 07, 2023, 09, 01)}, // WCS 2023 Stretchy Form Tatsugiri
        {1521, new(2023, 08, 08, 2023, 09, 19)}, // My Very Own Mew
        {0506, new(2023, 08, 10, 2023, 08, 15)}, // Eduardo Gastrodon
        {1524, new(2023, 09, 06, 2024, 09, 01)}, // Glaseado Cetitan
        {0507, new(2023, 10, 13, 2024, 01, 01)}, // Trixie Mimikyu
        {0031, new(2023, 11, 01, 2025, 02, 01)}, // PokéCenter Birthday Charcadet and Pawmi
        {1006, new(2023, 11, 02, 2024, 01, 01)}, // Korea Bundle Fidough
        {0508, new(2023, 11, 17, 2023, 11, 21)}, // Alex's Dragapult
        {1526, new(2023, 11, 22, 2024, 11, 01)}, // Team Star Revavroom
        {1529, new(2023, 12, 07, 2023, 12, 22)}, // New Moon Darkrai
        {1530, new(2023, 12, 07, 2024, 01, 04)}, // Shiny Buddy Lucario
        {1527, new(2023, 12, 13, 2024, 12, 01)}, // Paldea Gimmighoul
        {0036, new(2023, 12, 14, 2024, 02, 14)}, // コロコロ Roaring Moon and Iron Valiant
        {1007, new(2023, 12, 29, 2024, 02, 11)}, // 윈터페스타 Baxcalibur
        {0038, new(2024, 01, 14, 2024, 03, 14)}, // コロコロ Scream Tail, Brute Bonnet, Flutter Mane, Iron Hands, Iron Jugulis, and Iron Thorns
        {0048, new(2024, 02, 22, 2024, 04, 01)}, // Project Snorlax Campaign Gift
        {1534, new(2024, 03, 12, 2025, 03, 01)}, // YOASOBI Pawmot
        {1535, new(2024, 03, 14, 2024, 10, 01)}, // Liko's Sprigatito
        {0509, new(2024, 04, 04, 2024, 04, 09)}, // Marco's Iron Hands
        {1008, new(2024, 05, 04, 2024, 05, 08)}, // 신여명's Flutter Mane
        {0052, new(2024, 05, 11, 2024, 07, 01)}, // Sophia's Gyarados
        {1536, new(2024, 05, 18, 2024, 12, 01)}, // Dot's Quaxly
        {0049, new(2024, 05, 31, 2024, 06, 03)}, // ナーク's Talonflame
        {0510, new(2024, 06, 07, 2024, 06, 11)}, // Nils's Porygon2
        {0050, new(2024, 07, 13, 2024, 10, 01)}, // Japan's Pokéss Summer Festival Eevee
        {1537, new(2024, 07, 24, 2025, 02, 01)}, // Roy's Fuecoco
        {0511, new(2024, 08, 15, 2024, 08, 31)}, // WCS 2024 Steenee
        {0512, new(2024, 08, 16, 2024, 08, 20)}, // Tomoya's Sylveon
        {0062, new(2024, 10, 31, 2026, 02, 01)}, // PokéCenter Birthday Tandemaus
        {0513, new(2024, 11, 15, 2024, 11, 23)}, // Patrick's Pelipper
        {0054, new(2024, 11, 21, 2025, 06, 01)}, // Operation Get Mythical's JPN Keldeo
        {0055, new(2024, 11, 21, 2025, 06, 01)}, // Operation Get Mythical's JPN Zarude
        {0056, new(2024, 11, 21, 2025, 06, 01)}, // Operation Get Mythical's JPN Deoxys
        {1011, new(2024, 11, 21, 2025, 06, 01)}, // Operation Get Mythical's KOR Keldeo
        {1012, new(2024, 11, 21, 2025, 06, 01)}, // Operation Get Mythical's KOR Zarude
        {1013, new(2024, 11, 21, 2025, 06, 01)}, // Operation Get Mythical's KOR Deoxys
        {1010, new(2025, 01, 21, 2025, 04, 01)}, // Pokémon Lucario & The Mystery of Mew Movie Gift KOR 아론's Lucario
        {0514, new(2025, 02, 05, 2025, 07, 01, +2)}, // Pokémon Day 2025 Flying Tera Type Eevee
        {0519, new(2025, 02, 20, 2025, 03, 01)}, // Marco's Jumpluff
        {0066, new(2025, 04, 18, 2025, 08, 01)}, // Wei Chyr's Rillaboom
        {1019, new(2025, 04, 24, 2025, 07, 01)}, // Pokémon Town - KOR Ditto Project
        {1020, new(2025, 06, 06, 2025, 06, 10)}, // PTC 2025 홍주영's Porygon2
        {0523, new(2025, 06, 13, 2025, 06, 21)}, // NAIC 2025 Wolfe's Incineroar
        {0067, new(2025, 06, 20, 2025, 06, 23)}, // PJCS 2025 Hyuma Hara's Flutter Mane
        {0068, new(2025, 06, 20, 2025, 10, 01)}, // PJCS 2025 Ray Yamanaka's Amoonguss
        {1542, new(2025, 08, 07, 2025, 10, 01)}, // Shiny Wo-Chien
        {1544, new(2025, 08, 21, 2025, 10, 01)}, // Shiny Chien-Pao
        {1546, new(2025, 09, 04, 2025, 10, 01)}, // Shiny Ting-Lu
        {1548, new(2025, 09, 18, 2025, 10, 01)}, // Shiny Chi-Yu
        {0524, new(2025, 08, 14, 2025, 08, 31)}, // WCS 2025 Toedscool
        {0525, new(2025, 08, 15, 2025, 08, 23)}, // WCS 2025 Luca Ceribelli's Farigiraf
        {1540, new(2025, 09, 25, 2025, 10, 25)}, // Shiny Miraidon / Koraidon Gift
        {0070, new(2025, 10, 31, 2027, 02, 01)}, // PokéCenter Fidough Birthday Gift
        {0526, new(2025, 11, 21, 2025, 12, 01)}, // LAIC 2026 Federico Camporesi’s Whimsicott
        {0527, new(2026, 02, 12, 2026, 02, 21)}, // EUIC 2026 Yuma Kinugawa's Hisuian Typhlosion 

        {9021, HOME3_ML}, // Hidden Ability Sprigatito
        {9022, HOME3_ML}, // Hidden Ability Fuecoco
        {9023, HOME3_ML}, // Hidden Ability Quaxly
        {9024, new(2024, 10, 16)}, // Shiny Meloetta
        {9025, new(2024, 11, 01)}, // PokéCenter Birthday Tandemaus
        {9030, new(2025, 10, 31)}, // PokéCenter Fidough Birthday Gift
    };

    /// <summary>
    /// Minimum date the gift can be received.
    /// </summary>
    private static readonly Dictionary<int, DistributionWindow> WA9Gifts = new()
    {
        {1601, new(2025, 10, 14, 2026, 03, 01, +2)}, // Ralts holding Gardevoirite
        {0102, new(2025, 10, 23, 2026, 02, 01, +2)}, // Slowpoke PokéCenter Gift
        {0101, new(2025, 10, 31, 2027, 02, 01)}, // PokéCenter Audino Birthday Gift
        {1607, new(2025, 12, 09, 2026, 01, 20)}, // Alpha Charizard
    };
}
