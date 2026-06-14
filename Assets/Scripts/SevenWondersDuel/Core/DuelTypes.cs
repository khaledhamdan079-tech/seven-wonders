using System;
using System.Collections.Generic;

namespace SevenWondersDuel.Core
{
    public enum GamePhase
    {
        WonderDraft,
        PlayingAge,
        ChoosingProgress,
        ChoosingOpponentCard,
        ChoosingDiscardedCard,
        ChoosingLibraryProgress,
        GameOver
    }

    public enum CardColor
    {
        RawMaterial,
        Manufactured,
        Civilian,
        Science,
        Commercial,
        Military,
        Guild
    }

    public enum ResourceType
    {
        Wood,
        Clay,
        Stone,
        Glass,
        Papyrus
    }

    public enum ScienceSymbol
    {
        None,
        Wheel,
        Mortar,
        Sundial,
        Quill,
        Globe,
        Compass,
        Law
    }

    public enum DuelActionType
    {
        ChooseWonder,
        TakeCard,
        ChooseProgress,
        ChooseOpponentCard,
        ChooseDiscardedCard,
        ChooseLibraryProgress
    }

    public enum CardTakeMode
    {
        Build,
        Discard,
        BuildWonder
    }

    public enum EffectKind
    {
        ProduceResource,
        Coins,
        Points,
        Military,
        Science,
        ProduceRawChoice,
        ProduceManufacturedChoice,
        DiscountRaw,
        DiscountManufactured,
        DiscountResource,
        OpponentLoseCoins,
        OpponentDiscardRaw,
        OpponentDiscardManufactured,
        BuildFromDiscard,
        ProgressFromRemoved,
        Economy,
        Strategy,
        Architecture,
        Masonry,
        Theology,
        Urbanism,
        RepeatTurn,
        CoinsPerRaw,
        CoinsPerManufactured,
        CoinsPerCommercial,
        CoinsPerMilitary,
        CoinsPerWonder,
        CoinsPerMostRawAndManufactured,
        CoinsPerMostCommercial,
        CoinsPerMostMilitary,
        CoinsPerMostScience,
        CoinsPerMostCivilian,
        CoinsPerMostWonder,
        CoinsPerRichestThreeCoins,
        PointsPerRaw,
        PointsPerManufactured,
        PointsPerCommercial,
        PointsPerMilitary,
        PointsPerScience,
        PointsPerWonder,
        PointsPerProgress,
        PointsPerThreeCoins,
        PointsPerMostRawAndManufactured,
        PointsPerMostCommercial,
        PointsPerMostMilitary,
        PointsPerMostScience,
        PointsPerMostCivilian,
        PointsPerMostWonder,
        PointsPerRichestThreeCoins
    }

    public enum VictoryKind
    {
        None,
        Military,
        Science,
        Civilian
    }

    [Serializable]
    public struct ResourceBundle
    {
        public int Wood;
        public int Clay;
        public int Stone;
        public int Glass;
        public int Papyrus;

        public static readonly ResourceType[] AllTypes =
        {
            ResourceType.Wood,
            ResourceType.Clay,
            ResourceType.Stone,
            ResourceType.Glass,
            ResourceType.Papyrus
        };

        public ResourceBundle(int wood, int clay, int stone, int glass, int papyrus)
        {
            Wood = wood;
            Clay = clay;
            Stone = stone;
            Glass = glass;
            Papyrus = papyrus;
        }

        public static ResourceBundle None()
        {
            return new ResourceBundle(0, 0, 0, 0, 0);
        }

        public static ResourceBundle One(ResourceType type, int amount = 1)
        {
            var bundle = None();
            bundle.Add(type, amount);
            return bundle;
        }

        public int Get(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood:
                    return Wood;
                case ResourceType.Clay:
                    return Clay;
                case ResourceType.Stone:
                    return Stone;
                case ResourceType.Glass:
                    return Glass;
                case ResourceType.Papyrus:
                    return Papyrus;
                default:
                    return 0;
            }
        }

        public void Add(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Wood:
                    Wood += amount;
                    break;
                case ResourceType.Clay:
                    Clay += amount;
                    break;
                case ResourceType.Stone:
                    Stone += amount;
                    break;
                case ResourceType.Glass:
                    Glass += amount;
                    break;
                case ResourceType.Papyrus:
                    Papyrus += amount;
                    break;
            }
        }

        public bool IsEmpty()
        {
            return Wood == 0 && Clay == 0 && Stone == 0 && Glass == 0 && Papyrus == 0;
        }

        public string ToShortString()
        {
            var parts = new List<string>();
            if (Wood > 0)
            {
                parts.Add("W" + Wood);
            }

            if (Clay > 0)
            {
                parts.Add("C" + Clay);
            }

            if (Stone > 0)
            {
                parts.Add("S" + Stone);
            }

            if (Glass > 0)
            {
                parts.Add("G" + Glass);
            }

            if (Papyrus > 0)
            {
                parts.Add("P" + Papyrus);
            }

            return parts.Count == 0 ? "-" : string.Join(" ", parts);
        }
    }

    [Serializable]
    public class Cost
    {
        public int Coins;
        public ResourceBundle Resources;
        public string FreeWithLink;

        public Cost()
        {
            Coins = 0;
            Resources = ResourceBundle.None();
            FreeWithLink = string.Empty;
        }

        public Cost(int coins, ResourceBundle resources, string freeWithLink = "")
        {
            Coins = coins;
            Resources = resources;
            FreeWithLink = freeWithLink;
        }

        public static Cost Free()
        {
            return new Cost();
        }
    }

    [Serializable]
    public class Effect
    {
        public EffectKind Kind;
        public int Amount;
        public ResourceType Resource;
        public ScienceSymbol Science;
        public string Label;

        public static Effect ResourceProduction(ResourceType type, int amount = 1)
        {
            return new Effect { Kind = EffectKind.ProduceResource, Resource = type, Amount = amount };
        }

        public static Effect Coins(int amount)
        {
            return new Effect { Kind = EffectKind.Coins, Amount = amount };
        }

        public static Effect Points(int amount)
        {
            return new Effect { Kind = EffectKind.Points, Amount = amount };
        }

        public static Effect Military(int amount)
        {
            return new Effect { Kind = EffectKind.Military, Amount = amount };
        }

        public static Effect ScienceSymbol(ScienceSymbol symbol)
        {
            return new Effect { Kind = EffectKind.Science, Science = symbol, Amount = 1 };
        }

        public static Effect RawChoice(int amount = 1)
        {
            return new Effect { Kind = EffectKind.ProduceRawChoice, Amount = amount };
        }

        public static Effect ManufacturedChoice(int amount = 1)
        {
            return new Effect { Kind = EffectKind.ProduceManufacturedChoice, Amount = amount };
        }

        public static Effect Simple(EffectKind kind, int amount = 0)
        {
            return new Effect { Kind = kind, Amount = amount };
        }

        public static Effect Simple(EffectKind kind, int amount, ResourceType resource)
        {
            return new Effect { Kind = kind, Amount = amount, Resource = resource };
        }
    }

    [Serializable]
    public class CardDefinition
    {
        public string Id;
        public string Name;
        public int Age;
        public CardColor Color;
        public Cost Cost = Cost.Free();
        public List<Effect> Effects = new List<Effect>();
        public string LinkProvided;
        public bool IsGuild;
    }

    [Serializable]
    public class WonderDefinition
    {
        public string Id;
        public string Name;
        public Cost Cost = Cost.Free();
        public List<Effect> Effects = new List<Effect>();
    }

    [Serializable]
    public class ProgressTokenDefinition
    {
        public string Id;
        public string Name;
        public List<Effect> Effects = new List<Effect>();
    }

    [Serializable]
    public class BoardSlotDefinition
    {
        public int SlotId;
        public float X;
        public float Y;
        public bool FaceUpAtStart;
        public List<int> CoveredBy = new List<int>();
    }

    [Serializable]
    public class BoardSlotState
    {
        public int SlotId;
        public string CardId;
        public bool FaceUp;
        public bool Removed;
        public List<int> CoveredBy = new List<int>();
        public float X;
        public float Y;
    }

    [Serializable]
    public class PlayerState
    {
        public string Name;
        public int Coins = 7;
        public ResourceBundle Resources = ResourceBundle.None();
        public int RawChoiceProduction;
        public int ManufacturedChoiceProduction;
        public bool RawDiscount;
        public bool ManufacturedDiscount;
        public bool Economy;
        public bool Strategy;
        public bool Architecture;
        public bool Masonry;
        public bool Theology;
        public bool Urbanism;
        public List<ResourceType> OneCoinResources = new List<ResourceType>();
        public List<string> ReservedWonders = new List<string>();
        public List<string> BuiltWonders = new List<string>();
        public List<string> OwnedCards = new List<string>();
        public List<string> BuriedCards = new List<string>();
        public List<string> ProgressTokens = new List<string>();
        public List<ScienceSymbol> ScienceSymbols = new List<ScienceSymbol>();
    }

    [Serializable]
    public class GameState
    {
        public int Seed;
        public GamePhase Phase;
        public int CurrentAge;
        public int ActivePlayer;
        public int FirstPlayer;
        public int Military;
        public bool PendingRepeatTurn;
        public int PendingProgressPlayer = -1;
        public VictoryKind Victory;
        public int Winner = -1;
        public string GameOverReason = string.Empty;
        public PlayerState[] Players = { new PlayerState(), new PlayerState() };
        public List<BoardSlotState> Board = new List<BoardSlotState>();
        public List<string> AvailableProgressTokens = new List<string>();
        public List<string> RemovedProgressTokens = new List<string>();
        public List<string> PendingProgressOffer = new List<string>();
        public List<string> DiscardPile = new List<string>();
        public List<string> DraftDeck = new List<string>();
        public List<string> DraftOffer = new List<string>();
        public List<int> DraftPickOrder = new List<int>();
        public int DraftPickIndex;
        public int PendingChoicePlayer = -1;
        public CardColor PendingChoiceCardColor;
        public bool PendingRepeatAfterChoice;
    }

    [Serializable]
    public class DuelAction
    {
        public DuelActionType Type;
        public int PlayerIndex;
        public int SlotId = -1;
        public CardTakeMode CardMode;
        public string WonderId;
        public string ProgressTokenId;
        public string TargetCardId;
    }

    public class MoveResult
    {
        public bool Success;
        public string Message;

        public static MoveResult Ok(string message = "")
        {
            return new MoveResult { Success = true, Message = message };
        }

        public static MoveResult Fail(string message)
        {
            return new MoveResult { Success = false, Message = message };
        }
    }
}
