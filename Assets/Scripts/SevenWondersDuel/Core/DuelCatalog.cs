using System;
using System.Collections.Generic;
using System.Linq;

namespace SevenWondersDuel.Core
{
    public class DuelCatalog
    {
        public readonly List<CardDefinition> Cards = new List<CardDefinition>();
        public readonly List<WonderDefinition> Wonders = new List<WonderDefinition>();
        public readonly List<ProgressTokenDefinition> ProgressTokens = new List<ProgressTokenDefinition>();
        public readonly Dictionary<int, List<BoardSlotDefinition>> BoardLayouts = new Dictionary<int, List<BoardSlotDefinition>>();

        public readonly Dictionary<string, CardDefinition> CardsById = new Dictionary<string, CardDefinition>();
        public readonly Dictionary<string, WonderDefinition> WondersById = new Dictionary<string, WonderDefinition>();
        public readonly Dictionary<string, ProgressTokenDefinition> ProgressById = new Dictionary<string, ProgressTokenDefinition>();

        public void RebuildLookups()
        {
            CardsById.Clear();
            WondersById.Clear();
            ProgressById.Clear();

            foreach (var card in Cards)
            {
                CardsById[card.Id] = card;
            }

            foreach (var wonder in Wonders)
            {
                WondersById[wonder.Id] = wonder;
            }

            foreach (var token in ProgressTokens)
            {
                ProgressById[token.Id] = token;
            }
        }

        public static DuelCatalog CreateDefault()
        {
            var catalog = new DuelCatalog();
            AddCards(catalog);
            AddWonders(catalog);
            AddProgressTokens(catalog);
            AddBoardLayouts(catalog);
            catalog.RebuildLookups();
            return catalog;
        }

        private static void AddCards(DuelCatalog catalog)
        {
            AddCard(catalog, 1, "age1_lumber_yard", "Lumber Yard", CardColor.RawMaterial, Cost.Free(), "", Effect.ResourceProduction(ResourceType.Wood));
            AddCard(catalog, 1, "age1_logging_camp", "Logging Camp", CardColor.RawMaterial, C(1), "", Effect.ResourceProduction(ResourceType.Wood));
            AddCard(catalog, 1, "age1_clay_pool", "Clay Pool", CardColor.RawMaterial, Cost.Free(), "", Effect.ResourceProduction(ResourceType.Clay));
            AddCard(catalog, 1, "age1_clay_pit", "Clay Pit", CardColor.RawMaterial, C(1), "", Effect.ResourceProduction(ResourceType.Clay));
            AddCard(catalog, 1, "age1_quarry", "Quarry", CardColor.RawMaterial, Cost.Free(), "", Effect.ResourceProduction(ResourceType.Stone));
            AddCard(catalog, 1, "age1_stone_pit", "Stone Pit", CardColor.RawMaterial, C(1), "", Effect.ResourceProduction(ResourceType.Stone));
            AddCard(catalog, 1, "age1_glassworks", "Glassworks", CardColor.Manufactured, C(1), "", Effect.ResourceProduction(ResourceType.Glass));
            AddCard(catalog, 1, "age1_press", "Press", CardColor.Manufactured, C(1), "", Effect.ResourceProduction(ResourceType.Papyrus));
            AddCard(catalog, 1, "age1_guard_tower", "Guard Tower", CardColor.Military, Cost.Free(), "", Effect.Military(1));
            AddCard(catalog, 1, "age1_stable", "Stable", CardColor.Military, C(0, 1), "horse", Effect.Military(1));
            AddCard(catalog, 1, "age1_garrison", "Garrison", CardColor.Military, C(0, 0, 1), "sword", Effect.Military(1));
            AddCard(catalog, 1, "age1_palisade", "Palisade", CardColor.Military, C(2), "tower", Effect.Military(1));
            AddCard(catalog, 1, "age1_workshop", "Workshop", CardColor.Science, C(0, 0, 0, 0, 0, 1), "gear", Effect.ScienceSymbol(ScienceSymbol.Compass), Effect.Points(1));
            AddCard(catalog, 1, "age1_apothecary", "Apothecary", CardColor.Science, C(0, 0, 0, 0, 1), "wheel", Effect.ScienceSymbol(ScienceSymbol.Wheel), Effect.Points(1));
            AddCard(catalog, 1, "age1_scriptorium", "Scriptorium", CardColor.Science, C(2), "book", Effect.ScienceSymbol(ScienceSymbol.Quill));
            AddCard(catalog, 1, "age1_pharmacist", "Pharmacist", CardColor.Science, C(2), "mortar", Effect.ScienceSymbol(ScienceSymbol.Mortar));
            AddCard(catalog, 1, "age1_theater", "Theater", CardColor.Civilian, Cost.Free(), "mask", Effect.Points(3));
            AddCard(catalog, 1, "age1_altar", "Altar", CardColor.Civilian, Cost.Free(), "moon", Effect.Points(3));
            AddCard(catalog, 1, "age1_baths", "Baths", CardColor.Civilian, C(0, 0, 0, 1), "water", Effect.Points(3));
            AddCard(catalog, 1, "age1_tavern", "Tavern", CardColor.Commercial, Cost.Free(), "barrel", Effect.Coins(4));
            AddCard(catalog, 1, "age1_stone_reserve", "Stone Reserve", CardColor.Commercial, C(3), "", Effect.Simple(EffectKind.DiscountResource, 0, ResourceType.Stone));
            AddCard(catalog, 1, "age1_clay_reserve", "Clay Reserve", CardColor.Commercial, C(3), "", Effect.Simple(EffectKind.DiscountResource, 0, ResourceType.Clay));
            AddCard(catalog, 1, "age1_wood_reserve", "Wood Reserve", CardColor.Commercial, C(3), "", Effect.Simple(EffectKind.DiscountResource, 0, ResourceType.Wood));

            AddCard(catalog, 2, "age2_sawmill", "Sawmill", CardColor.RawMaterial, C(2), "", Effect.ResourceProduction(ResourceType.Wood, 2));
            AddCard(catalog, 2, "age2_brickyard", "Brickyard", CardColor.RawMaterial, C(2), "", Effect.ResourceProduction(ResourceType.Clay, 2));
            AddCard(catalog, 2, "age2_shelf_quarry", "Shelf Quarry", CardColor.RawMaterial, C(2), "", Effect.ResourceProduction(ResourceType.Stone, 2));
            AddCard(catalog, 2, "age2_glass_blower", "Glass-Blower", CardColor.Manufactured, Cost.Free(), "", Effect.ResourceProduction(ResourceType.Glass));
            AddCard(catalog, 2, "age2_drying_room", "Drying Room", CardColor.Manufactured, Cost.Free(), "", Effect.ResourceProduction(ResourceType.Papyrus));
            AddCard(catalog, 2, "age2_walls", "Walls", CardColor.Military, C(0, 0, 0, 2), "", Effect.Military(2));
            AddCard(catalog, 2, "age2_horse_breeders", "Horse Breeders", CardColor.Military, C(0, 1, 1, 0, 0, 0, "horse"), "", Effect.Military(1));
            AddCard(catalog, 2, "age2_barracks", "Barracks", CardColor.Military, C(3, 0, 0, 0, 0, 0, "sword"), "", Effect.Military(1));
            AddCard(catalog, 2, "age2_archery_range", "Archery Range", CardColor.Military, C(0, 1, 0, 1, 0, 1), "target", Effect.Military(2));
            AddCard(catalog, 2, "age2_parade_ground", "Parade Ground", CardColor.Military, C(0, 0, 2, 0, 1), "helmet", Effect.Military(2));
            AddCard(catalog, 2, "age2_library", "Library", CardColor.Science, C(0, 1, 0, 1, 1), "lyre", Effect.ScienceSymbol(ScienceSymbol.Quill), Effect.Points(2));
            AddCard(catalog, 2, "age2_dispensary", "Dispensary", CardColor.Science, C(0, 0, 2, 1), "observatory", Effect.ScienceSymbol(ScienceSymbol.Mortar), Effect.Points(2));
            AddCard(catalog, 2, "age2_school", "School", CardColor.Science, C(0, 1, 0, 0, 0, 2), "", Effect.ScienceSymbol(ScienceSymbol.Wheel), Effect.Points(1));
            AddCard(catalog, 2, "age2_laboratory", "Laboratory", CardColor.Science, C(0, 1, 0, 0, 2), "", Effect.ScienceSymbol(ScienceSymbol.Compass), Effect.Points(1));
            AddCard(catalog, 2, "age2_courthouse", "Courthouse", CardColor.Civilian, C(0, 2, 0, 0, 1), "", Effect.Points(5));
            AddCard(catalog, 2, "age2_statue", "Statue", CardColor.Civilian, C(0, 0, 2), "mask", Effect.Points(4));
            AddCard(catalog, 2, "age2_temple", "Temple", CardColor.Civilian, C(0, 1, 0, 0, 0, 1, "moon"), "lamp", Effect.Points(4));
            AddCard(catalog, 2, "age2_aqueduct", "Aqueduct", CardColor.Civilian, C(0, 0, 0, 3, 0, 0, "water"), "", Effect.Points(5));
            AddCard(catalog, 2, "age2_rostrum", "Rostrum", CardColor.Civilian, C(0, 1, 0, 1), "forum", Effect.Points(4));
            AddCard(catalog, 2, "age2_forum", "Forum", CardColor.Commercial, C(3), "", Effect.ManufacturedChoice());
            AddCard(catalog, 2, "age2_caravansery", "Caravansery", CardColor.Commercial, C(2, 0, 0, 0, 1, 1), "", Effect.RawChoice());
            AddCard(catalog, 2, "age2_customs_house", "Customs House", CardColor.Commercial, C(4), "", Effect.Simple(EffectKind.DiscountResource, 0, ResourceType.Glass), Effect.Simple(EffectKind.DiscountResource, 0, ResourceType.Papyrus));
            AddCard(catalog, 2, "age2_brewery", "Brewery", CardColor.Commercial, Cost.Free(), "arena", Effect.Coins(6));

            AddCard(catalog, 3, "age3_arsenal", "Arsenal", CardColor.Military, C(0, 2, 3), "", Effect.Military(3));
            AddCard(catalog, 3, "age3_pretorium", "Pretorium", CardColor.Military, C(8), "", Effect.Military(3));
            AddCard(catalog, 3, "age3_fortifications", "Fortifications", CardColor.Military, C(0, 0, 1, 2, 0, 1, "tower"), "", Effect.Military(2));
            AddCard(catalog, 3, "age3_siege_workshop", "Siege Workshop", CardColor.Military, C(0, 3, 0, 0, 1, 0, "target"), "", Effect.Military(2));
            AddCard(catalog, 3, "age3_circus", "Circus", CardColor.Military, C(0, 0, 2, 2, 0, 0, "helmet"), "", Effect.Military(2));
            AddCard(catalog, 3, "age3_academy", "Academy", CardColor.Science, C(0, 1, 0, 1, 1), "", Effect.ScienceSymbol(ScienceSymbol.Globe), Effect.Points(3));
            AddCard(catalog, 3, "age3_study", "Study", CardColor.Science, C(0, 2, 0, 0, 0, 1), "", Effect.ScienceSymbol(ScienceSymbol.Sundial), Effect.Points(3));
            AddCard(catalog, 3, "age3_university", "University", CardColor.Science, C(0, 1, 0, 0, 1, 1, "lyre"), "", Effect.ScienceSymbol(ScienceSymbol.Globe), Effect.Points(2));
            AddCard(catalog, 3, "age3_observatory", "Observatory", CardColor.Science, C(0, 0, 0, 1, 0, 2, "observatory"), "", Effect.ScienceSymbol(ScienceSymbol.Sundial), Effect.Points(2));
            AddCard(catalog, 3, "age3_palace", "Palace", CardColor.Civilian, C(0, 1, 1, 0, 2), "", Effect.Points(7));
            AddCard(catalog, 3, "age3_town_hall", "Town Hall", CardColor.Civilian, C(0, 2, 0, 3), "", Effect.Points(7));
            AddCard(catalog, 3, "age3_obelisk", "Obelisk", CardColor.Civilian, C(0, 0, 0, 2, 1), "", Effect.Points(5));
            AddCard(catalog, 3, "age3_gardens", "Gardens", CardColor.Civilian, C(0, 2, 2, 0, 0, 0, "mask"), "", Effect.Points(6));
            AddCard(catalog, 3, "age3_pantheon", "Pantheon", CardColor.Civilian, C(0, 1, 1, 0, 0, 2, "lamp"), "", Effect.Points(6));
            AddCard(catalog, 3, "age3_senate", "Senate", CardColor.Civilian, C(0, 0, 2, 1, 0, 1, "forum"), "", Effect.Points(5));
            AddCard(catalog, 3, "age3_chamber_of_commerce", "Chamber of Commerce", CardColor.Commercial, C(0, 0, 0, 0, 0, 2), "", Effect.Simple(EffectKind.CoinsPerManufactured, 3), Effect.Points(3));
            AddCard(catalog, 3, "age3_port", "Port", CardColor.Commercial, C(0, 1, 0, 0, 1, 1), "", Effect.Simple(EffectKind.CoinsPerRaw, 2), Effect.Points(3));
            AddCard(catalog, 3, "age3_armory", "Armory", CardColor.Commercial, C(0, 0, 0, 2, 1), "", Effect.Simple(EffectKind.CoinsPerMilitary, 1), Effect.Points(3));
            AddCard(catalog, 3, "age3_lighthouse", "Lighthouse", CardColor.Commercial, C(0, 0, 2, 0, 1), "", Effect.Simple(EffectKind.CoinsPerCommercial, 1), Effect.Points(3));
            AddCard(catalog, 3, "age3_arena", "Arena", CardColor.Commercial, C(0, 1, 1, 1, 0, 0, "arena"), "", Effect.Simple(EffectKind.CoinsPerWonder, 2), Effect.Points(3));

            AddGuild(catalog, "guild_merchants", "Merchants Guild", C(0, 1, 1, 0, 1, 1), Effect.Simple(EffectKind.CoinsPerMostCommercial, 1), Effect.Simple(EffectKind.PointsPerMostCommercial, 1));
            AddGuild(catalog, "guild_shipowners", "Shipowners Guild", C(0, 0, 1, 1, 1, 1), Effect.Simple(EffectKind.CoinsPerMostRawAndManufactured, 1), Effect.Simple(EffectKind.PointsPerMostRawAndManufactured, 1));
            AddGuild(catalog, "guild_builders", "Builders Guild", C(0, 1, 1, 2, 1), Effect.Simple(EffectKind.PointsPerMostWonder, 2));
            AddGuild(catalog, "guild_magistrates", "Magistrates Guild", C(0, 2, 1, 0, 0, 1), Effect.Simple(EffectKind.CoinsPerMostCivilian, 1), Effect.Simple(EffectKind.PointsPerMostCivilian, 1));
            AddGuild(catalog, "guild_scientists", "Scientists Guild", C(0, 2, 2), Effect.Simple(EffectKind.CoinsPerMostScience, 1), Effect.Simple(EffectKind.PointsPerMostScience, 1));
            AddGuild(catalog, "guild_moneylenders", "Moneylenders Guild", C(0, 2, 0, 2), Effect.Simple(EffectKind.CoinsPerRichestThreeCoins, 1), Effect.Simple(EffectKind.PointsPerRichestThreeCoins, 1));
            AddGuild(catalog, "guild_tacticians", "Tacticians Guild", C(0, 0, 1, 2, 0, 1), Effect.Simple(EffectKind.CoinsPerMostMilitary, 1), Effect.Simple(EffectKind.PointsPerMostMilitary, 1));
        }

        private static void AddWonders(DuelCatalog catalog)
        {
            AddWonder(catalog, "wonder_appian_way", "The Appian Way", C(0, 0, 2, 2), Effect.Coins(3), Effect.Simple(EffectKind.OpponentLoseCoins, 3), Effect.Simple(EffectKind.RepeatTurn), Effect.Points(3));
            AddWonder(catalog, "wonder_circus_maximus", "Circus Maximus", C(0, 1, 0, 1, 1), Effect.Simple(EffectKind.OpponentDiscardManufactured), Effect.Military(1), Effect.Points(3));
            AddWonder(catalog, "wonder_colossus", "The Colossus", C(0, 0, 3, 0, 1), Effect.Military(2), Effect.Points(3));
            AddWonder(catalog, "wonder_great_library", "The Great Library", C(0, 3, 0, 0, 1, 1), Effect.Simple(EffectKind.ProgressFromRemoved), Effect.Points(4));
            AddWonder(catalog, "wonder_great_lighthouse", "The Great Lighthouse", C(0, 0, 1, 1, 0, 1), Effect.RawChoice(), Effect.Points(4));
            AddWonder(catalog, "wonder_hanging_gardens", "The Hanging Gardens", C(0, 1, 0, 0, 1, 1), Effect.Coins(6), Effect.Simple(EffectKind.RepeatTurn), Effect.Points(3));
            AddWonder(catalog, "wonder_mausoleum", "The Mausoleum", C(0, 0, 2, 0, 2, 1), Effect.Simple(EffectKind.BuildFromDiscard), Effect.Points(2));
            AddWonder(catalog, "wonder_piraeus", "Piraeus", C(0, 2, 1, 1), Effect.ManufacturedChoice(), Effect.Simple(EffectKind.RepeatTurn), Effect.Points(2));
            AddWonder(catalog, "wonder_pyramids", "The Pyramids", C(0, 0, 0, 3), Effect.Points(9));
            AddWonder(catalog, "wonder_sphinx", "The Sphinx", C(0, 0, 1, 1, 2), Effect.Simple(EffectKind.RepeatTurn), Effect.Points(6));
            AddWonder(catalog, "wonder_statue_zeus", "The Statue of Zeus", C(0, 1, 1, 1, 0, 1), Effect.Simple(EffectKind.OpponentDiscardRaw), Effect.Military(1), Effect.Points(3));
            AddWonder(catalog, "wonder_temple_artemis", "The Temple of Artemis", C(0, 1, 0, 1, 1, 1), Effect.Coins(12), Effect.Simple(EffectKind.RepeatTurn));
        }

        private static void AddProgressTokens(DuelCatalog catalog)
        {
            AddProgress(catalog, "progress_agriculture", "Agriculture", Effect.Coins(6), Effect.Points(4));
            AddProgress(catalog, "progress_architecture", "Architecture", Effect.Simple(EffectKind.Architecture));
            AddProgress(catalog, "progress_economy", "Economy", Effect.Simple(EffectKind.Economy));
            AddProgress(catalog, "progress_law", "Law", Effect.ScienceSymbol(ScienceSymbol.Law));
            AddProgress(catalog, "progress_masonry", "Masonry", Effect.Simple(EffectKind.Masonry));
            AddProgress(catalog, "progress_mathematics", "Mathematics", Effect.Simple(EffectKind.PointsPerProgress, 3));
            AddProgress(catalog, "progress_philosophy", "Philosophy", Effect.Points(7));
            AddProgress(catalog, "progress_strategy", "Strategy", Effect.Simple(EffectKind.Strategy));
            AddProgress(catalog, "progress_theology", "Theology", Effect.Simple(EffectKind.Theology));
            AddProgress(catalog, "progress_urbanism", "Urbanism", Effect.Coins(6), Effect.Simple(EffectKind.Urbanism));
        }

        private static void AddBoardLayouts(DuelCatalog catalog)
        {
            catalog.BoardLayouts[1] = PyramidLayout(
                new[] { 6, 5, 4, 3, 2 },
                new[] { true, false, true, false, true });

            catalog.BoardLayouts[2] = PyramidLayout(
                new[] { 2, 3, 4, 5, 6 },
                new[] { true, false, true, false, true });

            catalog.BoardLayouts[3] = PyramidLayout(
                new[] { 4, 5, 4, 5, 2 },
                new[] { true, false, false, true, true });
        }

        private static List<BoardSlotDefinition> PyramidLayout(int[] rowSizes, bool[] faceUpRows)
        {
            var rows = new List<List<int>>();
            var slots = new List<BoardSlotDefinition>();
            var nextId = 0;
            var max = rowSizes.Max();

            for (var row = 0; row < rowSizes.Length; row++)
            {
                rows.Add(new List<int>());
                var count = rowSizes[row];
                for (var i = 0; i < count; i++)
                {
                    var centeredX = i - ((count - 1) * 0.5f);
                    var slot = new BoardSlotDefinition
                    {
                        SlotId = nextId,
                        X = centeredX + (max - count) * 0.05f,
                        Y = row,
                        FaceUpAtStart = faceUpRows[Math.Min(row, faceUpRows.Length - 1)]
                    };

                    if (row > 0)
                    {
                        var previous = rows[row - 1];
                        var left = Math.Min(i, previous.Count - 1);
                        var right = Math.Min(i + 1, previous.Count - 1);
                        slot.CoveredBy.Add(previous[left]);
                        if (right != left)
                        {
                            slot.CoveredBy.Add(previous[right]);
                        }
                    }

                    rows[row].Add(nextId);
                    slots.Add(slot);
                    nextId++;
                }
            }

            return slots;
        }

        private static Cost C(int coins = 0, int wood = 0, int clay = 0, int stone = 0, int glass = 0, int papyrus = 0, string freeWith = "")
        {
            return new Cost(coins, new ResourceBundle(wood, clay, stone, glass, papyrus), freeWith);
        }

        private static void AddCard(DuelCatalog catalog, int age, string id, string name, CardColor color, Cost cost, string link, params Effect[] effects)
        {
            catalog.Cards.Add(new CardDefinition
            {
                Id = id,
                Name = name,
                Age = age,
                Color = color,
                Cost = cost,
                LinkProvided = link,
                Effects = effects.ToList()
            });
        }

        private static void AddGuild(DuelCatalog catalog, string id, string name, Cost cost, params Effect[] effects)
        {
            AddCard(catalog, 3, id, name, CardColor.Guild, cost, "", effects);
            catalog.Cards[catalog.Cards.Count - 1].IsGuild = true;
        }

        private static void AddWonder(DuelCatalog catalog, string id, string name, Cost cost, params Effect[] effects)
        {
            catalog.Wonders.Add(new WonderDefinition
            {
                Id = id,
                Name = name,
                Cost = cost,
                Effects = effects.ToList()
            });
        }

        private static void AddProgress(DuelCatalog catalog, string id, string name, params Effect[] effects)
        {
            catalog.ProgressTokens.Add(new ProgressTokenDefinition
            {
                Id = id,
                Name = name,
                Effects = effects.ToList()
            });
        }
    }
}
