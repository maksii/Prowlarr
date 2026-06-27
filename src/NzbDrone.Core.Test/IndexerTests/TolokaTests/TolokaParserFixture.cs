using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.TolokaTests
{
    [TestFixture]
    public class TolokaParserFixture : CoreTest
    {
        private static readonly IndexerCategory[] TvCategory = { NewznabStandardCategory.TV };
        private static readonly IndexerCategory[] MovieCategory = { NewznabStandardCategory.Movies };
        private static readonly IndexerCategory[] AnimeCategory = { NewznabStandardCategory.TVAnime };
        private static readonly IndexerCategory[] DocCategory = { NewznabStandardCategory.TVDocumentary };
        private static readonly IndexerCategory[] OtherTvCategory = { NewznabStandardCategory.TVOther };

        // Singular NOUN-first "серія 12 з 12" = the single 12th episode -> E12; bare "(12 з 12)" = a count -> E01-E12.
        [TestCase("Нянпір / Nyanpire The Animation (серія 12 з 12) (2011) HDTVRip Ukr/Jap | Sub Ukr", ExpectedResult = "Nyanpire The Animation E12 (2011) HDTV Ukrainian")]
        [TestCase("Детективне агентство прекрасних хлопчиків (12 з 12) / Bishounen Tanteidan (2021) BDRip 1080p Ukr/Jap | Ukr Sub", ExpectedResult = "Bishounen Tanteidan E01-E12 (2021) BluRay 1080p Ukrainian")]
        // Wave-1: number-first season list "(1, 2 сезони)" -> S01-S02; "+ 5 Спешлів" count -> S01 + Specials; "+ОВА" -> OVA.
        [TestCase("Привид в латах (1, 2 сезони) / Ghost in the Shell: Stand Alone Complex (2002-2004) BDRip 1080p 2xUkr/Jap | Sub Ukr", ExpectedResult = "Ghost in the Shell: Stand Alone Complex S01-S02 (2002-2004) BluRay 1080p Ukrainian")]
        [TestCase("Окультна Академія / Seikimatsu Occult Gakuin (Сезон 1 + 5 Спешлів) (2010) BDRip 1080p Ukr/Jap | Sub Ukr", ExpectedResult = "Seikimatsu Occult Gakuin S01 (2010) BluRay 1080p Specials Ukrainian")]
        [TestCase("Поневіряння мага Орфена / Majutsushi Orphen Hagure Tabi (сезон 1+ОВА) (2020) WEBDL 720p", ExpectedResult = "Majutsushi Orphen Hagure Tabi S01 (2020) WEB-DL 720p OVA")]
        // Episode-count "of XX / ???" unknown-total placeholder: "Сезон 4, серії 11 з ХХ" = 11 of XX episodes (a
        // count) -> S04E01-E11 (not S04E11); "Сезон 4, 1-11 з ???" = episodes 1-11 -> S04E01-E11 (not S01-S11).
        [TestCase("Моє переродження в Слиз (Сезон 4, серії 11 з ХХ) / Tensei shitara Slime Datta Ken (Season 4) (2026) WEBDLRip 1080p H.265 Ukr/Jap | sub Ukr", ExpectedResult = "Tensei shitara Slime Datta Ken S04E01-E11 (2026) WEBRip 1080p x265 Ukrainian")]
        [TestCase("Про моє переродження в слиз (Сезон 4, 1-11 з ???) / Tensei shitara Slime Datta Ken (Season 4) (2026) WEBDLRip 1080p H.264", ExpectedResult = "Tensei shitara Slime Datta Ken S04E01-E11 (2026) WEBRip 1080p x264")]
        public string parses_zero_floor_anime(string title)
        {
            return new TolokaTitleParser().Parse(title, AnimeCategory, true);
        }

        // A full date "(2014.07.13)" collapses to its year; reverse homoglyph "Свiт"->"Світ" + "9 випусків"->E01-E09;
        // specials' own episode numbers ("+ specials (episodes 9-10)") must NOT become the main season.
        [TestCase("«Вікна-Новини» Спецрепортаж - Слов'янськ після смерті (2014.07.13) SATRip", ExpectedResult = "Вікна-Новини Спецрепортаж - Слов'янськ після смерті (2014) HDTV")]
        [TestCase("Свiт Атома (2011) DVB-TVRip-AVC (9 випусків)", ExpectedResult = "Світ Атома E01-E09 (2011) TVRip x264")]
        [TestCase("The Blue Planet (season 1) (2001) BDRip 720p + specials (episodes 9-10)", ExpectedResult = "The Blue Planet S01 (2001) BluRay 720p Specials")]
        public string parses_zero_floor_documentary(string title)
        {
            return new TolokaTitleParser().Parse(title, DocCategory, true);
        }

        // A source token in the NAME, before the year ("...India Special SatRip (2011)") -> HDTV source slot.
        [TestCase("Топ Ґір. Спецвипуск. Індія / Top Gear. India Special SatRip (2011)", ExpectedResult = "Top Gear. India Special (2011) HDTV")]
        // Wave-1: reverse-homoglyph "[Усi серiї]" not forward-mangled to "Yci"; music "VA"/genre tags keep Cyrillic;
        // Cyrillic-month "(Червень 2019)" -> (2019).
        [TestCase("Ігронавти [Усi серiї] (2011-2016) TVRip-AVC 360p / 480p / 720p", ExpectedResult = "Ігронавти (2011-2016) TVRip x264 360p 480p 720p")]
        [TestCase("VA - Український самоспів. Частина перша (2016) DVDRip | Поп, Рок / Караоке", ExpectedResult = "VA - Український самоспів. Частина перша (2016) DVDRip")]
        [TestCase("Збірка музичних відео (Червень 2019) SiteRip 1080р | Pop, Rock, Rap etc.", ExpectedResult = "Збірка музичних відео (2019) WEBRip 1080p")]
        public string parses_zero_floor_other(string title)
        {
            return new TolokaTitleParser().Parse(title, OtherTvCategory, true);
        }

        [Test]
        public void should_not_duplicate_season_tag_when_stripping_cyrillic()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Правдива терапія (Сезон 1, серії 1-2) / Shrinking (Season 1, episodes 1-2) (2023) WEBRip 1080p Ukr/Eng",
                TvCategory,
                true);

            result.Should().Be("Shrinking S01E01-E02 (2023) WEBRip 1080p Ukrainian");
        }

        [Test]
        public void should_keep_both_localized_and_original_tags_when_cyrillic_kept()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Правдива терапія (Сезон 1, серії 1-2) / Shrinking (Season 1, episodes 1-2) (2023) WEBRip 1080p Ukr/Eng",
                TvCategory,
                false);

            result.Should().Be("Правдива терапія (S01E01-02) / Shrinking (S01E01-02) (2023) WEBRip 1080p Ukr/Eng");
        }

        // Season RANGE + trailing episode count: the count must NOT become a season endpoint.
        [TestCase("Малюки Луні Тюнз / Baby Looney Tunes (Сезон 1-2, 52 серії з 55) (2002) SATRip 720p", ExpectedResult = "Baby Looney Tunes S01-S02 (2002) HDTV 720p")]
        // Single season + episode RANGE (keyword after).
        [TestCase("Бургери Боба / Bob's Burgers (Сезон 1, 9-13 серії) (2011) WEB-DL 1080p", ExpectedResult = "Bob's Burgers S01E09-E13 (2011) WEB-DL 1080p")]
        // Single season + episode COUNT -> season pack only.
        [TestCase("Елвін / Alvin and the Chipmunks (Сезон 3, 44 серій з 52) (2017) WEB-DL 1080p", ExpectedResult = "Alvin and the Chipmunks S03E01-E44 (2017) WEB-DL 1080p")]
        // Yearless release: anchor on the source token, drop language clutter.
        [TestCase("Дім Давида (Сезон 2) / House of David (Season 2) WEB-DL 1080p Ukr/Eng", ExpectedResult = "House of David S02 WEB-DL 1080p Ukrainian")]
        // Per-segment repeated year: anchor on the LAST year so the Latin title survives.
        [TestCase("Готель дель Луна (Сезон 1) (2019) / Hotel del Luna (Season 1) (2019) HDTVRip 1080p", ExpectedResult = "Hotel del Luna S01 (2019) HDTV 1080p")]
        // Standalone "N-M серії" episode range with no season prefix; Cyrillic-only name preserved.
        [TestCase("Гвардія (1-4 серії, з 12) (2015) WEB-DLRip 720p", ExpectedResult = "Гвардія E01-E04 (2015) WEBRip 720p")]
        // Heavily homoglyphed source ("WЕВDLRір-АVС") normalized; "Випуски" recognized as episodes.
        [TestCase("Вар'яти Шоу (Сезон 1, Випуски 1-10) (2013) WЕВDLRір-АVС", ExpectedResult = "Вар'яти Шоу S01E01-E10 (2013) WEBRip x264")]
        // Specials markers in the many real forms uploaders use (verified against live Toloka). Before, only the bare
        // plurals "спецепізоди"/"спецвипуски" and Latin OVA/ONA were matched; the common singulars, the Cyrillic
        // look-alike "ОВА", the transliterated "спешл" and "OAD" were silently dropped on rebuild.
        // Cyrillic "ОВА" -> "OVA" (90 live rows write OVA in Cyrillic); audio is Japanese here (subs Ukr).
        [TestCase("Безтурботні часи (Сезон 1 + ОВА) / Non Non Biyori (Season 1 + OVA) (2013) BDRip 1080p H.265 Jap | sub Ukr", ExpectedResult = "Non Non Biyori S01 (2013) BluRay 1080p x265 OVA Japanese")]
        // Transliterated "спешл" -> "Specials".
        [TestCase("Робота Клітин (Сезон 1 + спешл) / Hataraku Saibou (Season 1) (2018) BDRip 1080p H.265 60fps Ukr/Jap | Sub Ukr", ExpectedResult = "Hataraku Saibou S01 (2018) BluRay 1080p x265 Specials Ukrainian")]
        // "спецепізоди" -> "Specials".
        [TestCase("Макросс 7 (Сезон 1 + спецепізоди) / Macross 7 (Season 1) (1994) BDRip 1080p H.265 Ukr/Jap | sub Ukr", ExpectedResult = "Macross 7 S01 (1994) BluRay 1080p x265 Specials Ukrainian")]
        // "OAD" (anime Original Animation DVD) is preserved as its own tag, not collapsed to OVA.
        [TestCase("Це провина не моя / WataMote! (Season 1 + OAD) (2013) WEB-DLRip 720p Ukr/Jap", ExpectedResult = "WataMote S01 (2013) WEBRip 720p OAD Ukrainian")]
        // "Спецсерії" -> "Specials"; the cross-season "302 серії" grand total is dropped (it is not season-relative,
        // and the "+Спецсерії" between the range and the count no longer breaks the season-range+count rule), so the
        // multi-season pack collapses cleanly to "S01-S12" with a Specials tag (matching Jackett).
        [TestCase("Щенячий патруль (Сезон 1-12+Спецсерії, 302 серії) / PAW Patrol (Season 1-12) (2013-2026) WEB-DLRip 1080p H.265 Ukr/Eng | sub Eng", ExpectedResult = "PAW Patrol S01-S12 (2013-2026) WEBRip 1080p x265 Specials Ukrainian")]
        // "серія 1 з 8" = the single 1st episode -> S03E01 (no degenerate S03E01-E01).
        [TestCase("Дім дракона (Сезон 3, серія 1 з 8) / House of the Dragon (Season 3) (2026) WEB-DL 1080p 2xUkr/Eng | Sub Eng", ExpectedResult = "House of the Dragon S03E01 (2026) WEB-DL 1080p Ukrainian")]
        // "Сезон 1, 5 з 13" = season 1, 5 of 13 episodes available -> S01E01-E05.
        [TestCase("Гарлі Квінн (Сезон 1, 5 з 13) / Harley Quinn (Season 1) (2019) BDRip 1080p H.265 Ukr/Eng | Sub Ukr", ExpectedResult = "Harley Quinn S01E01-E05 (2019) BluRay 1080p x265 Ukrainian")]
        // Bare (un-parenthesized) year "2019" before the resolution anchors the rebuild; Cyrillic name kept.
        [TestCase("Перші ластівки / Сезон 1 (Серії 8 з 8) 2019 480p", ExpectedResult = "Перші ластівки S01E01-E08 (2019) 480p")]
        // 2-digit years in a collection list "(1997,2000,02,06)" expand and collapse to a span.
        [TestCase("Каю / Кайю (Сезон 1, 7 серій, Сезон 2, 4 серії, Сезон 4, 1 серія, Сезон 5, 2 серії) / Caillou (Season 1, 7 episodes, Season 2, 4episodes, Season 4, 1 episode, Season 5, 1 episodes) (1997,2000,02,06)", ExpectedResult = "Caillou S01-S05 (1997-2006)")]
        // Multi-season comma list with a Latin twin -> "H S01-S04".
        [TestCase("Лікарня (Сезон 1, 2, 3, 4) / H (Season 1, 2, 3, 4) (1998-2001) DVDRip Ukr/Fre", ExpectedResult = "H S01-S04 (1998-2001) DVDRip Ukrainian")]
        // Part info already in the (Cyrillic) name -> the "Part" edition is not appended a second time.
        [TestCase("Правила життя. Вся правда про хліб. Частини 1-2 (2010) SatRip", ExpectedResult = "Правила життя. Вся правда про хліб. Частини 1-2 (2010) HDTV")]
        // A Latin alias left in parens once the Cyrillic main name is stripped ("Пилосос (Pilesos)") -> "Pilesos".
        [TestCase("Пилосос (Pilesos) (2009) | 1-10 серії SiteRip", ExpectedResult = "Pilesos E01-E10 (2009) WEBRip")]
        // "5 з XXX серії" (unknown total placeholder) -> 5 episodes available -> S01E01-E05.
        [TestCase("Пес Патрон (1 сезон 5 з XXX серії) WEBDLRip 720p", ExpectedResult = "Пес Патрон S01E01-E05 WEBRip 720p")]
        // Reverse homoglyph: a Latin "c"/"i" inside "cерiї" lets the episode keyword match ("22 cерiї з 52" -> E01-E22).
        [TestCase("Кароліна та її друзі (22 cерiї з 52) / Caroline And Her Friends (1994) VHSRip", ExpectedResult = "Caroline And Her Friends E01-E22 (1994) SDTV")]
        // Wave-1: comma-optional "Сезон 1 Серії 3-13"; number-first "2 сезон 1-18 серії"; "12 з 116 епізодів" -> E01-E12;
        // cross-season pack -> S01-S03; disc range/count -> not episodes (DVD); HDTRip -> HDTV; "(Mini Series)" -> S01;
        // bare year RANGE "2006-2007".
        [TestCase("Зелений ліхтар (Сезон 1 Серії 3-13 з 26) / Green Lantern: The Animated Series (Season 1 Episodes 3-13) (2011-2013) WEB-DL 720p Ukr/Eng", ExpectedResult = "Green Lantern: The Animated Series S01E03-E13 (2011-2013) WEB-DL 720p Ukrainian")]
        [TestCase("Коли ми вдома (2 сезон 1-18 серії) (2015) SATRip", ExpectedResult = "Коли ми вдома S02E01-E18 (2015) HDTV")]
        [TestCase("Скарби зі звалища / Auction Hunters (12 з 116 епізодів) (2010) TVRip", ExpectedResult = "Auction Hunters E01-E12 (2010) TVRip")]
        [TestCase("1000 способів померти (Сезон 1, Сезон 2 (2-3, 5-10), Сезон 3) / 1000 Ways To Die (Season 1-3) (2008-2010) HDTVRip", ExpectedResult = "1000 Ways To Die S01-S03 (2008-2010) HDTV")]
        [TestCase("Бетмен (Диски 1-16 з 16) / Batman: The Animated Series (Discs 1-16 of 16) 16xDVD9 (1992-1999) Ukr/Eng/Fra | Sub Eng", ExpectedResult = "Batman: The Animated Series (1992-1999) DVD Ukrainian")]
        [TestCase("Анатомія Грей / Grey's Anatomy Season 6, Episodes 1-4 (2009) HDTRip Eng | sub Ukr", ExpectedResult = "Grey's Anatomy S06E01-E04 (2009) HDTV English")]
        [TestCase("Феєрія мандрів (26 випусків) 2006-2007 TVRip", ExpectedResult = "Феєрія мандрів E01-E26 (2006-2007) TVRip")]
        [TestCase("Острів скарбів / Treasure Island (Mini Series) (2012) BDRemux 1080p Ukr/Eng | Sub Ukr", ExpectedResult = "Treasure Island S01 (2012) BluRay Remux 1080p Ukrainian")]
        public string parses_tv_season_episode_combos(string title)
        {
            return new TolokaTitleParser().Parse(title, TvCategory, true);
        }

        // --- Zero-floor round: name preservation, episodes, dates, editions, sources ---
        // Cyrillic-only name PRESERVED whole (only Latin is a "[ENG Transfer]" note) - never a lone apostrophe.
        [TestCase("Крихка Пам'ять (2022) WEB-DL 1080p | Sub Eng [ENG Transfer]", ExpectedResult = "Крихка Пам'ять (2022) WEB-DL 1080p")]
        // Stylized bracket title "[Rec]²": superscript folded, bracket unwrapped -> "Rec 2".
        [TestCase("Репортаж 2 / [Rec]² (2009) BDRemux 1080p Ukr/Spa | sub Eng", ExpectedResult = "Rec 2 (2009) BluRay Remux 1080p Ukrainian")]
        // Bracketed editions preserved; DVDRemux keeps the Remux tier; doubled-Part info not re-appended.
        [TestCase("Красуня і Чудовисько / Beauty and the Beast (1991) BDRemux 1080p Ukr/Eng | Sub Ukr/Eng [Special Edition]", ExpectedResult = "Beauty and the Beast (1991) BluRay Remux 1080p Special Edition Ukrainian")]
        [TestCase("Золота лихоманка / The Gold Rush (1925) BDRip 1080p H.265 Eng | Sub Ukr/Eng [Silent Version]", ExpectedResult = "The Gold Rush (1925) BluRay 1080p x265 Silent Version English")]
        [TestCase("Сніговий гонщик / Кевін із півночі / Chilly Dogs / Kevin of the North (2001) DVDRemux 2xUkr/Eng | Sub Eng", ExpectedResult = "Kevin of the North (2001) DVD Remux Ukrainian")]
        [TestCase("Відомий Львів невідомий. Частина 1 / Known and Unknown Lviv. Vol. 1 (2005) DVD5 Ukr", ExpectedResult = "Known and Unknown Lviv. Vol. 1 (2005) DVD Ukrainian")]
        // Alphanumeric "F9" and pure-number "2067" are valid names (not rejected as junk).
        [TestCase("Форсаж 9: Нестримна сага / F9 (2021) BDRip 720p Ukr/Eng | Sub Eng", ExpectedResult = "F9 (2021) BluRay 720p Ukrainian")]
        [TestCase("2067: Петля часу / 2067 (2020) BDRip 1080p H.265 2xUkr/Eng | sub Eng", ExpectedResult = "2067 (2020) BluRay 1080p x265 Ukrainian")]
        // Wave-1: DVD-5 -> DVD; bare "1080" after a source -> 1080p; "[Complete Restored Edition]" preserved;
        // "Vol. I-II" not double-Parted.
        [TestCase("Бандерівці (2008) DVD-5", ExpectedResult = "Бандерівці (2008) DVD")]
        [TestCase("Кілька / A Few Moments of Cheers (2024) BDRip 1080 Jap", ExpectedResult = "A Few Moments of Cheers (2024) BluRay 1080p Japanese")]
        [TestCase("Метрополіс / Metropolis [Complete Restored Edition] (1927) BDRip Ger | sub Ukr", ExpectedResult = "Metropolis (1927) BluRay Complete Restored Edition German")]
        [TestCase("Німфоманка: Частина 1-2 / Nymphomaniac: Vol. I-II (2013) BDRip-AVC Eng | sub Ukr/Eng", ExpectedResult = "Nymphomaniac: Vol. I-II (2013) BluRay x264 English")]
        public string parses_zero_floor_movies(string title)
        {
            return new TolokaTitleParser().Parse(title, MovieCategory, true);
        }

        // "Of <number>" is part of a real MOVIE title and must NOT be stripped as an episode count.
        [TestCase("Клас 1999го / Class Of 1999 (1990) BDRip-AVC Ukr/Eng | Sub Ukr/Eng", ExpectedResult = "Class Of 1999 (1990) BluRay x264 Ukrainian")]
        // "Lang sub" = subtitle language, not audio: French audio must win over the Ukrainian subtitle.
        [TestCase("Примарна Індія / L'Inde fantôme (1969) DVDRip Fre / Ukr sub", ExpectedResult = "L'Inde fantôme (1969) DVDRip French")]
        // Pure-Latin original title ("Joy Ride") beats a mixed localized segment ("Check-in у халепу").
        [TestCase("Check-in у халепу / Весела поїздочка / Joy Ride (2023) BDRip 1080p 2xUkr/Eng | Sub Eng", ExpectedResult = "Joy Ride (2023) BluRay 1080p Ukrainian")]
        public string parses_movie_titles(string title)
        {
            return new TolokaTitleParser().Parse(title, MovieCategory, true);
        }

        // --- Format/language standardization round ---
        // BDRemux -> "BluRay Remux" (Remux tier); HDR10/DV preserved; Esp->Spanish, Deu->German; x-first multi-dub "x2Ukr".
        [TestCase("Дюна / Dune (2021) BDRemux 1080p H.265 Ukr/Eng | Sub Ukr", ExpectedResult = "Dune (2021) BluRay Remux 1080p x265 Ukrainian")]
        [TestCase("Аватар / Avatar (2022) BDRemux 2160p HDR10 DV H.265 Ukr/Eng", ExpectedResult = "Avatar (2022) BluRay Remux 2160p HDR10 DV x265 Ukrainian")]
        [TestCase("Лабіринт фавна / El laberinto del fauno (2006) BDRip 1080p Esp | Sub Ukr", ExpectedResult = "El laberinto del fauno (2006) BluRay 1080p Spanish")]
        [TestCase("Бункер / Der Untergang (2004) BDRip 1080p Deu | Sub Ukr", ExpectedResult = "Der Untergang (2004) BluRay 1080p German")]
        [TestCase("Атака титанів / Attack on Titan (2013) BDRip 1080p H.265 x2Ukr/Jap | Sub Eng", ExpectedResult = "Attack on Titan (2013) BluRay 1080p x265 Ukrainian")]
        public string parses_format_language_round_movie(string title)
        {
            return new TolokaTitleParser().Parse(title, MovieCategory, true);
        }

        [Test]
        public void should_normalize_source_tokens_when_quality_normalization_on()
        {
            // Default (toggle on): Toloka's "BDRemux" is mapped to the canonical "BluRay Remux" tier.
            var result = new TolokaTitleParser().Parse(
                "Дюна / Dune (2021) BDRemux 1080p H.264 Ukr/Eng | Sub Ukr",
                MovieCategory, true, null, null, exactRanges: false, normalizeQuality: true);
            result.Should().Be("Dune (2021) BluRay Remux 1080p x264 Ukrainian");
        }

        [Test]
        public void should_keep_original_source_tokens_when_quality_normalization_off()
        {
            // Toggle off: keep Toloka's original source token ("BDRemux"); resolution + codec are still normalized.
            var result = new TolokaTitleParser().Parse(
                "Дюна / Dune (2021) BDRemux 1080p H.264 Ukr/Eng | Sub Ukr",
                MovieCategory, true, null, null, exactRanges: false, normalizeQuality: false);
            result.Should().Be("Dune (2021) BDRemux 1080p x264 Ukrainian");
        }

        [Test]
        public void should_reconstruct_tv_season_for_archive_video_mixed_category()
        {
            // Archive video (forum 72) and unformatted video (45) map to BOTH Movies and TV. A TV title in that mix
            // must still be reconstructed with its season token (it would be passed through verbatim under "Other").
            var categories = new[] { NewznabStandardCategory.Movies, NewznabStandardCategory.TV };
            var result = new TolokaTitleParser().Parse(
                "Дім Давида (Сезон 2) / House of David (Season 2) WEB-DL 1080p Ukr/Eng",
                categories, true);
            result.Should().StartWith("House of David S02");
            result.Should().Contain("WEB-DL 1080p");
        }

        [Test]
        public void should_pick_romaji_title_and_read_audio_when_pipe_separates_titles()
        {
            // " | " is a TITLE separator here (not the audio|sub divider): pick "Ao no Hako" and still tag Ukrainian.
            var result = new TolokaTitleParser().Parse("Блакитна коробка | Ao no Hako (2024) WEB-DL 1080p Ukr/Jap", TvCategory, true);
            result.Should().Be("Ao no Hako (2024) WEB-DL 1080p Ukrainian");
        }

        [TestCase("FanVoxUA", ExpectedResult = "FanVoxUA")]
        // A transliterated multi-word Cyrillic name is also underscore-joined (no spaces in a release group).
        [TestCase("Сталь Кується", ExpectedResult = "Stal_Kuietsia")]
        // Anonymous uploads still get an explicit "Anonymous" group (both the Latin and Cyrillic markers).
        [TestCase("Anonymous", ExpectedResult = "Anonymous")]
        [TestCase("Анонім", ExpectedResult = "Anonymous")]
        [TestCase("", ExpectedResult = null)]
        // A multi-word handle has its spaces replaced with underscores (a release group token cannot contain spaces).
        [TestCase("Ukr Voice Team", ExpectedResult = "Ukr_Voice_Team")]
        [TestCase("Marco Polo", ExpectedResult = "Marco_Polo")]
        // ASCII handles keep their non-space separators/case verbatim so they match the community custom formats.
        [TestCase("HaKer_256", ExpectedResult = "HaKer_256")]
        [TestCase("Romario_O", ExpectedResult = "Romario_O")]
        [TestCase("Seto.Haruki", ExpectedResult = "Seto.Haruki")]
        [TestCase("Otaku-First", ExpectedResult = "Otaku-First")]
        [TestCase("Gwean_&_Maslinka", ExpectedResult = "Gwean_&_Maslinka")]
        // Latin handles with hidden Cyrillic homoglyphs: look-alike-normalized, NOT phonetically transliterated
        // ("х" would otherwise become "kh" -> "Alekh"; "а" stays "a").
        [TestCase("wаrden", ExpectedResult = "warden")]
        [TestCase("Аlех", ExpectedResult = "Alex")]
        // All-Cyrillic name is transliterated (НТН -> NTN, not the homoglyph garbage HTH).
        [TestCase("НТН", ExpectedResult = "NTN")]
        public string should_sanitize_release_group(string author)
        {
            return TolokaTitleParser.SanitizeReleaseGroup(author);
        }

        // A username carrying hidden Cyrillic look-alikes is normalized to Latin (so uploader search matches the real
        // account); a genuinely Cyrillic handle is left untouched for the caller to transliterate.
        [TestCase("wаrden", ExpectedResult = "warden")]
        [TestCase("Аlех", ExpectedResult = "Alex")]
        [TestCase("warden", ExpectedResult = "warden")]
        [TestCase("Гуртом", ExpectedResult = "Гуртом")]
        // All-Cyrillic acronym whose every letter has a Latin look-alike: must stay Cyrillic (it's a real word,
        // not a disguised Latin one), so it transliterates to "NTN" downstream rather than the garbage "HTH".
        [TestCase("НТН", ExpectedResult = "НТН")]
        public string should_normalize_name_homoglyphs(string name)
        {
            return TolokaTitleParser.NormalizeNameHomoglyphs(name);
        }

        [Test]
        public void should_append_release_group_and_zero_pad_for_sonarr()
        {
            var parser = new TolokaTitleParser();
            var group = TolokaTitleParser.SanitizeReleaseGroup("FanVoxUA");

            var result = parser.Parse(
                "Магічна битва (Сезон 3) / Jujutsu Kaisen (Season 3) (2026) WEBDLRip 1080p H.265",
                TvCategory,
                true,
                group);

            result.Should().Contain("S03");
            result.Should().EndWith("-FanVoxUA");
        }

        [Test]
        public void should_retain_season_for_anime_season_pack()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Магічна битва (Сезон 3) / Jujutsu Kaisen (Season 3) (2026) WEBDLRip 1080p H.265 Ukr/Jap | sub Ukr",
                TvCategory,
                true);

            result.Should().Be("Jujutsu Kaisen S03 (2026) WEBRip 1080p x265 Ukrainian");
        }

        [Test]
        public void should_normalize_webdlrip_and_strip_cyrillic_for_movie()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Дюна / Dune (2021) BDRip 1080p H.264 Ukr/Eng | Sub Ukr",
                MovieCategory,
                true);

            result.Should().Be("Dune (2021) BluRay 1080p x264 Ukrainian");
        }

        [Test]
        public void should_normalize_cyrillic_homoglyphs_in_tech_tokens()
        {
            var parser = new TolokaTitleParser();

            // "BDRір" (Cyrillic і/р) and "1080р" (Cyrillic р) must be normalised so the quality survives.
            var result = parser.Parse(
                "Дюна / Dune (2021) BDRір 1080р H.264 Ukr/Eng",
                MovieCategory,
                true);

            result.Should().Be("Dune (2021) BluRay 1080p x264 Ukrainian");
        }

        [Test]
        public void should_preserve_edition_tag()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Джон Кеннеді / JFK [Director's Cut] (1991) BDRip H.264 Ukr/Eng",
                MovieCategory,
                true);

            result.Should().Be("JFK (1991) BluRay x264 Director's Cut Ukrainian");
        }

        [Test]
        public void should_preserve_proper_tag()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Дюна / Dune (2021) PROPER BDRip 1080p H.264 Ukr",
                MovieCategory,
                true);

            result.Should().Be("Dune (2021) BluRay 1080p x264 PROPER Ukrainian");
        }

        [Test]
        public void should_emit_dashed_season_range_for_multi_season_pack()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Воїн / Warrior (Season 1-3) (2019-2023) WEB-DL AVC",
                TvCategory,
                true);

            result.Should().Be("Warrior S01-S03 (2019-2023) WEB-DL x264");
        }

        [Test]
        public void should_collapse_disjoint_episode_ranges_to_envelope()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Бліч / Bleach (серії 110-127, 138-167, 190-203, 215-226, 266-286, 288-293 з 366) (2004-2012) BDRip 1080p",
                TvCategory,
                true);

            result.Should().Be("Bleach E110-E293 (2004-2012) BluRay 1080p");
        }

        [Test]
        public void should_preserve_part_marker()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Життя з нуля / Re:Zero (Сезон 4, частина 1) (2026) WEB-DL 1080p",
                TvCategory,
                true);

            result.Should().Be("Re:Zero S04 (2026) WEB-DL 1080p Part 1");
        }

        [Test]
        public void should_pick_segment_with_most_latin_not_leftover_fragment()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Побачення з життям IV (Сезон 4) / Date a Live IV (Season 4) (2022) BDRip 1080р H.265 2xUkr/Jap | Sub Ukr/Eng",
                TvCategory,
                true);

            result.Should().Be("Date a Live IV S04 (2022) BluRay 1080p x265 Ukrainian");
        }

        [Test]
        public void should_not_duplicate_part_marker_present_in_series_name()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Дюна: Частина 2 / Dune: Part 2 (2024) BDRip 1080p Ukr/Eng",
                MovieCategory,
                true);

            result.Should().Be("Dune: Part 2 (2024) BluRay 1080p Ukrainian");
        }

        [Test]
        public void should_keep_cyrillic_only_title_instead_of_quality_salad()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Замість тисячі слів - Збірка коротких метрів (2014-2021) WEBDLRip 1080p H.264 Jap | Sub Ukr",
                TvCategory,
                true);

            result.Should().Be("Замість тисячі слів - Збірка коротких метрів (2014-2021) WEBRip 1080p x264 Japanese");
        }

        [Test]
        public void should_not_read_title_word_as_audio_language()
        {
            var parser = new TolokaTitleParser();

            // "Dan" in the title must not be read as Danish; only post-year tokens count, so audio "Eng" -> English.
            var result = parser.Parse(
                "Танець з Деном / Dance with Dan (2024) BDRip 1080p Eng | Sub Ukr",
                MovieCategory,
                true);

            result.Should().Be("Dance with Dan (2024) BluRay 1080p English");
        }

        [Test]
        public void should_resolve_multi_digit_dub_prefix_to_ukrainian()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Пеклорай / Jigokuraku (2023) BDRip 1080p H.265 10xUkr/Jap | sub Eng",
                MovieCategory,
                true);

            result.Should().Be("Jigokuraku (2023) BluRay 1080p x265 Ukrainian");
        }

        [Test]
        public void should_preserve_non_standard_resolution_token()
        {
            var parser = new TolokaTitleParser();
            var result = parser.Parse("Клеопатра / Cleopatra (1999) DVDRemux 540p Ukr/Eng | Sub Eng", MovieCategory, true);
            result.Should().Be("Cleopatra (1999) DVD Remux 540p Ukrainian");
        }

        [Test]
        public void should_preserve_hybrid_uhd_hdr_sources()
        {
            var parser = new TolokaTitleParser();
            var result = parser.Parse("Яйце / Tenshi no Tamago (1985) UHD-BDRip 1080p HDR H.265 2xUkr/Jpn | Sub Jpn", MovieCategory, true);
            result.Should().Be("Tenshi no Tamago (1985) UHD BluRay 1080p HDR x265 Ukrainian");
        }

        [Test]
        public void should_preserve_ai_remaster_edition()
        {
            var parser = new TolokaTitleParser();
            var result = parser.Parse("Мудрець / Rakudai Kenja (Season 1) (2026) WEBRip Ai Rem 1080p H.265 Ukr/Jap | Sub Ukr", TvCategory, true);
            result.Should().Be("Rakudai Kenja S01 (2026) WEBRip 1080p x265 AI Remastered Ukrainian");
        }

        [Test]
        public void should_reconstruct_multi_year_collection_pack()
        {
            var parser = new TolokaTitleParser();
            var result = parser.Parse("Планета Мавп. Колекція / Planet of the Apes. Collection (1968,1970,2001) BDRip-AVC Ukr/Eng | Sub Ukr/Eng", MovieCategory, true);
            result.Should().Be("Planet of the Apes (1968-2001) BluRay x264 Ukrainian");
        }

        [Test]
        public void should_not_double_numeric_only_title()
        {
            var parser = new TolokaTitleParser();
            var result = parser.Parse("9-1-1 (Служба порятунку) (Сезони 1-8) / 9-1-1 (Season 1-8) (2018-2025) WEB-DLRip-AVC Ukr/Eng | Sub Eng", TvCategory, true);
            result.Should().Be("9-1-1 S01-S08 (2018-2025) WEBRip x264 Ukrainian");
        }

        [Test]
        public void should_keep_all_seasons_in_comma_list()
        {
            var parser = new TolokaTitleParser();
            var result = parser.Parse("Дурні тести / Baka to Test (Season 1, 2) (2010-2011) BDRip 1080p H.265 Ukr/Jap | Sub Ukr", TvCategory, true);
            result.Should().Be("Baka to Test S01-S02 (2010-2011) BluRay 1080p x265 Ukrainian");
        }

        [Test]
        public void should_treat_episode_count_as_range_not_single()
        {
            var parser = new TolokaTitleParser();
            var result = parser.Parse("Магічна битва (Сезон 3, серій 12 з 23) / Jujutsu Kaisen (Season 3, episodes 12 of 23) (2026) WEB-DL 1080p Ukr/Jap | Sub Ukr", TvCategory, true);
            result.Should().Be("Jujutsu Kaisen S03E01-E12 (2026) WEB-DL 1080p Ukrainian");
        }

        [Test]
        public void should_not_leave_stray_bracket_from_nested_parens()
        {
            var parser = new TolokaTitleParser();
            var result = parser.Parse("Зла наука (Сезон 1 (серії 1-8, 11-26), Сезон 2) / Wicked Science (Season 1 (episodes 1-8, 11-26), Season 2) (2004-2006) DVDRip-AVC Ukr/Eng | sub Eng", TvCategory, true);

            // The two-season pack (season 1 partial + season 2) collapses to the cross-season envelope S01-S02.
            result.Should().Be("Wicked Science S01-S02 (2004-2006) DVDRip x264 Ukrainian");
        }

        [Test]
        public void should_strip_collection_descriptor_from_name()
        {
            var parser = new TolokaTitleParser();
            var result = parser.Parse("Пуститися берега. Всі сезони + фільм / Breaking Bad. All seasons + movie (2008-2013/2019) BDRip-AVC Ukr/Eng | Sub Ukr/Eng", TvCategory, true);
            result.Should().Be("Breaking Bad (2008-2019) BluRay x264 Ukrainian");
        }

        [Test]
        public void should_preserve_exact_disjoint_episodes_when_enabled()
        {
            var parser = new TolokaTitleParser();
            var result = parser.Parse(
                "Бліч / Bleach (серії 110-127, 138-167, 190-203, 215-226, 266-286, 288-293 з 366) (2004-2012) BDRip 1080p",
                TvCategory, true, null, null, exactRanges: true);
            result.Should().Be("Bleach E110-E127, E138-E167, E190-E203, E215-E226, E266-E286, E288-E293 (2004-2012) BluRay 1080p");
        }

        [Test]
        public void should_collapse_disjoint_episodes_in_range_mode()
        {
            var parser = new TolokaTitleParser();
            var result = parser.Parse(
                "Бліч / Bleach (серії 110-127, 138-167, 190-203, 215-226, 266-286, 288-293 з 366) (2004-2012) BDRip 1080p",
                TvCategory, true, null, null, exactRanges: false);
            result.Should().Be("Bleach E110-E293 (2004-2012) BluRay 1080p");
        }

        [Test]
        public void should_preserve_exact_disjoint_seasons_when_enabled()
        {
            var parser = new TolokaTitleParser();
            var result = parser.Parse(
                "Молодий Морс / Endeavour (Сезони 1-6, 8-9) (2012-2023) BDRip-AVC Ukr/Eng | Sub Ukr",
                TvCategory, true, null, true, exactRanges: true);
            result.Should().Be("Endeavour S01-S06, S08-S09 (2012-2023) BluRay x264 Ukrainian");
        }

        [Test]
        public void should_preserve_exact_disjoint_episodes_within_season_when_enabled()
        {
            var parser = new TolokaTitleParser();
            var result = parser.Parse(
                "Зла наука / Wicked Science (Сезон 1, серії 1-8, 11-26) (2004) DVDRip Ukr/Eng | Sub Eng",
                TvCategory, true, null, true, exactRanges: true);
            result.Should().Be("Wicked Science S01E01-E08, S01E11-E26 (2004) DVDRip Ukrainian");
        }

        [Test]
        public void should_keep_envelope_when_contiguous_even_in_exact_mode()
        {
            var parser = new TolokaTitleParser();
            var result = parser.Parse(
                "Бліч / Bleach (серії 1-12, 13-24) (2004) BDRip 1080p",
                TvCategory, true, null, null, exactRanges: true);
            result.Should().Be("Bleach E01-E24 (2004) BluRay 1080p");
        }

        [Test]
        public void should_tag_ukrainian_from_forum_convention_when_no_audio_token()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Коли я переродився слизом / Tensei shitara Slime Datta Ken (Season 4) (2026) WEB-DL 1080p",
                TvCategory,
                true,
                null,
                ukrainianAudioDefault: true);

            result.Should().EndWith("Ukrainian");
        }

        [Test]
        public void should_not_tag_ukrainian_for_subtitle_forum_without_audio_token()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Коли я переродився слизом / Tensei shitara Slime Datta Ken (Season 4) (2026) WEB-DL 1080p",
                TvCategory,
                true,
                null,
                ukrainianAudioDefault: false);

            result.Should().NotContain("Ukrainian");
        }

        [Test]
        public void should_tag_ukrainian_when_ukr_audio_with_english_subs()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Дюна / Dune (2024) WEB-DL 1080p Ukr/Jap | Sub Eng",
                MovieCategory,
                true,
                null,
                ukrainianAudioDefault: true);

            result.Should().Be("Dune (2024) WEB-DL 1080p Ukrainian");
        }

        [Test]
        public void should_emit_season_range_for_ukrainian_plural_seasons()
        {
            var parser = new TolokaTitleParser();

            var result = parser.Parse(
                "Офіс / The Office (Сезони 1-9) (2005-2013) WEB-DL 1080p Ukr/Eng | Sub Eng",
                TvCategory,
                true);

            result.Should().Be("The Office S01-S09 (2005-2013) WEB-DL 1080p Ukrainian");
        }

        [Test]
        public void should_recover_resolution_from_details_frame_size()
        {
            const string html = @"
<html><body>
    <a class=""maintitle"" href=""t1"">Sample (1996)</a>
    <span>Відео: кодек: H.264 розмір кадру: 1024 х 576 бітрейт: 1800 кб/с</span>
</body></html>";

            var meta = TolokaParser.ParseDetailsPage(html);

            meta.Resolution.Should().Be("576p");
        }

        [Test]
        public void should_parse_details_page_metadata()
        {
            const string html = @"
<html>
<head>
    <link rel=""image_src"" href=""https://thumb.hurtom.com/image/w250/toloka.to/photos/sample_f0_0.jpg"" />
</head>
<body>
    <a class=""maintitle"" href=""t693540"">Sample Movie (2026)</a>
    <a href=""https://www.imdb.com/title/tt12343534/"">IMDb</a>
    <a href=""magnet:?xt=urn:btih:6f93077a2377edf06d67654abc1e1057c12e19d7&amp;dn=Sample"">magnet</a>
    <a href=""download.php?id=708258"">Завантажити</a>
</body>
</html>";

            var meta = TolokaParser.ParseDetailsPage(html);

            meta.ImdbId.Should().Be(12343534);
            meta.InfoHash.Should().Be("6f93077a2377edf06d67654abc1e1057c12e19d7");
            meta.MagnetUrl.Should().StartWith("magnet:?xt=urn:btih:6f93077a");
            meta.PosterUrl.Should().Be("https://thumb.hurtom.com/image/w250/toloka.to/photos/sample_f0_0.jpg");
        }

        [Test]
        public void should_return_empty_details_when_nothing_present()
        {
            var meta = TolokaParser.ParseDetailsPage("<html><body>nothing</body></html>");

            meta.ImdbId.Should().BeNull();
            meta.InfoHash.Should().BeNull();
            meta.MagnetUrl.Should().BeNull();
            meta.PosterUrl.Should().BeNull();
        }

        [Test]
        public void should_parse_grab_counts_from_api_json()
        {
            // The api.php search returns the completed/grabs count the HTML page hides ("complete").
            const string json = @"[
  { ""id"": ""695553"", ""title"": ""Slime S4"", ""seeders"": ""22"", ""complete"": ""117"" },
  { ""id"": ""678039"", ""title"": ""Slime S3"", ""seeders"": ""20"", ""complete"": ""1245"" }
]";
            var grabs = TolokaParser.ParseGrabCounts(json);

            grabs["695553"].Should().Be(117);
            grabs["678039"].Should().Be(1245);
        }

        [Test]
        public void should_return_no_grab_counts_for_non_json_body()
        {
            // The api returns plain text on error/empty - must yield no counts rather than throw.
            TolokaParser.ParseGrabCounts("").Should().BeEmpty();
            TolokaParser.ParseGrabCounts("Nothing found").Should().BeEmpty();
        }
    }
}
