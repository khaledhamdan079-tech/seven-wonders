using System;
using System.Collections.Generic;
using System.Linq;

namespace SevenWondersDuel.Core
{
    public static class DuelReducer
    {
        private static readonly int[] DraftOrder = { 0, 1, 1, 0, 1, 0, 0, 1 };

        public static GameState CreateNewGame(int seed, string playerOne, string playerTwo, DuelCatalog catalog)
        {
            var state = new GameState
            {
                Seed = seed,
                FirstPlayer = Math.Abs(seed) % 2,
                ActivePlayer = Math.Abs(seed) % 2,
                Phase = GamePhase.WonderDraft,
                CurrentAge = 0,
                Military = 0
            };

            state.Players[0].Name = string.IsNullOrWhiteSpace(playerOne) ? "Player 1" : playerOne;
            state.Players[1].Name = string.IsNullOrWhiteSpace(playerTwo) ? "Player 2" : playerTwo;

            var random = new Random(seed);
            var progressTokens = Shuffle(catalog.ProgressTokens.Select(t => t.Id).ToList(), random);
            state.AvailableProgressTokens = progressTokens.Take(5).ToList();
            state.RemovedProgressTokens = progressTokens.Skip(5).ToList();
            state.DraftDeck = Shuffle(catalog.Wonders.Select(w => w.Id).ToList(), random).Take(8).ToList();
            state.DraftPickOrder = DraftOrder.ToList();
            state.DraftPickIndex = 0;
            RefillDraftOffer(state);
            state.ActivePlayer = state.DraftPickOrder[state.DraftPickIndex];
            return state;
        }

        public static GameState RebuildFromActions(int seed, string playerOne, string playerTwo, DuelCatalog catalog, IList<DuelAction> actions)
        {
            var state = CreateNewGame(seed, playerOne, playerTwo, catalog);
            foreach (var action in actions)
            {
                ApplyAction(state, catalog, action);
            }

            return state;
        }

        public static MoveResult ApplyAction(GameState state, DuelCatalog catalog, DuelAction action)
        {
            if (state == null)
            {
                return MoveResult.Fail("No game is active.");
            }

            if (state.Phase == GamePhase.GameOver)
            {
                return MoveResult.Fail("The game is already over.");
            }

            if (action.PlayerIndex < 0 || action.PlayerIndex > 1)
            {
                return MoveResult.Fail("Unknown player.");
            }

            switch (action.Type)
            {
                case DuelActionType.ChooseWonder:
                    return ChooseWonder(state, catalog, action);
                case DuelActionType.TakeCard:
                    return TakeCard(state, catalog, action);
                case DuelActionType.ChooseProgress:
                    return ChooseProgress(state, catalog, action);
                case DuelActionType.ChooseOpponentCard:
                    return ChooseOpponentCard(state, catalog, action);
                case DuelActionType.ChooseDiscardedCard:
                    return ChooseDiscardedCard(state, catalog, action);
                case DuelActionType.ChooseLibraryProgress:
                    return ChooseLibraryProgress(state, catalog, action);
                default:
                    return MoveResult.Fail("Unknown action.");
            }
        }

        public static bool IsCardAvailable(GameState state, BoardSlotState slot)
        {
            if (slot == null || slot.Removed)
            {
                return false;
            }

            foreach (var blocker in slot.CoveredBy)
            {
                var blockingSlot = state.Board.FirstOrDefault(s => s.SlotId == blocker);
                if (blockingSlot != null && !blockingSlot.Removed)
                {
                    return false;
                }
            }

            return true;
        }

        public static int CalculateBuildCost(GameState state, DuelCatalog catalog, int playerIndex, CardDefinition card)
        {
            if (card == null)
            {
                return 0;
            }

            return CalculateCost(state, catalog, playerIndex, card.Cost, card.Color == CardColor.Civilian, false, out _);
        }

        public static int CalculateWonderCost(GameState state, DuelCatalog catalog, int playerIndex, WonderDefinition wonder)
        {
            if (wonder == null)
            {
                return 0;
            }

            return CalculateCost(state, catalog, playerIndex, wonder.Cost, false, true, out _);
        }

        public static ScoreBreakdown ScorePlayer(GameState state, DuelCatalog catalog, int playerIndex)
        {
            var player = state.Players[playerIndex];
            var score = new ScoreBreakdown();
            score.Coins = player.Coins / 3;
            score.Military = MilitaryScore(state, playerIndex);

            foreach (var cardId in player.OwnedCards)
            {
                if (catalog.CardsById.TryGetValue(cardId, out var card))
                {
                    AddScoringEffects(state, catalog, playerIndex, card.Effects, score);
                }
            }

            foreach (var wonderId in player.BuiltWonders)
            {
                if (catalog.WondersById.TryGetValue(wonderId, out var wonder))
                {
                    AddScoringEffects(state, catalog, playerIndex, wonder.Effects, score);
                }
            }

            foreach (var tokenId in player.ProgressTokens)
            {
                if (catalog.ProgressById.TryGetValue(tokenId, out var token))
                {
                    AddScoringEffects(state, catalog, playerIndex, token.Effects, score);
                }
            }

            return score;
        }

        public static int CountCards(GameState state, DuelCatalog catalog, int playerIndex, CardColor color)
        {
            var count = 0;
            foreach (var cardId in state.Players[playerIndex].OwnedCards)
            {
                if (catalog.CardsById.TryGetValue(cardId, out var card) && card.Color == color)
                {
                    count++;
                }
            }

            return count;
        }

        private static MoveResult ChooseWonder(GameState state, DuelCatalog catalog, DuelAction action)
        {
            if (state.Phase != GamePhase.WonderDraft)
            {
                return MoveResult.Fail("Wonders can only be chosen during the wonder draft.");
            }

            if (action.PlayerIndex != state.ActivePlayer)
            {
                return MoveResult.Fail("It is not this player's draft pick.");
            }

            if (string.IsNullOrEmpty(action.WonderId) || !state.DraftOffer.Contains(action.WonderId))
            {
                return MoveResult.Fail("That wonder is not in the current draft offer.");
            }

            if (!catalog.WondersById.ContainsKey(action.WonderId))
            {
                return MoveResult.Fail("Unknown wonder.");
            }

            state.Players[action.PlayerIndex].ReservedWonders.Add(action.WonderId);
            state.DraftOffer.Remove(action.WonderId);
            state.DraftPickIndex++;

            if (state.DraftPickIndex >= state.DraftPickOrder.Count)
            {
                StartAge(state, catalog, 1);
                return MoveResult.Ok("Wonder draft complete.");
            }

            if (state.DraftOffer.Count == 0)
            {
                RefillDraftOffer(state);
            }

            state.ActivePlayer = state.DraftPickOrder[state.DraftPickIndex];
            return MoveResult.Ok("Wonder selected.");
        }

        private static MoveResult TakeCard(GameState state, DuelCatalog catalog, DuelAction action)
        {
            if (state.Phase != GamePhase.PlayingAge)
            {
                return MoveResult.Fail("Cards can only be taken during an age.");
            }

            if (action.PlayerIndex != state.ActivePlayer)
            {
                return MoveResult.Fail("It is not this player's turn.");
            }

            var slot = state.Board.FirstOrDefault(s => s.SlotId == action.SlotId);
            if (slot == null || slot.Removed)
            {
                return MoveResult.Fail("That card is no longer in the structure.");
            }

            if (!IsCardAvailable(state, slot))
            {
                return MoveResult.Fail("That card is still covered.");
            }

            if (!catalog.CardsById.TryGetValue(slot.CardId, out var card))
            {
                return MoveResult.Fail("Unknown card.");
            }

            var player = state.Players[action.PlayerIndex];
            var repeat = false;

            switch (action.CardMode)
            {
                case CardTakeMode.Build:
                {
                    if (player.OwnedCards.Contains(card.Id))
                    {
                        return MoveResult.Fail("This player already built that card.");
                    }

                    var freeByLink = IsFreeByLink(player, catalog, card.Cost);
                    var cost = CalculateCost(state, catalog, action.PlayerIndex, card.Cost, card.Color == CardColor.Civilian, false, out var tradeCost);
                    if (player.Coins < cost)
                    {
                        return MoveResult.Fail("Not enough coins to build this card.");
                    }

                    player.Coins -= cost;
                    PayTradeIncome(state, action.PlayerIndex, tradeCost);
                    player.OwnedCards.Add(card.Id);
                    if (freeByLink && player.Urbanism)
                    {
                        player.Coins += 4;
                    }

                    ApplyEffects(state, catalog, action.PlayerIndex, card.Effects, ref repeat);
                    if (card.Color == CardColor.Military && player.Strategy)
                    {
                        AdvanceMilitary(state, action.PlayerIndex, 1);
                    }

                    break;
                }
                case CardTakeMode.Discard:
                {
                    player.Coins += 2 + CountCards(state, catalog, action.PlayerIndex, CardColor.Commercial);
                    state.DiscardPile.Add(card.Id);
                    break;
                }
                case CardTakeMode.BuildWonder:
                {
                    if (string.IsNullOrEmpty(action.WonderId) || !player.ReservedWonders.Contains(action.WonderId))
                    {
                        return MoveResult.Fail("Choose one of your unbuilt wonders.");
                    }

                    if (player.BuiltWonders.Contains(action.WonderId))
                    {
                        return MoveResult.Fail("That wonder is already built.");
                    }

                    if (TotalBuiltWonders(state) >= 7)
                    {
                        return MoveResult.Fail("Seven wonders are already built.");
                    }

                    if (!catalog.WondersById.TryGetValue(action.WonderId, out var wonder))
                    {
                        return MoveResult.Fail("Unknown wonder.");
                    }

                    var cost = CalculateCost(state, catalog, action.PlayerIndex, wonder.Cost, false, true, out var tradeCost);
                    if (player.Coins < cost)
                    {
                        return MoveResult.Fail("Not enough coins to build this wonder.");
                    }

                    player.Coins -= cost;
                    PayTradeIncome(state, action.PlayerIndex, tradeCost);
                    player.BuiltWonders.Add(action.WonderId);
                    player.BuriedCards.Add(card.Id);
                    ApplyEffects(state, catalog, action.PlayerIndex, wonder.Effects, ref repeat);
                    if (player.Theology && !wonder.Effects.Any(effect => effect.Kind == EffectKind.RepeatTurn))
                    {
                        repeat = true;
                    }

                    break;
                }
                default:
                    return MoveResult.Fail("Unknown card action.");
            }

            slot.Removed = true;
            RevealUncoveredCards(state);
            ResolveAfterCardAction(state, catalog, repeat);
            return MoveResult.Ok("Card resolved.");
        }

        private static MoveResult ChooseProgress(GameState state, DuelCatalog catalog, DuelAction action)
        {
            if (state.Phase != GamePhase.ChoosingProgress)
            {
                return MoveResult.Fail("No progress token is being chosen.");
            }

            if (action.PlayerIndex != state.PendingProgressPlayer)
            {
                return MoveResult.Fail("This player is not choosing a progress token.");
            }

            if (string.IsNullOrEmpty(action.ProgressTokenId) || !state.AvailableProgressTokens.Contains(action.ProgressTokenId))
            {
                return MoveResult.Fail("That progress token is not available.");
            }

            if (!catalog.ProgressById.TryGetValue(action.ProgressTokenId, out var token))
            {
                return MoveResult.Fail("Unknown progress token.");
            }

            state.AvailableProgressTokens.Remove(action.ProgressTokenId);
            state.Players[action.PlayerIndex].ProgressTokens.Add(action.ProgressTokenId);

            var repeat = state.PendingRepeatTurn;
            state.PendingRepeatTurn = false;
            state.PendingProgressPlayer = -1;
            state.Phase = GamePhase.PlayingAge;
            ApplyEffects(state, catalog, action.PlayerIndex, token.Effects, ref repeat);
            ResolveAfterCardAction(state, catalog, repeat);
            return MoveResult.Ok("Progress token selected.");
        }

        private static MoveResult ChooseOpponentCard(GameState state, DuelCatalog catalog, DuelAction action)
        {
            if (state.Phase != GamePhase.ChoosingOpponentCard)
            {
                return MoveResult.Fail("No opponent card is being chosen.");
            }

            if (action.PlayerIndex != state.PendingChoicePlayer)
            {
                return MoveResult.Fail("This player is not choosing the opponent card.");
            }

            var opponent = state.Players[1 - action.PlayerIndex];
            if (string.IsNullOrEmpty(action.TargetCardId) || !opponent.OwnedCards.Contains(action.TargetCardId))
            {
                return MoveResult.Fail("That opponent card is not available.");
            }

            if (!catalog.CardsById.TryGetValue(action.TargetCardId, out var card) || card.Color != state.PendingChoiceCardColor)
            {
                return MoveResult.Fail("That card does not match this wonder effect.");
            }

            opponent.OwnedCards.Remove(action.TargetCardId);
            RemoveProductionEffects(opponent, card.Effects);
            state.DiscardPile.Add(action.TargetCardId);

            var repeat = state.PendingRepeatAfterChoice;
            ClearSpecialChoice(state);
            ResolveAfterCardAction(state, catalog, repeat);
            return MoveResult.Ok("Opponent card discarded.");
        }

        private static MoveResult ChooseDiscardedCard(GameState state, DuelCatalog catalog, DuelAction action)
        {
            if (state.Phase != GamePhase.ChoosingDiscardedCard)
            {
                return MoveResult.Fail("No discarded card is being chosen.");
            }

            if (action.PlayerIndex != state.PendingChoicePlayer)
            {
                return MoveResult.Fail("This player is not choosing from the discard pile.");
            }

            if (string.IsNullOrEmpty(action.TargetCardId) || !state.DiscardPile.Contains(action.TargetCardId))
            {
                return MoveResult.Fail("That discarded card is not available.");
            }

            if (!catalog.CardsById.TryGetValue(action.TargetCardId, out var card))
            {
                return MoveResult.Fail("Unknown discarded card.");
            }

            var player = state.Players[action.PlayerIndex];
            if (player.OwnedCards.Contains(card.Id))
            {
                return MoveResult.Fail("This player already built that card.");
            }

            state.DiscardPile.Remove(action.TargetCardId);
            player.OwnedCards.Add(card.Id);

            var repeat = state.PendingRepeatAfterChoice;
            ClearSpecialChoice(state);
            ApplyEffects(state, catalog, action.PlayerIndex, card.Effects, ref repeat);
            if (card.Color == CardColor.Military && player.Strategy)
            {
                AdvanceMilitary(state, action.PlayerIndex, 1);
            }

            ResolveAfterCardAction(state, catalog, repeat);
            return MoveResult.Ok("Discarded card built.");
        }

        private static MoveResult ChooseLibraryProgress(GameState state, DuelCatalog catalog, DuelAction action)
        {
            if (state.Phase != GamePhase.ChoosingLibraryProgress)
            {
                return MoveResult.Fail("No Great Library progress token is being chosen.");
            }

            if (action.PlayerIndex != state.PendingChoicePlayer)
            {
                return MoveResult.Fail("This player is not choosing a Great Library token.");
            }

            if (string.IsNullOrEmpty(action.ProgressTokenId) || !state.PendingProgressOffer.Contains(action.ProgressTokenId))
            {
                return MoveResult.Fail("That progress token is not in the Great Library offer.");
            }

            if (!catalog.ProgressById.TryGetValue(action.ProgressTokenId, out var token))
            {
                return MoveResult.Fail("Unknown progress token.");
            }

            state.RemovedProgressTokens.Remove(action.ProgressTokenId);
            state.Players[action.PlayerIndex].ProgressTokens.Add(action.ProgressTokenId);

            var repeat = state.PendingRepeatAfterChoice;
            ClearSpecialChoice(state);
            ApplyEffects(state, catalog, action.PlayerIndex, token.Effects, ref repeat);
            ResolveAfterCardAction(state, catalog, repeat);
            return MoveResult.Ok("Great Library progress token selected.");
        }

        private static void ResolveAfterCardAction(GameState state, DuelCatalog catalog, bool repeat)
        {
            if (state.Phase == GamePhase.GameOver)
            {
                return;
            }

            if (state.Phase == GamePhase.ChoosingProgress)
            {
                state.PendingRepeatTurn = repeat;
                return;
            }

            if (IsSpecialChoicePhase(state.Phase))
            {
                state.PendingRepeatAfterChoice = repeat;
                return;
            }

            if (state.Board.Count > 0 && state.Board.All(s => s.Removed))
            {
                if (state.CurrentAge >= 3)
                {
                    EndByCivilianScore(state, catalog);
                    return;
                }

                var nextAge = state.CurrentAge + 1;
                StartAge(state, catalog, nextAge);
                return;
            }

            if (!repeat)
            {
                state.ActivePlayer = 1 - state.ActivePlayer;
            }
        }

        private static void StartAge(GameState state, DuelCatalog catalog, int age)
        {
            state.Phase = GamePhase.PlayingAge;
            state.CurrentAge = age;
            state.PendingRepeatTurn = false;
            state.PendingProgressPlayer = -1;
            state.PendingChoicePlayer = -1;
            state.PendingRepeatAfterChoice = false;
            state.PendingProgressOffer.Clear();
            state.Board.Clear();

            if (!catalog.BoardLayouts.TryGetValue(age, out var layout))
            {
                EndByCivilianScore(state, catalog);
                return;
            }

            var random = new Random(state.Seed + age * 997);
            var ageCards = Shuffle(catalog.Cards.Where(c => c.Age == age && !c.IsGuild).Select(c => c.Id).ToList(), random);
            if (age == 3)
            {
                ageCards = ageCards.Take(Math.Max(0, layout.Count - 3)).ToList();
                var guilds = Shuffle(catalog.Cards.Where(c => c.Age == 3 && c.IsGuild).Select(c => c.Id).ToList(), random).Take(3);
                ageCards.AddRange(guilds);
            }
            else
            {
                ageCards = ageCards.Take(layout.Count).ToList();
            }

            ageCards = Shuffle(ageCards, random).Take(layout.Count).ToList();
            for (var i = 0; i < layout.Count; i++)
            {
                var definition = layout[i];
                state.Board.Add(new BoardSlotState
                {
                    SlotId = definition.SlotId,
                    CardId = ageCards[i],
                    FaceUp = definition.FaceUpAtStart,
                    Removed = false,
                    CoveredBy = definition.CoveredBy.ToList(),
                    X = definition.X,
                    Y = definition.Y
                });
            }

            RevealUncoveredCards(state);

            if (age == 1)
            {
                state.ActivePlayer = state.FirstPlayer;
            }
            else if (state.Military > 0)
            {
                state.ActivePlayer = 1;
            }
            else if (state.Military < 0)
            {
                state.ActivePlayer = 0;
            }
            else
            {
                state.ActivePlayer = 1 - state.FirstPlayer;
            }
        }

        private static void RefillDraftOffer(GameState state)
        {
            state.DraftOffer.Clear();
            while (state.DraftOffer.Count < 4 && state.DraftDeck.Count > 0)
            {
                state.DraftOffer.Add(state.DraftDeck[0]);
                state.DraftDeck.RemoveAt(0);
            }
        }

        private static void RevealUncoveredCards(GameState state)
        {
            foreach (var slot in state.Board)
            {
                if (!slot.Removed && IsCardAvailable(state, slot))
                {
                    slot.FaceUp = true;
                }
            }
        }

        private static int CalculateCost(GameState state, DuelCatalog catalog, int playerIndex, Cost cost, bool isCivilianCard, bool isWonder, out int tradeCost)
        {
            tradeCost = 0;
            if (cost == null)
            {
                return 0;
            }

            var player = state.Players[playerIndex];
            if (!string.IsNullOrEmpty(cost.FreeWithLink) && HasLink(player, catalog, cost.FreeWithLink))
            {
                return 0;
            }

            var total = cost.Coins;
            var required = cost.Resources;
            ApplyResourceDiscounts(state, playerIndex, ref required, isCivilianCard, isWonder);

            var missingByType = new Dictionary<ResourceType, int>();
            foreach (var resource in ResourceBundle.AllTypes)
            {
                var produced = player.Resources.Get(resource);
                missingByType[resource] = Math.Max(0, required.Get(resource) - produced);
            }

            SpendFlexibleProduction(state, playerIndex, missingByType, player.RawChoiceProduction, true);
            SpendFlexibleProduction(state, playerIndex, missingByType, player.ManufacturedChoiceProduction, false);

            foreach (var resource in ResourceBundle.AllTypes)
            {
                var missing = missingByType[resource];
                if (missing <= 0)
                {
                    continue;
                }

                tradeCost += missing * TradePrice(state, playerIndex, resource);
            }

            total += tradeCost;
            return total;
        }

        private static void ApplyResourceDiscounts(GameState state, int playerIndex, ref ResourceBundle required, bool isCivilianCard, bool isWonder)
        {
            var player = state.Players[playerIndex];
            var discounts = 0;
            if (isWonder && player.Architecture)
            {
                discounts += 2;
            }

            if (isCivilianCard && player.Masonry)
            {
                discounts += 2;
            }

            for (var i = 0; i < discounts; i++)
            {
                var bestResource = ResourceType.Wood;
                var bestPrice = -1;
                foreach (var resource in ResourceBundle.AllTypes)
                {
                    if (required.Get(resource) <= 0)
                    {
                        continue;
                    }

                    var price = TradePrice(state, playerIndex, resource);
                    if (price > bestPrice)
                    {
                        bestPrice = price;
                        bestResource = resource;
                    }
                }

                if (bestPrice < 0)
                {
                    return;
                }

                required.Add(bestResource, -1);
            }
        }

        private static void SpendFlexibleProduction(GameState state, int playerIndex, Dictionary<ResourceType, int> missingByType, int amount, bool raw)
        {
            for (var i = 0; i < amount; i++)
            {
                var bestResource = ResourceType.Wood;
                var bestPrice = -1;
                foreach (var resource in ResourceBundle.AllTypes)
                {
                    if (missingByType[resource] <= 0)
                    {
                        continue;
                    }

                    var isRaw = resource == ResourceType.Wood || resource == ResourceType.Clay || resource == ResourceType.Stone;
                    if (isRaw != raw)
                    {
                        continue;
                    }

                    var price = TradePrice(state, playerIndex, resource);
                    if (price > bestPrice)
                    {
                        bestPrice = price;
                        bestResource = resource;
                    }
                }

                if (bestPrice < 0)
                {
                    return;
                }

                missingByType[bestResource]--;
            }
        }

        private static int TradePrice(GameState state, int playerIndex, ResourceType resource)
        {
            var player = state.Players[playerIndex];
            if (player.OneCoinResources.Contains(resource))
            {
                return 1;
            }

            var raw = resource == ResourceType.Wood || resource == ResourceType.Clay || resource == ResourceType.Stone;
            var manufactured = resource == ResourceType.Glass || resource == ResourceType.Papyrus;
            if ((raw && player.RawDiscount) || (manufactured && player.ManufacturedDiscount))
            {
                return 1;
            }

            var opponentProduction = state.Players[1 - playerIndex].Resources.Get(resource);
            return 2 + opponentProduction;
        }

        private static bool HasLink(PlayerState player, DuelCatalog catalog, string link)
        {
            foreach (var cardId in player.OwnedCards)
            {
                if (catalog.CardsById.TryGetValue(cardId, out var card) && card.LinkProvided == link)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsFreeByLink(PlayerState player, DuelCatalog catalog, Cost cost)
        {
            return cost != null && !string.IsNullOrEmpty(cost.FreeWithLink) && HasLink(player, catalog, cost.FreeWithLink);
        }

        private static void PayTradeIncome(GameState state, int buyerIndex, int tradeCost)
        {
            if (tradeCost <= 0)
            {
                return;
            }

            var opponent = state.Players[1 - buyerIndex];
            if (opponent.Economy)
            {
                opponent.Coins += tradeCost;
            }
        }

        private static void ApplyEffects(GameState state, DuelCatalog catalog, int playerIndex, IEnumerable<Effect> effects, ref bool repeatTurn)
        {
            var player = state.Players[playerIndex];
            foreach (var effect in effects)
            {
                switch (effect.Kind)
                {
                    case EffectKind.ProduceResource:
                        player.Resources.Add(effect.Resource, Math.Max(1, effect.Amount));
                        break;
                    case EffectKind.ProduceRawChoice:
                        player.RawChoiceProduction += Math.Max(1, effect.Amount);
                        break;
                    case EffectKind.ProduceManufacturedChoice:
                        player.ManufacturedChoiceProduction += Math.Max(1, effect.Amount);
                        break;
                    case EffectKind.Coins:
                        player.Coins += effect.Amount;
                        break;
                    case EffectKind.Military:
                        AdvanceMilitary(state, playerIndex, effect.Amount);
                        break;
                    case EffectKind.Science:
                        AddScienceSymbol(state, playerIndex, effect.Science);
                        break;
                    case EffectKind.DiscountRaw:
                        player.RawDiscount = true;
                        break;
                    case EffectKind.DiscountManufactured:
                        player.ManufacturedDiscount = true;
                        break;
                    case EffectKind.DiscountResource:
                        if (!player.OneCoinResources.Contains(effect.Resource))
                        {
                            player.OneCoinResources.Add(effect.Resource);
                        }
                        break;
                    case EffectKind.OpponentLoseCoins:
                    {
                        var opponent = state.Players[1 - playerIndex];
                        opponent.Coins = Math.Max(0, opponent.Coins - Math.Max(1, effect.Amount));
                        break;
                    }
                    case EffectKind.OpponentDiscardRaw:
                        QueueOpponentCardChoice(state, catalog, playerIndex, CardColor.RawMaterial);
                        break;
                    case EffectKind.OpponentDiscardManufactured:
                        QueueOpponentCardChoice(state, catalog, playerIndex, CardColor.Manufactured);
                        break;
                    case EffectKind.BuildFromDiscard:
                        QueueDiscardedCardChoice(state, playerIndex);
                        break;
                    case EffectKind.ProgressFromRemoved:
                        QueueLibraryProgressChoice(state, playerIndex);
                        break;
                    case EffectKind.Economy:
                        player.Economy = true;
                        break;
                    case EffectKind.Strategy:
                        player.Strategy = true;
                        break;
                    case EffectKind.Architecture:
                        player.Architecture = true;
                        break;
                    case EffectKind.Masonry:
                        player.Masonry = true;
                        break;
                    case EffectKind.Theology:
                        player.Theology = true;
                        break;
                    case EffectKind.Urbanism:
                        player.Urbanism = true;
                        break;
                    case EffectKind.RepeatTurn:
                        repeatTurn = true;
                        break;
                    case EffectKind.CoinsPerRaw:
                        player.Coins += CountCards(state, catalog, playerIndex, CardColor.RawMaterial) * Math.Max(1, effect.Amount);
                        break;
                    case EffectKind.CoinsPerManufactured:
                        player.Coins += CountCards(state, catalog, playerIndex, CardColor.Manufactured) * Math.Max(1, effect.Amount);
                        break;
                    case EffectKind.CoinsPerCommercial:
                        player.Coins += CountCards(state, catalog, playerIndex, CardColor.Commercial) * Math.Max(1, effect.Amount);
                        break;
                    case EffectKind.CoinsPerMilitary:
                        player.Coins += CountCards(state, catalog, playerIndex, CardColor.Military) * Math.Max(1, effect.Amount);
                        break;
                    case EffectKind.CoinsPerWonder:
                        player.Coins += player.BuiltWonders.Count * Math.Max(1, effect.Amount);
                        break;
                    case EffectKind.CoinsPerMostRawAndManufactured:
                        player.Coins += MostRawAndManufacturedCount(state, catalog) * Math.Max(1, effect.Amount);
                        break;
                    case EffectKind.CoinsPerMostCommercial:
                        player.Coins += MostCardCount(state, catalog, CardColor.Commercial) * Math.Max(1, effect.Amount);
                        break;
                    case EffectKind.CoinsPerMostMilitary:
                        player.Coins += MostCardCount(state, catalog, CardColor.Military) * Math.Max(1, effect.Amount);
                        break;
                    case EffectKind.CoinsPerMostScience:
                        player.Coins += MostCardCount(state, catalog, CardColor.Science) * Math.Max(1, effect.Amount);
                        break;
                    case EffectKind.CoinsPerMostCivilian:
                        player.Coins += MostCardCount(state, catalog, CardColor.Civilian) * Math.Max(1, effect.Amount);
                        break;
                    case EffectKind.CoinsPerMostWonder:
                        player.Coins += Math.Max(state.Players[0].BuiltWonders.Count, state.Players[1].BuiltWonders.Count) * Math.Max(1, effect.Amount);
                        break;
                    case EffectKind.CoinsPerRichestThreeCoins:
                        player.Coins += (Math.Max(state.Players[0].Coins, state.Players[1].Coins) / 3) * Math.Max(1, effect.Amount);
                        break;
                }

                if (state.Phase == GamePhase.GameOver)
                {
                    return;
                }
            }
        }

        private static void QueueOpponentCardChoice(GameState state, DuelCatalog catalog, int playerIndex, CardColor color)
        {
            if (state.Phase != GamePhase.PlayingAge)
            {
                return;
            }

            var opponent = state.Players[1 - playerIndex];
            var hasEligible = opponent.OwnedCards.Any(cardId => catalog.CardsById.TryGetValue(cardId, out var card) && card.Color == color);
            if (!hasEligible)
            {
                return;
            }

            state.Phase = GamePhase.ChoosingOpponentCard;
            state.PendingChoicePlayer = playerIndex;
            state.PendingChoiceCardColor = color;
        }

        private static void QueueDiscardedCardChoice(GameState state, int playerIndex)
        {
            if (state.Phase != GamePhase.PlayingAge || state.DiscardPile.Count == 0)
            {
                return;
            }

            state.Phase = GamePhase.ChoosingDiscardedCard;
            state.PendingChoicePlayer = playerIndex;
        }

        private static void QueueLibraryProgressChoice(GameState state, int playerIndex)
        {
            if (state.Phase != GamePhase.PlayingAge || state.RemovedProgressTokens.Count == 0)
            {
                return;
            }

            state.PendingProgressOffer = state.RemovedProgressTokens.Take(3).ToList();
            state.Phase = GamePhase.ChoosingLibraryProgress;
            state.PendingChoicePlayer = playerIndex;
        }

        private static void RemoveProductionEffects(PlayerState player, IEnumerable<Effect> effects)
        {
            foreach (var effect in effects)
            {
                if (effect.Kind == EffectKind.ProduceResource)
                {
                    player.Resources.Add(effect.Resource, -Math.Max(1, effect.Amount));
                }
            }
        }

        private static bool IsSpecialChoicePhase(GamePhase phase)
        {
            return phase == GamePhase.ChoosingOpponentCard ||
                phase == GamePhase.ChoosingDiscardedCard ||
                phase == GamePhase.ChoosingLibraryProgress;
        }

        private static void ClearSpecialChoice(GameState state)
        {
            state.Phase = GamePhase.PlayingAge;
            state.PendingChoicePlayer = -1;
            state.PendingRepeatAfterChoice = false;
            state.PendingProgressOffer.Clear();
        }

        private static void AddScienceSymbol(GameState state, int playerIndex, ScienceSymbol symbol)
        {
            if (symbol == ScienceSymbol.None)
            {
                return;
            }

            var player = state.Players[playerIndex];
            player.ScienceSymbols.Add(symbol);
            if (player.ScienceSymbols.Distinct().Count() >= 6)
            {
                EndGame(state, playerIndex, VictoryKind.Science, player.Name + " completed six science symbols.");
                return;
            }

            var duplicateCount = player.ScienceSymbols.Count(s => s == symbol);
            if (duplicateCount > 1 && state.AvailableProgressTokens.Count > 0 && state.Phase != GamePhase.ChoosingProgress)
            {
                state.Phase = GamePhase.ChoosingProgress;
                state.PendingProgressPlayer = playerIndex;
            }
        }

        private static void AdvanceMilitary(GameState state, int playerIndex, int shields)
        {
            var before = state.Military;
            state.Military += playerIndex == 0 ? shields : -shields;
            ApplyMilitaryCoinLoss(state, before, state.Military);

            if (state.Military >= 9)
            {
                EndGame(state, 0, VictoryKind.Military, state.Players[0].Name + " reached the enemy capital.");
            }
            else if (state.Military <= -9)
            {
                EndGame(state, 1, VictoryKind.Military, state.Players[1].Name + " reached the enemy capital.");
            }
        }

        private static void ApplyMilitaryCoinLoss(GameState state, int before, int after)
        {
            var thresholds = new[] { 2, 5 };
            var losses = new[] { 2, 5 };
            for (var i = 0; i < thresholds.Length; i++)
            {
                if (before < thresholds[i] && after >= thresholds[i])
                {
                    state.Players[1].Coins = Math.Max(0, state.Players[1].Coins - losses[i]);
                }

                if (before > -thresholds[i] && after <= -thresholds[i])
                {
                    state.Players[0].Coins = Math.Max(0, state.Players[0].Coins - losses[i]);
                }
            }
        }

        private static void EndByCivilianScore(GameState state, DuelCatalog catalog)
        {
            var first = ScorePlayer(state, catalog, 0).Total;
            var second = ScorePlayer(state, catalog, 1).Total;
            if (first == second)
            {
                var firstCivilian = CivilianCardPoints(state, catalog, 0);
                var secondCivilian = CivilianCardPoints(state, catalog, 1);
                if (firstCivilian == secondCivilian)
                {
                    EndGame(state, -1, VictoryKind.Civilian, "Final score: " + first + " - " + second + ". Civilian points are tied.");
                    return;
                }

                EndGame(state, firstCivilian > secondCivilian ? 0 : 1, VictoryKind.Civilian, "Final score: " + first + " - " + second + ". Civilian buildings broke the tie.");
                return;
            }

            EndGame(state, first > second ? 0 : 1, VictoryKind.Civilian, "Final score: " + first + " - " + second + ".");
        }

        private static void EndGame(GameState state, int winner, VictoryKind victory, string reason)
        {
            state.Phase = GamePhase.GameOver;
            state.Winner = winner;
            state.Victory = victory;
            state.GameOverReason = reason;
        }

        private static void AddScoringEffects(GameState state, DuelCatalog catalog, int playerIndex, IEnumerable<Effect> effects, ScoreBreakdown score)
        {
            var player = state.Players[playerIndex];
            foreach (var effect in effects)
            {
                switch (effect.Kind)
                {
                    case EffectKind.Points:
                        score.CardsAndWonders += effect.Amount;
                        break;
                    case EffectKind.PointsPerRaw:
                        score.GuildsAndBonuses += CountCards(state, catalog, playerIndex, CardColor.RawMaterial) * effect.Amount;
                        break;
                    case EffectKind.PointsPerManufactured:
                        score.GuildsAndBonuses += CountCards(state, catalog, playerIndex, CardColor.Manufactured) * effect.Amount;
                        break;
                    case EffectKind.PointsPerCommercial:
                        score.GuildsAndBonuses += CountCards(state, catalog, playerIndex, CardColor.Commercial) * effect.Amount;
                        break;
                    case EffectKind.PointsPerMilitary:
                        score.GuildsAndBonuses += CountCards(state, catalog, playerIndex, CardColor.Military) * effect.Amount;
                        break;
                    case EffectKind.PointsPerScience:
                        score.GuildsAndBonuses += CountCards(state, catalog, playerIndex, CardColor.Science) * effect.Amount;
                        break;
                    case EffectKind.PointsPerWonder:
                        score.GuildsAndBonuses += player.BuiltWonders.Count * effect.Amount;
                        break;
                    case EffectKind.PointsPerProgress:
                        score.GuildsAndBonuses += player.ProgressTokens.Count * effect.Amount;
                        break;
                    case EffectKind.PointsPerThreeCoins:
                        score.GuildsAndBonuses += (player.Coins / 3) * effect.Amount;
                        break;
                    case EffectKind.PointsPerMostRawAndManufactured:
                        score.GuildsAndBonuses += MostRawAndManufacturedCount(state, catalog) * effect.Amount;
                        break;
                    case EffectKind.PointsPerMostCommercial:
                        score.GuildsAndBonuses += MostCardCount(state, catalog, CardColor.Commercial) * effect.Amount;
                        break;
                    case EffectKind.PointsPerMostMilitary:
                        score.GuildsAndBonuses += MostCardCount(state, catalog, CardColor.Military) * effect.Amount;
                        break;
                    case EffectKind.PointsPerMostScience:
                        score.GuildsAndBonuses += MostCardCount(state, catalog, CardColor.Science) * effect.Amount;
                        break;
                    case EffectKind.PointsPerMostCivilian:
                        score.GuildsAndBonuses += MostCardCount(state, catalog, CardColor.Civilian) * effect.Amount;
                        break;
                    case EffectKind.PointsPerMostWonder:
                        score.GuildsAndBonuses += Math.Max(state.Players[0].BuiltWonders.Count, state.Players[1].BuiltWonders.Count) * effect.Amount;
                        break;
                    case EffectKind.PointsPerRichestThreeCoins:
                        score.GuildsAndBonuses += (Math.Max(state.Players[0].Coins, state.Players[1].Coins) / 3) * effect.Amount;
                        break;
                }
            }
        }

        private static int MostCardCount(GameState state, DuelCatalog catalog, CardColor color)
        {
            return Math.Max(CountCards(state, catalog, 0, color), CountCards(state, catalog, 1, color));
        }

        private static int MostRawAndManufacturedCount(GameState state, DuelCatalog catalog)
        {
            var first = CountCards(state, catalog, 0, CardColor.RawMaterial) + CountCards(state, catalog, 0, CardColor.Manufactured);
            var second = CountCards(state, catalog, 1, CardColor.RawMaterial) + CountCards(state, catalog, 1, CardColor.Manufactured);
            return Math.Max(first, second);
        }

        private static int MilitaryScore(GameState state, int playerIndex)
        {
            var distance = playerIndex == 0 ? state.Military : -state.Military;
            if (distance <= 0)
            {
                return 0;
            }

            if (distance >= 6)
            {
                return 10;
            }

            return distance >= 3 ? 5 : 2;
        }

        private static int CivilianCardPoints(GameState state, DuelCatalog catalog, int playerIndex)
        {
            var total = 0;
            foreach (var cardId in state.Players[playerIndex].OwnedCards)
            {
                if (!catalog.CardsById.TryGetValue(cardId, out var card) || card.Color != CardColor.Civilian)
                {
                    continue;
                }

                foreach (var effect in card.Effects)
                {
                    if (effect.Kind == EffectKind.Points)
                    {
                        total += effect.Amount;
                    }
                }
            }

            return total;
        }

        private static int TotalBuiltWonders(GameState state)
        {
            return state.Players[0].BuiltWonders.Count + state.Players[1].BuiltWonders.Count;
        }

        private static List<T> Shuffle<T>(List<T> items, Random random)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var swapIndex = random.Next(i + 1);
                var temp = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = temp;
            }

            return items;
        }
    }

    public class ScoreBreakdown
    {
        public int CardsAndWonders;
        public int GuildsAndBonuses;
        public int Military;
        public int Coins;

        public int Total
        {
            get { return CardsAndWonders + GuildsAndBonuses + Military + Coins; }
        }
    }
}
