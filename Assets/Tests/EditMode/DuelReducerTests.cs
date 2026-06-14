#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using SevenWondersDuel.Core;

public class DuelReducerTests
{
    [Test]
    public void NewGameStartsWithWonderDraftOffer()
    {
        var catalog = DuelCatalog.CreateDefault();
        var state = DuelReducer.CreateNewGame(1234, "A", "B", catalog);

        Assert.AreEqual(GamePhase.WonderDraft, state.Phase);
        Assert.AreEqual(4, state.DraftOffer.Count);
        Assert.AreEqual(5, state.AvailableProgressTokens.Count);
    }

    [Test]
    public void CompletingWonderDraftStartsFirstAge()
    {
        var catalog = DuelCatalog.CreateDefault();
        var state = DuelReducer.CreateNewGame(1234, "A", "B", catalog);

        while (state.Phase == GamePhase.WonderDraft)
        {
            var action = new DuelAction
            {
                Type = DuelActionType.ChooseWonder,
                PlayerIndex = state.ActivePlayer,
                WonderId = state.DraftOffer[0]
            };
            Assert.IsTrue(DuelReducer.ApplyAction(state, catalog, action).Success);
        }

        Assert.AreEqual(GamePhase.PlayingAge, state.Phase);
        Assert.AreEqual(1, state.CurrentAge);
        Assert.AreEqual(20, state.Board.Count);
        Assert.IsTrue(state.Board.Any(slot => DuelReducer.IsCardAvailable(state, slot)));
    }

    [Test]
    public void DiscardingAvailableCardAddsCoinsAndSwitchesTurn()
    {
        var catalog = DuelCatalog.CreateDefault();
        var state = StartedGame(catalog);
        var player = state.ActivePlayer;
        var coinsBefore = state.Players[player].Coins;
        var slot = state.Board.First(card => card.FaceUp && DuelReducer.IsCardAvailable(state, card));

        var result = DuelReducer.ApplyAction(state, catalog, new DuelAction
        {
            Type = DuelActionType.TakeCard,
            PlayerIndex = player,
            SlotId = slot.SlotId,
            CardMode = CardTakeMode.Discard
        });

        Assert.IsTrue(result.Success, result.Message);
        Assert.AreEqual(coinsBefore + 2, state.Players[player].Coins);
        Assert.AreEqual(1 - player, state.ActivePlayer);
        Assert.IsTrue(slot.Removed);
    }

    private static GameState StartedGame(DuelCatalog catalog)
    {
        var state = DuelReducer.CreateNewGame(4321, "A", "B", catalog);
        while (state.Phase == GamePhase.WonderDraft)
        {
            DuelReducer.ApplyAction(state, catalog, new DuelAction
            {
                Type = DuelActionType.ChooseWonder,
                PlayerIndex = state.ActivePlayer,
                WonderId = state.DraftOffer[0]
            });
        }

        return state;
    }
}
#endif
