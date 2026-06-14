using System;
using System.Collections.Generic;
using System.Linq;
using SevenWondersDuel.Core;
using SevenWondersDuel.Online;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SevenWondersDuel.UI
{
    public class DuelGameApp : MonoBehaviour
    {
        private const float PollInterval = 1.25f;
        private const int BaseTextSize = 20;

        private static readonly Color Ink = new Color(0.055f, 0.058f, 0.062f);
        private static readonly Color PanelDark = new Color(0.105f, 0.112f, 0.118f, 0.98f);
        private static readonly Color PanelMid = new Color(0.15f, 0.155f, 0.162f, 0.98f);
        private static readonly Color Table = new Color(0.18f, 0.16f, 0.13f, 0.98f);
        private static readonly Color GlassPanel = new Color(0.06f, 0.066f, 0.072f, 0.1f);
        private static readonly Color GlassPanelWarm = new Color(0.08f, 0.072f, 0.06f, 0.1f);
        private static readonly Color GlassPanelDeep = new Color(0.035f, 0.04f, 0.045f, 0.14f);
        private static readonly Color Gold = new Color(0.86f, 0.68f, 0.34f);
        private static readonly Color PaleGold = new Color(0.96f, 0.9f, 0.68f);
        private static readonly Color MutedText = new Color(0.72f, 0.72f, 0.68f);
        private static readonly Color Good = new Color(0.24f, 0.52f, 0.4f);
        private static readonly Color Danger = new Color(0.56f, 0.18f, 0.16f);

        private DuelCatalog catalog;
        private DuelOnlineClient online;
        private Canvas canvas;
        private RectTransform root;
        private Font font;
        private Texture2D tableTexture;
        private Texture2D cardBackTexture;
        private Texture2D cardFrontTexture;
        private Sprite cardBackSprite;
        private Sprite cardFrontSprite;
        private Sprite glowSprite;
        private GameState state;
        private bool onlineMode;
        private int localPlayerIndex = -1;
        private string playerName = "Player";
        private string serverUrl = DuelOnlineClient.DefaultBaseUrl;
        private string joinCode = "";
        private string statusMessage = "";
        private float pollCountdown;
        private int selectedSlotId = -1;
        private int renderedActionCount = -1;
        private string renderedRoomState = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<DuelGameApp>() != null)
            {
                return;
            }

            var app = new GameObject("Seven Wonders Duel App");
            DontDestroyOnLoad(app);
            app.AddComponent<DuelGameApp>();
        }

        private void Awake()
        {
            catalog = DuelCatalog.CreateDefault();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            LoadArtAssets();
            online = gameObject.AddComponent<DuelOnlineClient>();
            EnsureEventSystem();
            BuildCanvas();
            RenderLobby();
        }

        private void Update()
        {
            if (!onlineMode || online == null || online.Snapshot == null || string.IsNullOrEmpty(online.Snapshot.roomId))
            {
                return;
            }

            pollCountdown -= Time.deltaTime;
            if (pollCountdown > 0f)
            {
                return;
            }

            pollCountdown = PollInterval;
            StartCoroutine(online.PollRoom(OnOnlineSnapshot));
        }

        private void LoadArtAssets()
        {
            tableTexture = Resources.Load<Texture2D>("SevenWondersDuel/table_background");
            cardBackTexture = Resources.Load<Texture2D>("SevenWondersDuel/card_back");
            cardFrontTexture = Resources.Load<Texture2D>("SevenWondersDuel/card_front_frame");
            if (cardBackTexture != null)
            {
                cardBackSprite = Sprite.Create(cardBackTexture, new Rect(0, 0, cardBackTexture.width, cardBackTexture.height), new Vector2(0.5f, 0.5f), 100f);
            }

            if (cardFrontTexture != null)
            {
                cardFrontSprite = Sprite.Create(cardFrontTexture, new Rect(0, 0, cardFrontTexture.width, cardFrontTexture.height), new Vector2(0.5f, 0.5f), 100f);
            }

            glowSprite = Sprite.Create(CreateGlowTexture(256), new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f), 100f);
        }

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("Duel Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            root = canvasObject.GetComponent<RectTransform>();
        }

        private void RenderTableBackdrop(float shadeAlpha)
        {
            if (tableTexture != null)
            {
                var backdrop = new GameObject("Generated Table Background", typeof(RectTransform), typeof(RawImage));
                backdrop.transform.SetParent(root, false);
                SetRect(backdrop.GetComponent<RectTransform>(), Anchor(0, 0, 1, 1));
                var raw = backdrop.GetComponent<RawImage>();
                raw.texture = tableTexture;
                raw.color = Color.white;
                raw.raycastTarget = false;
                backdrop.AddComponent<SlowBackdropDrift>();
            }
            else
            {
                Panel(root, "Background", Anchor(0, 0, 1, 1), Ink, Gold, 0f);
            }

            Panel(root, "Backdrop Shade", Anchor(0, 0, 1, 1), new Color(0.02f, 0.018f, 0.014f, shadeAlpha), new Color(0, 0, 0, 0), 0f);
            Glow(root, "Left Candle Glow", Anchor(-0.08f, 0.44f, 0.23f, 1.02f), new Color(1f, 0.52f, 0.18f, 0.22f), 0.22f);
            Glow(root, "Right Bronze Glow", Anchor(0.78f, -0.05f, 1.08f, 0.45f), new Color(0.7f, 0.42f, 0.16f, 0.18f), 0.31f);
            RenderAmbientMotes(root, 18);
        }

        private void RenderAmbientMotes(RectTransform parent, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var x = Mathf.Repeat(0.08f + i * 0.137f, 1.04f) - 0.02f;
                var y = Mathf.Repeat(0.11f + i * 0.219f, 1.06f) - 0.03f;
                var size = 14f + (i % 5) * 7f;
                var go = new GameObject("Ambient Mote " + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(x, y);
                rect.anchorMax = new Vector2(x, y);
                rect.sizeDelta = new Vector2(size, size);
                var image = go.GetComponent<Image>();
                image.sprite = glowSprite;
                image.color = new Color(1f, 0.78f, 0.38f, 0.06f + (i % 3) * 0.025f);
                image.raycastTarget = false;
                var mote = go.AddComponent<UiMoteAnimator>();
                mote.Phase = i * 0.37f;
                mote.Drift = new Vector2(((i % 4) - 1.5f) * 4.5f, ((i % 5) - 2f) * 3.2f);
            }
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            DontDestroyOnLoad(eventSystem.gameObject);

            foreach (var legacyModule in FindObjectsByType<StandaloneInputModule>(FindObjectsInactive.Include))
            {
                legacyModule.enabled = false;
                Destroy(legacyModule);
            }

            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            inputModule.AssignDefaultActions();
        }

        private void RenderLobby()
        {
            renderedActionCount = -1;
            renderedRoomState = "";
            ClearRoot();
            RenderTableBackdrop(0.08f);
            DecorativePanel(root, "Top Accent", Anchor(0, 0.94f, 1, 1), new Color(0.42f, 0.24f, 0.14f, 0.22f), Gold, 0f);
            DecorativePanel(root, "Bottom Accent", Anchor(0, 0, 1, 0.028f), new Color(0.1f, 0.16f, 0.15f, 0.28f), Gold, 0f);
            Glow(root, "Lobby Center Glow", Anchor(0.16f, 0.08f, 0.84f, 0.92f), new Color(0.96f, 0.72f, 0.35f, 0.08f), 0.44f);

            var title = Panel(root, "Lobby Title", Anchor(0.055f, 0.16f, 0.47f, 0.86f), GlassPanelWarm, new Color(0.96f, 0.76f, 0.38f, 0.95f), 2f);
            AddGlassChrome(title, new Color(0.95f, 0.66f, 0.24f, 0.08f));
            Text(title, "DUEL OF WONDERS", 58, TextAnchor.UpperLeft, PaleGold, Anchor(0.08f, 0.66f, 0.92f, 0.92f));
            Text(title, "ONLINE TABLE", 28, TextAnchor.UpperLeft, Gold, Anchor(0.085f, 0.58f, 0.8f, 0.66f));
            Text(title, "A two-player ancient city duel", 24, TextAnchor.UpperLeft, new Color(0.88f, 0.86f, 0.76f), Anchor(0.085f, 0.48f, 0.82f, 0.57f));
            DrawLobbyEmblem(title);

            var controls = Panel(root, "Lobby Controls", Anchor(0.53f, 0.12f, 0.93f, 0.88f), GlassPanel, new Color(0.86f, 0.68f, 0.34f, 0.95f), 2f);
            AddGlassChrome(controls, new Color(0.28f, 0.54f, 0.64f, 0.07f));
            Text(controls, "Match Setup", 34, TextAnchor.MiddleLeft, Color.white, Anchor(0.07f, 0.87f, 0.72f, 0.96f));
            Text(controls, string.IsNullOrEmpty(statusMessage) ? "Ready" : statusMessage, 18, TextAnchor.MiddleRight, Gold, Anchor(0.52f, 0.875f, 0.93f, 0.955f));

            AddInput(controls, "Player name", playerName, value => playerName = string.IsNullOrWhiteSpace(value) ? "Player" : value, Anchor(0.07f, 0.735f, 0.93f, 0.845f));
            AddInput(controls, "Server URL", serverUrl, value => serverUrl = string.IsNullOrWhiteSpace(value) ? DuelOnlineClient.DefaultBaseUrl : value.Trim(), Anchor(0.07f, 0.595f, 0.93f, 0.705f));

            StyledButton(controls, "LOCAL HOTSEAT", StartLocalGame, Good, Anchor(0.07f, 0.47f, 0.48f, 0.555f));
            StyledButton(controls, "FIND MATCH", FindOnlineMatch, new Color(0.24f, 0.36f, 0.6f), Anchor(0.52f, 0.47f, 0.93f, 0.555f));
            StyledButton(controls, "CREATE ROOM", CreatePrivateRoom, new Color(0.43f, 0.33f, 0.56f), Anchor(0.07f, 0.355f, 0.93f, 0.44f));

            Text(controls, "Private Room", 22, TextAnchor.MiddleLeft, Color.white, Anchor(0.07f, 0.27f, 0.45f, 0.33f));
            AddInput(controls, "Room code", joinCode, value => joinCode = value.Trim().ToUpperInvariant(), Anchor(0.07f, 0.15f, 0.55f, 0.26f));
            StyledButton(controls, "JOIN", JoinPrivateRoom, new Color(0.55f, 0.42f, 0.18f), Anchor(0.59f, 0.15f, 0.93f, 0.26f));

            Text(controls, "Server: " + serverUrl, 15, TextAnchor.MiddleLeft, MutedText, Anchor(0.07f, 0.045f, 0.93f, 0.105f));
        }

        private void DrawLobbyEmblem(RectTransform parent)
        {
            var baseRect = Panel(parent, "Emblem Base", Anchor(0.16f, 0.12f, 0.84f, 0.42f), new Color(0.16f, 0.12f, 0.075f, 0.08f), new Color(0.86f, 0.68f, 0.34f, 0.88f), 2f);
            Glow(baseRect, "Emblem Glow", Anchor(-0.12f, -0.55f, 1.12f, 1.45f), new Color(0.95f, 0.65f, 0.26f, 0.12f), 0.26f).SetAsFirstSibling();
            Text(baseRect, "AGE I", 20, TextAnchor.MiddleCenter, PaleGold, Anchor(0.04f, 0.66f, 0.28f, 0.9f));
            Text(baseRect, "AGE II", 20, TextAnchor.MiddleCenter, PaleGold, Anchor(0.38f, 0.66f, 0.62f, 0.9f));
            Text(baseRect, "AGE III", 20, TextAnchor.MiddleCenter, PaleGold, Anchor(0.7f, 0.66f, 0.96f, 0.9f));
            for (var i = 0; i < 7; i++)
            {
                var x = 0.08f + i * 0.13f;
                DecorativePanel(baseRect, "Column " + i, Anchor(x, 0.14f, x + 0.05f, 0.62f), i % 2 == 0 ? new Color(0.86f, 0.68f, 0.34f, 0.9f) : new Color(0.5f, 0.4f, 0.24f, 0.85f), Gold, 0f);
            }
        }

        private void StartLocalGame()
        {
            onlineMode = false;
            localPlayerIndex = -1;
            selectedSlotId = -1;
            var seed = UnityEngine.Random.Range(1, int.MaxValue);
            state = DuelReducer.CreateNewGame(seed, "Player 1", "Player 2", catalog);
            statusMessage = "Local hotseat game started.";
            RenderGame();
        }

        private void FindOnlineMatch()
        {
            onlineMode = true;
            selectedSlotId = -1;
            serverUrl = DuelOnlineClient.NormalizeBaseUrl(serverUrl);
            online.BaseUrl = serverUrl;
            statusMessage = "Searching for another player";
            RenderWaiting();
            StartCoroutine(online.Matchmake(playerName, OnOnlineSnapshot));
        }

        private void CreatePrivateRoom()
        {
            onlineMode = true;
            selectedSlotId = -1;
            serverUrl = DuelOnlineClient.NormalizeBaseUrl(serverUrl);
            online.BaseUrl = serverUrl;
            statusMessage = "Creating private room";
            RenderWaiting();
            StartCoroutine(online.CreatePrivateRoom(playerName, OnOnlineSnapshot));
        }

        private void JoinPrivateRoom()
        {
            onlineMode = true;
            selectedSlotId = -1;
            serverUrl = DuelOnlineClient.NormalizeBaseUrl(serverUrl);
            online.BaseUrl = serverUrl;
            statusMessage = "Joining room";
            RenderWaiting();
            StartCoroutine(online.JoinPrivateRoom(joinCode, playerName, OnOnlineSnapshot));
        }

        private void OnOnlineSnapshot(RoomSnapshotDto snapshot)
        {
            if (snapshot == null)
            {
                statusMessage = string.IsNullOrEmpty(online.LastError) ? "Network request failed" : online.LastError;
                RenderWaiting();
                return;
            }

            localPlayerIndex = snapshot.playerIndex;
            if (!snapshot.started || snapshot.players == null || snapshot.players.Count < 2)
            {
                statusMessage = "Room " + snapshot.roomCode + " is waiting";
                var roomKey = snapshot.roomCode + ":" + snapshot.started + ":" + (snapshot.players == null ? 0 : snapshot.players.Count);
                if (renderedRoomState != roomKey)
                {
                    renderedRoomState = roomKey;
                    RenderWaiting();
                }

                return;
            }

            var first = snapshot.players.FirstOrDefault(p => p.index == 0);
            var second = snapshot.players.FirstOrDefault(p => p.index == 1);
            var firstName = first != null ? first.name : "Player 1";
            var secondName = second != null ? second.name : "Player 2";
            var actionCount = snapshot.actions == null ? 0 : snapshot.actions.Count;
            if (state != null && renderedActionCount == actionCount && renderedRoomState == snapshot.roomId)
            {
                return;
            }

            state = DuelReducer.RebuildFromActions(snapshot.seed, firstName, secondName, catalog, snapshot.actions ?? new List<DuelAction>());
            renderedActionCount = actionCount;
            renderedRoomState = snapshot.roomId;
            statusMessage = "Room " + snapshot.roomCode + " | Player " + (localPlayerIndex + 1);
            RenderGame();
        }

        private void RenderWaiting()
        {
            ClearRoot();
            RenderTableBackdrop(0.62f);
            var panel = Panel(root, "Waiting", Anchor(0.31f, 0.24f, 0.69f, 0.76f), GlassPanel, Gold, 2f);
            AddGlassChrome(panel, new Color(0.86f, 0.68f, 0.34f, 0.055f));
            Text(panel, "Online Room", 38, TextAnchor.MiddleCenter, PaleGold, Anchor(0.08f, 0.76f, 0.92f, 0.92f));
            if (online != null && online.Snapshot != null && !string.IsNullOrEmpty(online.Snapshot.roomCode))
            {
                Text(panel, online.Snapshot.roomCode, 54, TextAnchor.MiddleCenter, Color.white, Anchor(0.18f, 0.52f, 0.82f, 0.72f));
            }

            Text(panel, statusMessage, 22, TextAnchor.MiddleCenter, MutedText, Anchor(0.1f, 0.36f, 0.9f, 0.48f));
            StyledButton(panel, "BACK TO LOBBY", () =>
            {
                onlineMode = false;
                state = null;
                RenderLobby();
            }, Danger, Anchor(0.26f, 0.12f, 0.74f, 0.26f));
        }

        private void RenderGame()
        {
            if (state == null)
            {
                RenderLobby();
                return;
            }

            ClearRoot();
            RenderTableBackdrop(0.28f);
            RenderTopBar();
            RenderTurnBanner();
            RenderPlayerPanel(0, Anchor(0.012f, 0.08f, 0.172f, 0.91f));
            RenderPlayerPanel(1, Anchor(0.828f, 0.08f, 0.988f, 0.91f));

            if (state.Phase == GamePhase.WonderDraft)
            {
                RenderWonderDraft();
                return;
            }

            RenderBoard();
            RenderActionPanel();

            if (state.Phase == GamePhase.ChoosingProgress)
            {
                RenderProgressChoice();
            }

            if (state.Phase == GamePhase.ChoosingOpponentCard)
            {
                RenderOpponentCardChoice();
            }

            if (state.Phase == GamePhase.ChoosingDiscardedCard)
            {
                RenderDiscardedCardChoice();
            }

            if (state.Phase == GamePhase.ChoosingLibraryProgress)
            {
                RenderLibraryProgressChoice();
            }

            if (state.Phase == GamePhase.GameOver)
            {
                RenderGameOver();
            }
        }

        private void RenderTopBar()
        {
            var top = Panel(root, "Top Bar", Anchor(0, 0.905f, 1, 1), GlassPanel, new Color(0.22f, 0.18f, 0.12f, 0.2f), 0f);
            DecorativePanel(top, "Top Gold Hairline", Anchor(0, 0, 1, 0.035f), new Color(0.86f, 0.68f, 0.34f, 0.76f), new Color(0, 0, 0, 0), 0f);
            DecorativePanel(top, "Top Header Shine", Anchor(0, 0.64f, 1, 1), new Color(1f, 0.9f, 0.58f, 0.045f), new Color(0, 0, 0, 0), 0f);
            Glow(top, "Top Bar Warm Wash", Anchor(0.03f, -1.4f, 0.34f, 1.65f), new Color(0.95f, 0.55f, 0.2f, 0.08f), 0.6f).SetAsFirstSibling();
            Text(top, "7 WONDERS DUEL", 25, TextAnchor.MiddleLeft, PaleGold, Anchor(0.02f, 0.12f, 0.19f, 0.9f));
            Text(top, StatusLine(), 21, TextAnchor.MiddleLeft, Color.white, Anchor(0.205f, 0.12f, 0.54f, 0.9f));
            RenderMilitaryTrack(top);
            StyledButton(top, "LOBBY", () =>
            {
                onlineMode = false;
                state = null;
                RenderLobby();
            }, Danger, Anchor(0.91f, 0.19f, 0.985f, 0.81f), 15);
        }

        private void RenderTurnBanner()
        {
            if (state.Phase == GamePhase.GameOver)
            {
                return;
            }

            var activePlayer = state.Phase == GamePhase.ChoosingProgress ? state.PendingProgressPlayer : IsSpecialChoicePhase(state.Phase) ? state.PendingChoicePlayer : state.ActivePlayer;
            if (activePlayer < 0 || activePlayer >= state.Players.Length)
            {
                activePlayer = state.ActivePlayer;
            }

            var canAct = CanActFor(activePlayer);
            var color = canAct ? Good : Gold;
            var banner = Panel(root, "Turn Banner", Anchor(0.355f, 0.872f, 0.645f, 0.928f), GlassPanelDeep, color, 2f);
            AddGlassChrome(banner, new Color(color.r, color.g, color.b, 0.045f));
            banner.gameObject.AddComponent<UiFloatAnimator>().Amplitude = 3f;
            Glow(banner, "Turn Banner Glow", Anchor(-0.12f, -1.25f, 1.12f, 1.45f), new Color(color.r, color.g, color.b, 0.18f), 0.74f).SetAsFirstSibling();
            Panel(banner, "Turn Banner Rail", Anchor(0.035f, 0.08f, 0.965f, 0.16f), new Color(color.r, color.g, color.b, 0.55f), new Color(0, 0, 0, 0), 0f);

            var label = state.Phase == GamePhase.WonderDraft ? "WONDER DRAFT" : state.Phase == GamePhase.ChoosingProgress ? "PROGRESS TOKEN" : IsSpecialChoicePhase(state.Phase) ? "WONDER EFFECT" : "AGE " + state.CurrentAge;
            var turn = canAct ? "YOUR TURN" : state.Players[activePlayer].Name + "'S TURN";
            Text(banner, label, 12, TextAnchor.MiddleLeft, Gold, Anchor(0.07f, 0.48f, 0.35f, 0.92f));
            Text(banner, turn, 18, TextAnchor.MiddleCenter, Color.white, Anchor(0.24f, 0.22f, 0.76f, 0.9f));
            Text(banner, onlineMode ? "ONLINE" : "HOTSEAT", 12, TextAnchor.MiddleRight, MutedText, Anchor(0.68f, 0.48f, 0.93f, 0.92f));
        }

        private void RenderMilitaryTrack(RectTransform parent)
        {
            var track = Panel(parent, "Military Track", Anchor(0.555f, 0.14f, 0.895f, 0.86f), GlassPanelDeep, new Color(0.86f, 0.68f, 0.34f, 0.92f), 2f);
            AddGlassChrome(track, new Color(0.86f, 0.2f, 0.12f, 0.05f));
            DecorativePanel(track, "Military Blue Side", Anchor(0.055f, 0.37f, 0.49f, 0.66f), new Color(0.12f, 0.28f, 0.56f, 0.46f), new Color(0, 0, 0, 0), 0f);
            DecorativePanel(track, "Military Red Side", Anchor(0.51f, 0.37f, 0.945f, 0.66f), new Color(0.58f, 0.14f, 0.12f, 0.46f), new Color(0, 0, 0, 0), 0f);
            DecorativePanel(track, "Military Center", Anchor(0.487f, 0.26f, 0.513f, 0.78f), new Color(0.94f, 0.82f, 0.52f, 0.95f), new Color(0, 0, 0, 0), 0f);
            Text(track, "MILITARY", 11, TextAnchor.MiddleLeft, Gold, Anchor(0.04f, 0.69f, 0.3f, 0.98f));
            Text(track, state.Military.ToString(), 18, TextAnchor.MiddleCenter, Color.white, Anchor(0.43f, 0.68f, 0.57f, 0.98f));
            Text(track, "-9", 12, TextAnchor.MiddleLeft, new Color(0.72f, 0.82f, 1f), Anchor(0.05f, 0.04f, 0.15f, 0.32f));
            Text(track, "0", 12, TextAnchor.MiddleCenter, PaleGold, Anchor(0.43f, 0.04f, 0.57f, 0.32f));
            Text(track, "+9", 12, TextAnchor.MiddleRight, new Color(1f, 0.72f, 0.66f), Anchor(0.85f, 0.04f, 0.95f, 0.32f));

            for (var i = 0; i <= 6; i++)
            {
                var x = Mathf.Lerp(0.08f, 0.92f, i / 6f);
                DecorativePanel(track, "Military Tick " + i, Anchor(x - 0.003f, 0.31f, x + 0.003f, 0.72f), new Color(1f, 0.93f, 0.68f, i == 3 ? 0.55f : 0.25f), new Color(0, 0, 0, 0), 0f);
            }

            var normalized = Mathf.InverseLerp(-9f, 9f, state.Military);
            var markerColor = state.Military >= 0 ? Danger : new Color(0.22f, 0.38f, 0.65f);
            Glow(track, "Military Marker Glow", Anchor(normalized - 0.06f, -0.18f, normalized + 0.06f, 1.2f), new Color(markerColor.r, markerColor.g, markerColor.b, 0.22f), 0.38f);
            var marker = Panel(track, "Military Marker", Anchor(normalized - 0.024f, 0.18f, normalized + 0.024f, 0.86f), markerColor, Color.white, 2f);
            Text(marker, "ARMY", 10, TextAnchor.MiddleCenter, Color.white, Anchor(0.05f, 0.1f, 0.95f, 0.9f));
        }

        private string StatusLine()
        {
            if (state.Phase == GamePhase.GameOver)
            {
                return state.GameOverReason;
            }

            var mode = onlineMode ? "Online" : "Hotseat";
            if (state.Phase == GamePhase.WonderDraft)
            {
                return mode + " | Wonder draft | " + state.Players[state.ActivePlayer].Name;
            }

            if (state.Phase == GamePhase.ChoosingProgress)
            {
                return mode + " | Progress token | " + state.Players[state.PendingProgressPlayer].Name;
            }

            if (IsSpecialChoicePhase(state.Phase))
            {
                return mode + " | Wonder effect | " + state.Players[state.PendingChoicePlayer].Name;
            }

            return mode + " | Age " + state.CurrentAge + " | " + state.Players[state.ActivePlayer].Name + "'s turn";
        }

        private void RenderPlayerPanel(int playerIndex, Rect anchor)
        {
            var player = state.Players[playerIndex];
            var actor = state.Phase == GamePhase.ChoosingProgress ? state.PendingProgressPlayer : IsSpecialChoicePhase(state.Phase) ? state.PendingChoicePlayer : state.ActivePlayer;
            var active = actor == playerIndex && state.Phase != GamePhase.GameOver;
            var panelColor = GlassPanel;
            var panel = Panel(root, "Player " + playerIndex, anchor, panelColor, active ? Good : new Color(0.28f, 0.25f, 0.2f), active ? 3f : 1f);
            AddGlassChrome(panel, active ? new Color(0.3f, 0.78f, 0.48f, 0.08f) : new Color(0.8f, 0.62f, 0.32f, 0.05f));
            if (active)
            {
                Glow(panel, "Active City Glow", Anchor(-0.28f, 0.72f, 1.28f, 1.1f), new Color(0.36f, 0.82f, 0.52f, 0.16f), 0.37f).SetAsFirstSibling();
                var badge = Panel(panel, "Active Badge", Anchor(0.64f, 0.878f, 0.94f, 0.918f), new Color(0.22f, 0.42f, 0.28f, 0.94f), Good, 1f);
                badge.gameObject.AddComponent<UiFloatAnimator>().Amplitude = 1.4f;
                Text(badge, "ACTIVE", 11, TextAnchor.MiddleCenter, Color.white, Anchor(0.05f, 0.08f, 0.95f, 0.92f));
            }

            Text(panel, player.Name, 24, TextAnchor.MiddleCenter, Color.white, Anchor(0.06f, 0.925f, 0.94f, 0.985f));
            Text(panel, playerIndex == 0 ? "WEST CITY" : "EAST CITY", 13, TextAnchor.MiddleLeft, Gold, Anchor(0.08f, 0.88f, 0.62f, 0.925f));

            StatPill(panel, "COINS", player.Coins.ToString(), new Color(0.62f, 0.45f, 0.14f), Anchor(0.08f, 0.805f, 0.92f, 0.865f));
            RenderResourceInventory(panel, player, Anchor(0.08f, 0.595f, 0.92f, 0.785f));
            StatPill(panel, "SCIENCE", ScienceText(player), new Color(0.18f, 0.38f, 0.23f), Anchor(0.08f, 0.525f, 0.92f, 0.585f));

            Text(panel, "Cards", 17, TextAnchor.MiddleLeft, PaleGold, Anchor(0.08f, 0.475f, 0.42f, 0.515f));
            RenderCardCountGrid(panel, playerIndex, Anchor(0.08f, 0.335f, 0.92f, 0.47f));

            Text(panel, "Wonders", 17, TextAnchor.MiddleLeft, PaleGold, Anchor(0.08f, 0.285f, 0.48f, 0.325f));
            Text(panel, player.BuiltWonders.Count + "/" + player.ReservedWonders.Count + " built", 12, TextAnchor.MiddleRight, MutedText, Anchor(0.5f, 0.285f, 0.92f, 0.325f));
            RenderWonderStatus(panel, player, Anchor(0.08f, 0.145f, 0.92f, 0.28f));

            Text(panel, "Progress", 16, TextAnchor.MiddleLeft, PaleGold, Anchor(0.08f, 0.095f, 0.46f, 0.135f));
            Text(panel, TokenText(player), 13, TextAnchor.UpperLeft, MutedText, Anchor(0.08f, 0.025f, 0.92f, 0.095f));

            if (state.Phase == GamePhase.GameOver)
            {
                var score = DuelReducer.ScorePlayer(state, catalog, playerIndex);
                StatPill(panel, "SCORE", score.Total.ToString(), state.Winner < 0 || playerIndex == state.Winner ? Good : Danger, Anchor(0.08f, 0.01f, 0.92f, 0.065f));
            }
        }

        private void RenderCardCountGrid(RectTransform parent, int playerIndex, Rect anchor)
        {
            var grid = Panel(parent, "Card Counts", anchor, GlassPanelDeep, new Color(0, 0, 0, 0), 0f);
            CardCountChip(grid, "RAW", CardColor.RawMaterial, playerIndex, Anchor(0.02f, 0.55f, 0.31f, 0.95f));
            CardCountChip(grid, "GOODS", CardColor.Manufactured, playerIndex, Anchor(0.35f, 0.55f, 0.65f, 0.95f));
            CardCountChip(grid, "VP", CardColor.Civilian, playerIndex, Anchor(0.69f, 0.55f, 0.98f, 0.95f));
            CardCountChip(grid, "SCI", CardColor.Science, playerIndex, Anchor(0.02f, 0.08f, 0.31f, 0.48f));
            CardCountChip(grid, "WAR", CardColor.Military, playerIndex, Anchor(0.35f, 0.08f, 0.65f, 0.48f));
            CardCountChip(grid, "TRADE", CardColor.Commercial, playerIndex, Anchor(0.69f, 0.08f, 0.98f, 0.48f));
        }

        private void CardCountChip(RectTransform parent, string label, CardColor color, int playerIndex, Rect anchor)
        {
            var chip = Panel(parent, "Chip " + label, anchor, Color.Lerp(CardColorValue(color), Color.black, 0.25f), new Color(0, 0, 0, 0), 0f);
            Text(chip, label + " " + DuelReducer.CountCards(state, catalog, playerIndex, color), 12, TextAnchor.MiddleCenter, Color.white, Anchor(0.04f, 0.1f, 0.96f, 0.9f));
        }

        private void StatPill(RectTransform parent, string label, string value, Color color, Rect anchor)
        {
            var pill = Panel(parent, "Stat " + label, anchor, new Color(color.r, color.g, color.b, 0.34f), new Color(0, 0, 0, 0), 0f);
            DecorativePanel(pill, "Stat Highlight", Anchor(0, 0.55f, 1, 1), new Color(1f, 1f, 1f, 0.08f), new Color(0, 0, 0, 0), 0f);
            Text(pill, label, 12, TextAnchor.MiddleLeft, new Color(0.9f, 0.88f, 0.76f), Anchor(0.06f, 0, 0.38f, 1));
            Text(pill, value, 15, TextAnchor.MiddleRight, Color.white, Anchor(0.32f, 0, 0.94f, 1));
        }

        private void RenderResourceInventory(RectTransform parent, PlayerState player, Rect anchor)
        {
            var box = Panel(parent, "Resource Inventory", anchor, GlassPanelDeep, new Color(0.25f, 0.45f, 0.3f, 0.58f), 1f);
            DecorativePanel(box, "Resource Header Wash", Anchor(0, 0.78f, 1, 1), new Color(0.16f, 0.36f, 0.22f, 0.12f), new Color(0, 0, 0, 0), 0f);
            Text(box, "Resources", 13, TextAnchor.MiddleLeft, PaleGold, Anchor(0.06f, 0.79f, 0.58f, 0.99f));
            Text(box, ResourceSummary(player), 12, TextAnchor.MiddleRight, new Color(0.86f, 0.94f, 0.78f), Anchor(0.38f, 0.79f, 0.94f, 0.99f));

            for (var i = 0; i < ResourceBundle.AllTypes.Length; i++)
            {
                var resource = ResourceBundle.AllTypes[i];
                var amount = player.Resources.Get(resource);
                var row = 0.61f - i * 0.15f;
                var chipColor = amount > 0 ? ResourceColor(resource, 0.46f) : new Color(0.11f, 0.115f, 0.11f, 0.18f);
                var chip = Panel(box, "Resource " + resource, Anchor(0.06f, row, 0.94f, row + 0.115f), chipColor, new Color(0, 0, 0, 0), 0f);
                DecorativePanel(chip, "Resource Shine", Anchor(0, 0.52f, 1, 1), new Color(1f, 1f, 1f, amount > 0 ? 0.08f : 0.035f), new Color(0, 0, 0, 0), 0f);
                Text(chip, ResourceName(resource), 11, TextAnchor.MiddleLeft, amount > 0 ? Color.white : new Color(0.58f, 0.58f, 0.54f), Anchor(0.06f, 0, 0.68f, 1));
                Text(chip, amount.ToString(), 13, TextAnchor.MiddleRight, amount > 0 ? Color.white : new Color(0.58f, 0.58f, 0.54f), Anchor(0.72f, 0, 0.92f, 1));
            }
        }

        private void RenderWonderStatus(RectTransform parent, PlayerState player, Rect anchor)
        {
            var box = Panel(parent, "Wonder Status", anchor, GlassPanelDeep, new Color(0.44f, 0.32f, 0.15f, 0.58f), 1f);
            if (player.ReservedWonders.Count == 0)
            {
                Text(box, "-", 13, TextAnchor.MiddleCenter, MutedText, Anchor(0.06f, 0.1f, 0.94f, 0.9f));
                return;
            }

            for (var i = 0; i < player.ReservedWonders.Count; i++)
            {
                var wonderId = player.ReservedWonders[i];
                if (!catalog.WondersById.TryGetValue(wonderId, out var wonder))
                {
                    continue;
                }

                var built = player.BuiltWonders.Contains(wonderId);
                var y1 = 0.94f - i * 0.24f;
                var y0 = y1 - 0.18f;
                var row = Panel(box, "Wonder Row " + i, Anchor(0.04f, y0, 0.96f, y1), built ? new Color(0.2f, 0.42f, 0.28f, 0.38f) : new Color(0.25f, 0.18f, 0.09f, 0.28f), built ? Good : new Color(0, 0, 0, 0), built ? 1f : 0f);
                DecorativePanel(row, "Wonder Shine", Anchor(0, 0.54f, 1, 1), new Color(1f, 1f, 1f, built ? 0.08f : 0.045f), new Color(0, 0, 0, 0), 0f);
                Text(row, built ? "BUILT" : "PLAN", 9, TextAnchor.MiddleLeft, built ? Color.white : Gold, Anchor(0.05f, 0.03f, 0.27f, 0.97f));
                Text(row, ShortWonderName(wonder.Name), 10, TextAnchor.MiddleRight, Color.white, Anchor(0.28f, 0.03f, 0.95f, 0.97f));
            }
        }

        private void RenderWonderDraft()
        {
            var table = Panel(root, "Draft Table", Anchor(0.19f, 0.11f, 0.81f, 0.89f), GlassPanelWarm, new Color(0.86f, 0.68f, 0.34f, 0.92f), 2f);
            AddGlassChrome(table, new Color(0.95f, 0.58f, 0.18f, 0.06f));
            Glow(table, "Draft Warm Halo", Anchor(-0.06f, -0.18f, 1.06f, 1.18f), new Color(0.9f, 0.58f, 0.22f, 0.08f), 0.32f).SetAsFirstSibling();
            DecorativePanel(table, "Draft Top Rail", Anchor(0.035f, 0.82f, 0.965f, 0.835f), new Color(0.86f, 0.68f, 0.34f, 0.42f), new Color(0, 0, 0, 0), 0f);
            Text(table, "Wonder Draft", 38, TextAnchor.MiddleCenter, PaleGold, Anchor(0.08f, 0.88f, 0.92f, 0.97f));
            Text(table, state.Players[state.ActivePlayer].Name, 24, TextAnchor.MiddleCenter, Color.white, Anchor(0.2f, 0.815f, 0.8f, 0.875f));

            var canPick = CanActFor(state.ActivePlayer);
            for (var i = 0; i < state.DraftOffer.Count; i++)
            {
                var wonderId = state.DraftOffer[i];
                if (!catalog.WondersById.TryGetValue(wonderId, out var wonder))
                {
                    continue;
                }

                var x0 = 0.055f + i * 0.235f;
                var selectedWonderId = wonder.Id;
                var button = CreateWonderButton(table, wonder, Anchor(x0, 0.24f, x0 + 0.195f, 0.77f), () => SubmitAction(new DuelAction
                {
                    Type = DuelActionType.ChooseWonder,
                    PlayerIndex = ActionPlayer(),
                    WonderId = selectedWonderId
                }));
                button.interactable = canPick;
            }

            Text(table, canPick ? "Select a wonder" : "Waiting for opponent", 22, TextAnchor.MiddleCenter, MutedText, Anchor(0.1f, 0.08f, 0.9f, 0.18f));
        }

        private Button CreateWonderButton(RectTransform parent, WonderDefinition wonder, Rect anchor, Action clicked)
        {
            var button = FramedButton(parent, "Wonder " + wonder.Id, clicked, new Color(0.28f, 0.22f, 0.14f), Gold, anchor);
            var rect = button.GetComponent<RectTransform>();
            DecorativePanel(rect, "Wonder Static Rim", Anchor(0.035f, 0.03f, 0.965f, 0.97f), new Color(1f, 0.88f, 0.42f, 0.04f), new Color(0.96f, 0.78f, 0.38f, 0.68f), 1f).SetAsFirstSibling();
            if (cardFrontSprite != null)
            {
                var art = new GameObject("Wonder Front Art", typeof(RectTransform), typeof(Image));
                art.transform.SetParent(rect, false);
                SetRect(art.GetComponent<RectTransform>(), Anchor(0.025f, 0.02f, 0.975f, 0.98f));
                var image = art.GetComponent<Image>();
                image.sprite = cardFrontSprite;
                image.color = new Color(1f, 0.88f, 0.58f, 1f);
                image.raycastTarget = false;
            }

            Panel(rect, "Wonder Band", Anchor(0.095f, 0.80f, 0.905f, 0.92f), new Color(0.52f, 0.36f, 0.16f, 0.94f), new Color(0, 0, 0, 0), 0f);
            Text(rect, wonder.Name, 18, TextAnchor.MiddleCenter, Color.white, Anchor(0.08f, 0.80f, 0.92f, 0.92f));
            Text(rect, "WONDER", 18, TextAnchor.MiddleCenter, new Color(0.16f, 0.11f, 0.04f), Anchor(0.08f, 0.63f, 0.92f, 0.76f));
            RenderEffectChips(rect, wonder.Effects, Anchor(0.12f, 0.48f, 0.88f, 0.59f), 3, 10);
            Text(rect, "Cost\n" + CostText(wonder.Cost, true), 14, TextAnchor.MiddleCenter, new Color(0.18f, 0.13f, 0.06f), Anchor(0.12f, 0.30f, 0.88f, 0.46f));
            Text(rect, EffectsText(wonder.Effects, true, 72), 13, TextAnchor.UpperCenter, new Color(0.16f, 0.1f, 0.05f), Anchor(0.12f, 0.10f, 0.88f, 0.29f));
            return button;
        }

        private void RenderBoard()
        {
            var board = Panel(root, "Board", Anchor(0.19f, 0.245f, 0.81f, 0.905f), GlassPanelWarm, new Color(0.86f, 0.68f, 0.34f, 0.9f), 2f);
            AddGlassChrome(board, new Color(0.95f, 0.58f, 0.18f, 0.045f));
            RenderBoardOrnaments(board);
            Text(board, "Age " + state.CurrentAge, 26, TextAnchor.UpperCenter, PaleGold, Anchor(0.38f, 0.92f, 0.62f, 0.99f));
            RenderProgressTokens(board);

            foreach (var slot in state.Board.OrderByDescending(s => s.Y).ThenBy(s => s.X))
            {
                if (slot.Removed)
                {
                    continue;
                }

                var available = DuelReducer.IsCardAvailable(state, slot);
                catalog.CardsById.TryGetValue(slot.CardId, out var card);
                var button = CreateBoardCard(board, slot, card, available, () =>
                {
                    selectedSlotId = slot.SlotId;
                    RenderGame();
                });
                button.interactable = available && slot.FaceUp && state.Phase == GamePhase.PlayingAge;
            }
        }

        private void RenderBoardOrnaments(RectTransform board)
        {
            Glow(board, "Board Left Lamp", Anchor(-0.18f, 0.1f, 0.18f, 0.92f), new Color(1f, 0.54f, 0.2f, 0.1f), 0.48f).SetAsFirstSibling();
            Glow(board, "Board Right Lamp", Anchor(0.82f, 0.0f, 1.18f, 0.82f), new Color(0.22f, 0.72f, 0.52f, 0.08f), 0.82f).SetAsFirstSibling();
            Panel(board, "Board Inner Top Rail", Anchor(0.02f, 0.905f, 0.98f, 0.915f), new Color(0.86f, 0.68f, 0.34f, 0.28f), new Color(0, 0, 0, 0), 0f);
            Panel(board, "Board Inner Bottom Rail", Anchor(0.02f, 0.105f, 0.98f, 0.115f), new Color(0.86f, 0.68f, 0.34f, 0.18f), new Color(0, 0, 0, 0), 0f);
            Panel(board, "Board Felt Grain", Anchor(0.025f, 0.12f, 0.975f, 0.9f), new Color(0.02f, 0.026f, 0.022f, 0.12f), new Color(0, 0, 0, 0), 0f).SetAsFirstSibling();
        }

        private void RenderProgressTokens(RectTransform board)
        {
            var tokenBar = Panel(board, "Progress Bar", Anchor(0.03f, 0.02f, 0.97f, 0.095f), GlassPanelDeep, new Color(0.25f, 0.45f, 0.31f, 0.52f), 1f);
            Glow(tokenBar, "Progress Glow", Anchor(-0.04f, -1.15f, 1.04f, 2.1f), new Color(0.18f, 0.62f, 0.32f, 0.06f), 0.12f).SetAsFirstSibling();
            Text(tokenBar, "Progress", 14, TextAnchor.MiddleLeft, Gold, Anchor(0.02f, 0, 0.14f, 1));
            for (var i = 0; i < state.AvailableProgressTokens.Count; i++)
            {
                if (!catalog.ProgressById.TryGetValue(state.AvailableProgressTokens[i], out var token))
                {
                    continue;
                }

                var x = 0.16f + i * 0.16f;
                var chip = Panel(tokenBar, "Progress " + token.Id, Anchor(x, 0.16f, x + 0.14f, 0.84f), new Color(0.13f, 0.26f, 0.18f, 0.42f), new Color(0.5f, 0.78f, 0.45f, 0.45f), 1f);
                Text(chip, token.Name, 11, TextAnchor.MiddleCenter, new Color(0.86f, 0.94f, 0.78f), Anchor(0.04f, 0.08f, 0.96f, 0.92f));
            }
        }

        private Button CreateBoardCard(RectTransform parent, BoardSlotState slot, CardDefinition card, bool available, Action clicked)
        {
            var color = slot.FaceUp && card != null ? CardColorValue(card.Color) : new Color(0.16f, 0.16f, 0.18f);
            var cardSize = new Vector2(148, 194);
            var cardPosition = new Vector2(slot.X * 162f, (slot.Y - 2f) * 110f + 8f);
            CardShadow(parent, cardPosition, cardSize, available, slot.SlotId == selectedSlotId);
            var button = FramedButton(parent, "Card " + slot.SlotId, clicked, Color.Lerp(color, Color.black, 0.1f), available ? PaleGold : new Color(0.22f, 0.22f, 0.22f), default(Rect));
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = cardSize;
            rect.anchoredPosition = cardPosition;

            var selected = slot.SlotId == selectedSlotId;
            if (selected)
            {
                button.GetComponent<Outline>().effectColor = Color.white;
                button.GetComponent<Outline>().effectDistance = new Vector2(4f, -4f);
                DecorativePanel(rect, "Selected Inner Frame", Anchor(0.04f, 0.035f, 0.96f, 0.965f), new Color(1f, 0.88f, 0.42f, 0.08f), new Color(1f, 0.9f, 0.62f, 0.92f), 1.5f).SetAsFirstSibling();
            }
            else if (available && slot.FaceUp)
            {
                var outline = button.GetComponent<Outline>();
                outline.effectColor = new Color(PaleGold.r, PaleGold.g, PaleGold.b, 0.88f);
                outline.effectDistance = new Vector2(3f, -3f);
            }

            if (!slot.FaceUp || card == null)
            {
                if (cardBackSprite != null)
                {
                    var art = new GameObject("Card Back Art", typeof(RectTransform), typeof(Image));
                    art.transform.SetParent(rect, false);
                    SetRect(art.GetComponent<RectTransform>(), Anchor(0.035f, 0.025f, 0.965f, 0.975f));
                    var image = art.GetComponent<Image>();
                    image.sprite = cardBackSprite;
                    image.color = Color.white;
                    image.raycastTarget = false;
                }

                Panel(rect, "Hidden Veil", Anchor(0.035f, 0.025f, 0.965f, 0.975f), new Color(0, 0, 0, 0.14f), new Color(0, 0, 0, 0), 0f);
                Text(rect, "SEALED", 12, TextAnchor.MiddleCenter, new Color(0.8f, 0.72f, 0.58f, 0.82f), Anchor(0.18f, 0.45f, 0.82f, 0.55f));
                return button;
            }

            if (cardFrontSprite != null)
            {
                var art = new GameObject("Card Front Art", typeof(RectTransform), typeof(Image));
                art.transform.SetParent(rect, false);
                SetRect(art.GetComponent<RectTransform>(), Anchor(0.02f, 0.015f, 0.98f, 0.985f));
                var image = art.GetComponent<Image>();
                image.sprite = cardFrontSprite;
                image.color = Color.Lerp(Color.white, color, 0.18f);
                image.raycastTarget = false;
            }

            Panel(rect, "Color Banner", Anchor(0.095f, 0.79f, 0.905f, 0.915f), new Color(color.r, color.g, color.b, 0.92f), new Color(0, 0, 0, 0), 0f);
            Text(rect, card.Name, 14, TextAnchor.MiddleCenter, Color.white, Anchor(0.1f, 0.795f, 0.9f, 0.91f));
            Text(rect, CardTypeTitle(card), 11, TextAnchor.MiddleCenter, new Color(0.18f, 0.12f, 0.045f), Anchor(0.12f, 0.69f, 0.88f, 0.76f));
            Text(rect, PrimaryEffectText(card), 12, TextAnchor.MiddleCenter, new Color(0.12f, 0.085f, 0.035f), Anchor(0.12f, 0.49f, 0.88f, 0.66f));
            RenderEffectChips(rect, card.Effects, Anchor(0.1f, 0.34f, 0.9f, 0.45f), 3, 9);
            Text(rect, "Cost: " + CostText(card.Cost, true), 9, TextAnchor.MiddleCenter, new Color(0.18f, 0.12f, 0.055f), Anchor(0.12f, 0.25f, 0.88f, 0.34f));
            Text(rect, EffectsText(card.Effects, true, 66), 9, TextAnchor.UpperCenter, new Color(0.13f, 0.09f, 0.045f), Anchor(0.12f, 0.11f, 0.88f, 0.24f));
            return button;
        }

        private void CardShadow(RectTransform parent, Vector2 anchoredPosition, Vector2 size, bool available, bool selected)
        {
            var alpha = selected ? 0.5f : available ? 0.42f : 0.28f;
            var shadow = Panel(parent, "Card Shadow", default(Rect), new Color(0, 0, 0, alpha), new Color(0, 0, 0, 0), 0f);
            shadow.anchorMin = new Vector2(0.5f, 0.5f);
            shadow.anchorMax = new Vector2(0.5f, 0.5f);
            shadow.sizeDelta = size + new Vector2(selected ? 28f : 20f, selected ? 30f : 22f);
            shadow.anchoredPosition = anchoredPosition + new Vector2(9f, -12f);
            shadow.SetAsFirstSibling();
            var image = shadow.GetComponent<Image>();
            image.raycastTarget = false;
        }

        private void RenderEffectChips(RectTransform parent, IEnumerable<Effect> effects, Rect anchor, int maxChips, int textSize)
        {
            if (effects == null)
            {
                return;
            }

            var visibleEffects = effects.Take(maxChips).ToList();
            if (visibleEffects.Count == 0)
            {
                return;
            }

            var row = Panel(parent, "Effect Chips", anchor, new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), 0f);
            row.GetComponent<Image>().raycastTarget = false;
            for (var i = 0; i < visibleEffects.Count; i++)
            {
                var effect = visibleEffects[i];
                var width = 1f / visibleEffects.Count;
                var x0 = i * width + 0.018f;
                var x1 = (i + 1) * width - 0.018f;
                var chip = Panel(row, "Effect " + i, Anchor(x0, 0.08f, x1, 0.92f), EffectChipColor(effect), new Color(0, 0, 0, 0), 0f);
                chip.GetComponent<Image>().raycastTarget = false;
                Text(chip, EffectChipLabel(effect), textSize, TextAnchor.MiddleCenter, Color.white, Anchor(0.05f, 0.02f, 0.95f, 0.98f));
            }
        }

        private void RenderSelectedCardPreview(RectTransform parent, CardDefinition card, Rect anchor)
        {
            var color = CardColorValue(card.Color);
            var preview = Panel(parent, "Selected Card Preview", anchor, Color.Lerp(color, PanelDark, 0.62f), color, 2f);
            DecorativePanel(preview, "Preview Inner Frame", Anchor(0.035f, 0.035f, 0.965f, 0.965f), new Color(1f, 0.88f, 0.42f, 0.04f), new Color(color.r, color.g, color.b, 0.82f), 1f).SetAsFirstSibling();

            if (cardFrontSprite != null)
            {
                var art = new GameObject("Preview Art", typeof(RectTransform), typeof(Image));
                art.transform.SetParent(preview, false);
                SetRect(art.GetComponent<RectTransform>(), Anchor(0.025f, 0.03f, 0.975f, 0.97f));
                var image = art.GetComponent<Image>();
                image.sprite = cardFrontSprite;
                image.color = Color.Lerp(Color.white, color, 0.16f);
                image.raycastTarget = false;
            }

            Panel(preview, "Preview Banner", Anchor(0.08f, 0.78f, 0.92f, 0.91f), new Color(color.r, color.g, color.b, 0.94f), new Color(0, 0, 0, 0), 0f);
            Text(preview, card.Name, 21, TextAnchor.MiddleCenter, Color.white, Anchor(0.08f, 0.79f, 0.92f, 0.905f));
            Text(preview, CardTypeTitle(card), 13, TextAnchor.MiddleCenter, new Color(0.16f, 0.1f, 0.04f), Anchor(0.12f, 0.66f, 0.88f, 0.75f));
            Text(preview, PrimaryEffectText(card), 17, TextAnchor.MiddleCenter, new Color(0.11f, 0.075f, 0.032f), Anchor(0.12f, 0.46f, 0.88f, 0.64f));
            RenderEffectChips(preview, card.Effects, Anchor(0.1f, 0.31f, 0.9f, 0.43f), 4, 11);
            var link = string.IsNullOrEmpty(card.LinkProvided) ? "" : "\nLink: " + card.LinkProvided;
            Text(preview, "Cost: " + CostText(card.Cost, true) + link, 12, TextAnchor.UpperCenter, new Color(0.16f, 0.1f, 0.042f), Anchor(0.1f, 0.12f, 0.9f, 0.28f));
        }

        private string CardTypeTitle(CardDefinition card)
        {
            switch (card.Color)
            {
                case CardColor.RawMaterial:
                    return "Raw Material";
                case CardColor.Manufactured:
                    return "Manufactured Good";
                case CardColor.Civilian:
                    return "Civilian Points";
                case CardColor.Science:
                    return "Science Symbol";
                case CardColor.Commercial:
                    return "Commerce";
                case CardColor.Military:
                    return "Military";
                case CardColor.Guild:
                    return "Guild Scoring";
                default:
                    return "Card";
            }
        }

        private void RenderActionPanel()
        {
            var panel = Panel(root, "Action", Anchor(0.19f, 0.025f, 0.81f, 0.225f), GlassPanel, new Color(0.86f, 0.68f, 0.34f, 0.88f), 2f);
            AddGlassChrome(panel, new Color(0.45f, 0.62f, 0.72f, 0.045f));
            if (state.Phase != GamePhase.PlayingAge)
            {
                Text(panel, state.Phase == GamePhase.GameOver ? "Game complete" : "Resolve pending choice", 24, TextAnchor.MiddleCenter, Color.white, Anchor(0.1f, 0.2f, 0.9f, 0.8f));
                return;
            }

            var slot = state.Board.FirstOrDefault(s => s.SlotId == selectedSlotId && !s.Removed);
            if (slot == null || !slot.FaceUp || !DuelReducer.IsCardAvailable(state, slot) || !catalog.CardsById.TryGetValue(slot.CardId, out var card))
            {
                Text(panel, CanActFor(state.ActivePlayer) ? "Choose a face-up card" : "Waiting for " + state.Players[state.ActivePlayer].Name, 24, TextAnchor.MiddleCenter, Color.white, Anchor(0.1f, 0.25f, 0.9f, 0.75f));
                return;
            }

            RenderSelectedCardPreview(panel, card, Anchor(0.025f, 0.055f, 0.18f, 0.94f));

            var context = Panel(panel, "Action Context", Anchor(0.2f, 0.14f, 0.505f, 0.86f), GlassPanelDeep, new Color(0.86f, 0.68f, 0.34f, 0.42f), 1f);
            Text(context, ColorLabel(card.Color), 12, TextAnchor.UpperLeft, Gold, Anchor(0.08f, 0.72f, 0.92f, 0.95f));
            Text(context, "Cost\n" + CostText(card.Cost, true), 14, TextAnchor.UpperLeft, Color.white, Anchor(0.08f, 0.43f, 0.92f, 0.72f));
            Text(context, EffectsText(card.Effects, true, 155), 13, TextAnchor.UpperLeft, new Color(0.9f, 0.88f, 0.8f), Anchor(0.08f, 0.08f, 0.92f, 0.42f));

            var canAct = CanActFor(state.ActivePlayer);
            var active = state.ActivePlayer;
            var buildCost = DuelReducer.CalculateBuildCost(state, catalog, active, card);
            var build = StyledButton(panel, "BUILD\n" + buildCost + " coins", () => SubmitAction(new DuelAction
            {
                Type = DuelActionType.TakeCard,
                PlayerIndex = ActionPlayer(),
                SlotId = slot.SlotId,
                CardMode = CardTakeMode.Build
            }), Good, Anchor(0.525f, 0.18f, 0.625f, 0.82f), 16);
            build.interactable = canAct && state.Players[active].Coins >= buildCost;

            var discard = StyledButton(panel, "DISCARD\n+" + (2 + DuelReducer.CountCards(state, catalog, active, CardColor.Commercial)) + " coins", () => SubmitAction(new DuelAction
            {
                Type = DuelActionType.TakeCard,
                PlayerIndex = ActionPlayer(),
                SlotId = slot.SlotId,
                CardMode = CardTakeMode.Discard
            }), new Color(0.45f, 0.28f, 0.2f), Anchor(0.64f, 0.18f, 0.74f, 0.82f), 16);
            discard.interactable = canAct;

            RenderWonderBuildButtons(panel, slot, active, canAct);
        }

        private void RenderWonderBuildButtons(RectTransform panel, BoardSlotState slot, int active, bool canAct)
        {
            var player = state.Players[active];
            var unbuilt = player.ReservedWonders.Where(id => !player.BuiltWonders.Contains(id)).Take(2).ToList();
            for (var i = 0; i < unbuilt.Count; i++)
            {
                var wonderId = unbuilt[i];
                if (!catalog.WondersById.TryGetValue(wonderId, out var wonder))
                {
                    continue;
                }

                var cost = DuelReducer.CalculateWonderCost(state, catalog, active, wonder);
                var selectedWonderId = wonderId;
                var button = StyledButton(panel, "WONDER\n" + ShortWonderName(wonder.Name) + "\n" + cost + " coins", () => SubmitAction(new DuelAction
                {
                    Type = DuelActionType.TakeCard,
                    PlayerIndex = ActionPlayer(),
                    SlotId = slot.SlotId,
                    CardMode = CardTakeMode.BuildWonder,
                    WonderId = selectedWonderId
                }), new Color(0.38f, 0.3f, 0.17f), Anchor(0.755f + i * 0.12f, 0.12f, 0.865f + i * 0.12f, 0.88f), 12);
                button.interactable = canAct && player.Coins >= cost && TotalBuiltWonders() < 7;
            }
        }

        private void RenderProgressChoice()
        {
            var shade = Panel(root, "Modal Shade", Anchor(0, 0, 1, 1), new Color(0, 0, 0, 0.58f), new Color(0, 0, 0, 0), 0f);
            var overlay = Panel(shade, "Progress Choice", Anchor(0.28f, 0.22f, 0.72f, 0.79f), GlassPanel, Good, 2f);
            AddGlassChrome(overlay, new Color(0.24f, 0.68f, 0.36f, 0.055f));
            Text(overlay, "Choose Progress", 34, TextAnchor.MiddleCenter, PaleGold, Anchor(0.08f, 0.84f, 0.92f, 0.95f));
            var canChoose = CanActFor(state.PendingProgressPlayer);

            for (var i = 0; i < state.AvailableProgressTokens.Count; i++)
            {
                if (!catalog.ProgressById.TryGetValue(state.AvailableProgressTokens[i], out var token))
                {
                    continue;
                }

                var row = 0.72f - i * 0.125f;
                var tokenId = token.Id;
                var button = StyledButton(overlay, token.Name + "   " + EffectsText(token.Effects), () => SubmitAction(new DuelAction
                {
                    Type = DuelActionType.ChooseProgress,
                    PlayerIndex = ActionPlayer(),
                    ProgressTokenId = tokenId
                }), new Color(0.18f, 0.38f, 0.24f), Anchor(0.08f, row, 0.92f, row + 0.085f), 16);
                button.interactable = canChoose;
            }
        }

        private void RenderOpponentCardChoice()
        {
            var opponentIndex = 1 - state.PendingChoicePlayer;
            var color = state.PendingChoiceCardColor;
            var choices = state.Players[opponentIndex].OwnedCards
                .Where(id => catalog.CardsById.ContainsKey(id) && catalog.CardsById[id].Color == color)
                .Take(8)
                .ToList();

            var shade = Panel(root, "Modal Shade", Anchor(0, 0, 1, 1), new Color(0, 0, 0, 0.58f), new Color(0, 0, 0, 0), 0f);
            var overlay = Panel(shade, "Opponent Card Choice", Anchor(0.29f, 0.21f, 0.71f, 0.8f), GlassPanel, Danger, 2f);
            AddGlassChrome(overlay, new Color(0.75f, 0.18f, 0.12f, 0.055f));
            Text(overlay, "Discard Opponent " + ColorLabel(color), 30, TextAnchor.MiddleCenter, PaleGold, Anchor(0.06f, 0.84f, 0.94f, 0.95f));
            Text(overlay, state.Players[state.PendingChoicePlayer].Name, 16, TextAnchor.MiddleCenter, MutedText, Anchor(0.1f, 0.775f, 0.9f, 0.835f));
            var canChoose = CanActFor(state.PendingChoicePlayer);
            RenderCardChoiceButtons(overlay, choices, canChoose, 0.68f, cardId => new DuelAction
            {
                Type = DuelActionType.ChooseOpponentCard,
                PlayerIndex = ActionPlayer(),
                TargetCardId = cardId
            });
        }

        private void RenderDiscardedCardChoice()
        {
            var choices = state.DiscardPile
                .Where(id => catalog.CardsById.ContainsKey(id) && !state.Players[state.PendingChoicePlayer].OwnedCards.Contains(id))
                .Take(8)
                .ToList();

            var shade = Panel(root, "Modal Shade", Anchor(0, 0, 1, 1), new Color(0, 0, 0, 0.58f), new Color(0, 0, 0, 0), 0f);
            var overlay = Panel(shade, "Discarded Card Choice", Anchor(0.29f, 0.21f, 0.71f, 0.8f), GlassPanel, Gold, 2f);
            AddGlassChrome(overlay, new Color(0.95f, 0.64f, 0.22f, 0.055f));
            Text(overlay, "Build From Discard", 30, TextAnchor.MiddleCenter, PaleGold, Anchor(0.06f, 0.84f, 0.94f, 0.95f));
            Text(overlay, state.Players[state.PendingChoicePlayer].Name, 16, TextAnchor.MiddleCenter, MutedText, Anchor(0.1f, 0.775f, 0.9f, 0.835f));
            var canChoose = CanActFor(state.PendingChoicePlayer);
            RenderCardChoiceButtons(overlay, choices, canChoose, 0.68f, cardId => new DuelAction
            {
                Type = DuelActionType.ChooseDiscardedCard,
                PlayerIndex = ActionPlayer(),
                TargetCardId = cardId
            });
        }

        private void RenderLibraryProgressChoice()
        {
            var shade = Panel(root, "Modal Shade", Anchor(0, 0, 1, 1), new Color(0, 0, 0, 0.58f), new Color(0, 0, 0, 0), 0f);
            var overlay = Panel(shade, "Great Library Choice", Anchor(0.28f, 0.24f, 0.72f, 0.76f), GlassPanel, Good, 2f);
            AddGlassChrome(overlay, new Color(0.24f, 0.68f, 0.36f, 0.055f));
            Text(overlay, "The Great Library", 31, TextAnchor.MiddleCenter, PaleGold, Anchor(0.08f, 0.82f, 0.92f, 0.94f));
            Text(overlay, "Choose one removed progress token", 17, TextAnchor.MiddleCenter, MutedText, Anchor(0.08f, 0.74f, 0.92f, 0.82f));
            var canChoose = CanActFor(state.PendingChoicePlayer);

            for (var i = 0; i < state.PendingProgressOffer.Count; i++)
            {
                if (!catalog.ProgressById.TryGetValue(state.PendingProgressOffer[i], out var token))
                {
                    continue;
                }

                var row = 0.58f - i * 0.16f;
                var tokenId = token.Id;
                var button = StyledButton(overlay, token.Name + "   " + EffectsText(token.Effects), () => SubmitAction(new DuelAction
                {
                    Type = DuelActionType.ChooseLibraryProgress,
                    PlayerIndex = ActionPlayer(),
                    ProgressTokenId = tokenId
                }), new Color(0.18f, 0.38f, 0.24f), Anchor(0.08f, row, 0.92f, row + 0.105f), 16);
                button.interactable = canChoose;
            }
        }

        private void RenderCardChoiceButtons(RectTransform overlay, List<string> choices, bool canChoose, float startY, Func<string, DuelAction> actionFactory)
        {
            if (choices.Count == 0)
            {
                Text(overlay, "No valid card is available", 20, TextAnchor.MiddleCenter, MutedText, Anchor(0.1f, 0.34f, 0.9f, 0.48f));
                return;
            }

            for (var i = 0; i < choices.Count; i++)
            {
                var cardId = choices[i];
                var card = catalog.CardsById[cardId];
                var row = startY - i * 0.075f;
                var button = StyledButton(overlay, card.Name + "   " + EffectsText(card.Effects, true, 54), () => SubmitAction(actionFactory(cardId)), CardColorValue(card.Color), Anchor(0.08f, row, 0.92f, row + 0.055f), 13);
                button.interactable = canChoose;
            }
        }

        private void RenderGameOver()
        {
            var shade = Panel(root, "Game Over Shade", Anchor(0, 0, 1, 1), new Color(0, 0, 0, 0.62f), new Color(0, 0, 0, 0), 0f);
            var overlay = Panel(shade, "Game Over", Anchor(0.34f, 0.3f, 0.66f, 0.72f), GlassPanel, Gold, 2f);
            AddGlassChrome(overlay, new Color(0.86f, 0.68f, 0.34f, 0.055f));
            var title = state.Winner < 0 ? "Shared victory" : state.Players[state.Winner].Name + " wins";
            Text(overlay, title, 36, TextAnchor.MiddleCenter, PaleGold, Anchor(0.08f, 0.74f, 0.92f, 0.9f));
            Text(overlay, state.Victory + " victory\n" + state.GameOverReason, 20, TextAnchor.MiddleCenter, Color.white, Anchor(0.1f, 0.48f, 0.9f, 0.7f));
            StyledButton(overlay, "NEW HOTSEAT GAME", StartLocalGame, Good, Anchor(0.16f, 0.25f, 0.84f, 0.38f));
            StyledButton(overlay, "LOBBY", () =>
            {
                onlineMode = false;
                state = null;
                RenderLobby();
            }, Danger, Anchor(0.16f, 0.1f, 0.84f, 0.22f));
        }

        private void SubmitAction(DuelAction action)
        {
            if (state == null)
            {
                return;
            }

            action.PlayerIndex = ActionPlayer();
            if (onlineMode)
            {
                if (!CanActFor(action.PlayerIndex))
                {
                    return;
                }

                StartCoroutine(online.SubmitAction(action, OnOnlineSnapshot));
                return;
            }

            var result = DuelReducer.ApplyAction(state, catalog, action);
            statusMessage = result.Message;
            selectedSlotId = -1;
            RenderGame();
        }

        private bool CanActFor(int playerIndex)
        {
            if (state == null || state.Phase == GamePhase.GameOver)
            {
                return false;
            }

            if (!onlineMode)
            {
                return true;
            }

            return localPlayerIndex == playerIndex;
        }

        private int ActionPlayer()
        {
            if (onlineMode)
            {
                return localPlayerIndex;
            }

            if (state.Phase == GamePhase.ChoosingProgress)
            {
                return state.PendingProgressPlayer;
            }

            if (IsSpecialChoicePhase(state.Phase))
            {
                return state.PendingChoicePlayer;
            }

            return state.ActivePlayer;
        }

        private int TotalBuiltWonders()
        {
            return state.Players[0].BuiltWonders.Count + state.Players[1].BuiltWonders.Count;
        }

        private static bool IsSpecialChoicePhase(GamePhase phase)
        {
            return phase == GamePhase.ChoosingOpponentCard ||
                phase == GamePhase.ChoosingDiscardedCard ||
                phase == GamePhase.ChoosingLibraryProgress;
        }

        private string CostText(Cost cost, bool detailed = false)
        {
            if (cost == null)
            {
                return "-";
            }

            var parts = new List<string>();
            if (cost.Coins > 0)
            {
                parts.Add(detailed ? cost.Coins + " coin" : cost.Coins + " coin");
            }

            var resources = detailed ? ResourceBundleText(cost.Resources) : cost.Resources.ToShortString();
            if (resources != "-")
            {
                parts.Add(resources);
            }

            if (!string.IsNullOrEmpty(cost.FreeWithLink))
            {
                parts.Add("link " + cost.FreeWithLink);
            }

            return parts.Count == 0 ? "free" : string.Join(" + ", parts.ToArray());
        }

        private string EffectsText(IEnumerable<Effect> effects, bool detailed = false, int maxLength = 42)
        {
            var parts = effects.Select(effect => EffectText(effect, detailed)).Where(value => !string.IsNullOrEmpty(value)).ToArray();
            return parts.Length == 0 ? "-" : Fit(string.Join(", ", parts), maxLength);
        }

        private string PrimaryEffectText(CardDefinition card)
        {
            if (card == null || card.Effects == null || card.Effects.Count == 0)
            {
                return "No effect";
            }

            return Fit(EffectText(card.Effects[0], true), 34);
        }

        private string EffectText(Effect effect, bool detailed = false)
        {
            switch (effect.Kind)
            {
                case EffectKind.ProduceResource:
                    return detailed
                        ? "Produces " + ResourceName(effect.Resource) + " x" + Math.Max(1, effect.Amount)
                        : ResourceShort(effect.Resource) + "+" + Math.Max(1, effect.Amount);
                case EffectKind.ProduceRawChoice:
                    return detailed ? "Produces wood, clay, or stone" : "raw choice";
                case EffectKind.ProduceManufacturedChoice:
                    return detailed ? "Produces glass or papyrus" : "goods choice";
                case EffectKind.Coins:
                    return detailed ? "Gain " + effect.Amount + " coins" : "coin +" + effect.Amount;
                case EffectKind.Points:
                    return detailed ? effect.Amount + " victory points" : "VP " + effect.Amount;
                case EffectKind.Military:
                    return detailed ? "Move conflict " + effect.Amount : "military +" + effect.Amount;
                case EffectKind.Science:
                    return detailed ? "Science: " + ScienceName(effect.Science) : "science " + effect.Science;
                case EffectKind.DiscountRaw:
                    return detailed ? "Raw trade costs 1" : "raw trade 1";
                case EffectKind.DiscountManufactured:
                    return detailed ? "Goods trade costs 1" : "goods trade 1";
                case EffectKind.OpponentLoseCoins:
                    return detailed ? "Opponent loses " + effect.Amount + " coins" : "opponent -" + effect.Amount + " coin";
                case EffectKind.OpponentDiscardRaw:
                    return detailed ? "Discard opponent raw card" : "discard raw";
                case EffectKind.OpponentDiscardManufactured:
                    return detailed ? "Discard opponent goods card" : "discard goods";
                case EffectKind.BuildFromDiscard:
                    return detailed ? "Build a discarded card free" : "build discard";
                case EffectKind.ProgressFromRemoved:
                    return detailed ? "Choose a removed progress token" : "progress pick";
                case EffectKind.Economy:
                    return detailed ? "Opponent trade coins go to you" : "economy";
                case EffectKind.Strategy:
                    return detailed ? "Future military cards gain +1 shield" : "strategy";
                case EffectKind.Architecture:
                    return detailed ? "Future wonders cost 2 fewer resources" : "architecture";
                case EffectKind.Masonry:
                    return detailed ? "Future blue cards cost 2 fewer resources" : "masonry";
                case EffectKind.Theology:
                    return detailed ? "Future wonders give another turn" : "theology";
                case EffectKind.Urbanism:
                    return detailed ? "Linked builds gain 4 coins" : "urbanism";
                case EffectKind.RepeatTurn:
                    return "extra turn";
                case EffectKind.CoinsPerRaw:
                    return detailed ? "Gain coins per raw card x" + effect.Amount : "coin/raw x" + effect.Amount;
                case EffectKind.CoinsPerManufactured:
                    return detailed ? "Gain coins per goods card x" + effect.Amount : "coin/goods x" + effect.Amount;
                case EffectKind.CoinsPerCommercial:
                    return detailed ? "Gain coins per yellow card x" + effect.Amount : "coin/yellow x" + effect.Amount;
                case EffectKind.CoinsPerMilitary:
                    return detailed ? "Gain coins per red card x" + effect.Amount : "coin/red x" + effect.Amount;
                case EffectKind.CoinsPerWonder:
                    return detailed ? "Gain coins per wonder x" + effect.Amount : "coin/wonder x" + effect.Amount;
                case EffectKind.CoinsPerMostRawAndManufactured:
                    return detailed ? "Coins for most raw/goods cards" : "coin most resources";
                case EffectKind.CoinsPerMostCommercial:
                    return detailed ? "Coins for most yellow cards" : "coin most yellow";
                case EffectKind.CoinsPerMostMilitary:
                    return detailed ? "Coins for most red cards" : "coin most red";
                case EffectKind.CoinsPerMostScience:
                    return detailed ? "Coins for most green cards" : "coin most green";
                case EffectKind.CoinsPerMostCivilian:
                    return detailed ? "Coins for most blue cards" : "coin most blue";
                case EffectKind.CoinsPerMostWonder:
                    return detailed ? "Coins for most wonders" : "coin most wonders";
                case EffectKind.CoinsPerRichestThreeCoins:
                    return detailed ? "Coins per 3 coins in richest city" : "coin/richest";
                case EffectKind.PointsPerRaw:
                    return detailed ? "VP per raw card x" + effect.Amount : "VP/raw x" + effect.Amount;
                case EffectKind.PointsPerManufactured:
                    return detailed ? "VP per goods card x" + effect.Amount : "VP/goods x" + effect.Amount;
                case EffectKind.PointsPerCommercial:
                    return detailed ? "VP per trade card x" + effect.Amount : "VP/yellow x" + effect.Amount;
                case EffectKind.PointsPerMilitary:
                    return detailed ? "VP per military card x" + effect.Amount : "VP/red x" + effect.Amount;
                case EffectKind.PointsPerScience:
                    return detailed ? "VP per science card x" + effect.Amount : "VP/green x" + effect.Amount;
                case EffectKind.PointsPerWonder:
                    return detailed ? "VP per wonder x" + effect.Amount : "VP/wonder x" + effect.Amount;
                case EffectKind.PointsPerProgress:
                    return detailed ? "VP per progress token x" + effect.Amount : "VP/token x" + effect.Amount;
                case EffectKind.PointsPerThreeCoins:
                    return detailed ? "VP per 3 coins x" + effect.Amount : "VP/3 coins x" + effect.Amount;
                case EffectKind.PointsPerMostRawAndManufactured:
                    return detailed ? "VP for most raw/goods cards" : "VP most resources";
                case EffectKind.PointsPerMostCommercial:
                    return detailed ? "VP for most yellow cards" : "VP most yellow";
                case EffectKind.PointsPerMostMilitary:
                    return detailed ? "VP for most red cards" : "VP most red";
                case EffectKind.PointsPerMostScience:
                    return detailed ? "VP for most green cards" : "VP most green";
                case EffectKind.PointsPerMostCivilian:
                    return detailed ? "VP for most blue cards" : "VP most blue";
                case EffectKind.PointsPerMostWonder:
                    return detailed ? "VP for most wonders" : "VP most wonders";
                case EffectKind.PointsPerRichestThreeCoins:
                    return detailed ? "VP per 3 coins in richest city" : "VP/richest";
                default:
                    return "";
            }
        }

        private static string EffectChipLabel(Effect effect)
        {
            switch (effect.Kind)
            {
                case EffectKind.ProduceResource:
                    return ResourceName(effect.Resource);
                case EffectKind.ProduceRawChoice:
                    return "Raw ?";
                case EffectKind.ProduceManufacturedChoice:
                    return "Good ?";
                case EffectKind.Coins:
                    return "Coin " + Signed(effect.Amount);
                case EffectKind.Points:
                    return "VP " + effect.Amount;
                case EffectKind.Military:
                    return "War " + effect.Amount;
                case EffectKind.Science:
                    return "Sci";
                case EffectKind.DiscountRaw:
                    return "Raw 1";
                case EffectKind.DiscountManufactured:
                    return "Good 1";
                case EffectKind.OpponentLoseCoins:
                    return "Drain " + effect.Amount;
                case EffectKind.OpponentDiscardRaw:
                    return "Cut Raw";
                case EffectKind.OpponentDiscardManufactured:
                    return "Cut Good";
                case EffectKind.BuildFromDiscard:
                    return "Reuse";
                case EffectKind.ProgressFromRemoved:
                    return "Token";
                case EffectKind.Economy:
                case EffectKind.Strategy:
                case EffectKind.Architecture:
                case EffectKind.Masonry:
                case EffectKind.Theology:
                case EffectKind.Urbanism:
                    return effect.Kind.ToString();
                case EffectKind.RepeatTurn:
                    return "Turn";
                case EffectKind.CoinsPerRaw:
                case EffectKind.CoinsPerManufactured:
                case EffectKind.CoinsPerCommercial:
                case EffectKind.CoinsPerMilitary:
                case EffectKind.CoinsPerWonder:
                case EffectKind.CoinsPerMostRawAndManufactured:
                case EffectKind.CoinsPerMostCommercial:
                case EffectKind.CoinsPerMostMilitary:
                case EffectKind.CoinsPerMostScience:
                case EffectKind.CoinsPerMostCivilian:
                case EffectKind.CoinsPerMostWonder:
                case EffectKind.CoinsPerRichestThreeCoins:
                    return "Coin";
                case EffectKind.PointsPerRaw:
                case EffectKind.PointsPerManufactured:
                case EffectKind.PointsPerCommercial:
                case EffectKind.PointsPerMilitary:
                case EffectKind.PointsPerScience:
                case EffectKind.PointsPerWonder:
                case EffectKind.PointsPerProgress:
                case EffectKind.PointsPerThreeCoins:
                case EffectKind.PointsPerMostRawAndManufactured:
                case EffectKind.PointsPerMostCommercial:
                case EffectKind.PointsPerMostMilitary:
                case EffectKind.PointsPerMostScience:
                case EffectKind.PointsPerMostCivilian:
                case EffectKind.PointsPerMostWonder:
                case EffectKind.PointsPerRichestThreeCoins:
                    return "Score";
                default:
                    return "Effect";
            }
        }

        private static Color EffectChipColor(Effect effect)
        {
            switch (effect.Kind)
            {
                case EffectKind.ProduceResource:
                case EffectKind.ProduceRawChoice:
                case EffectKind.ProduceManufacturedChoice:
                    return new Color(0.38f, 0.26f, 0.16f, 0.92f);
                case EffectKind.Coins:
                case EffectKind.CoinsPerRaw:
                case EffectKind.CoinsPerManufactured:
                case EffectKind.CoinsPerCommercial:
                case EffectKind.CoinsPerMilitary:
                case EffectKind.CoinsPerWonder:
                case EffectKind.CoinsPerMostRawAndManufactured:
                case EffectKind.CoinsPerMostCommercial:
                case EffectKind.CoinsPerMostMilitary:
                case EffectKind.CoinsPerMostScience:
                case EffectKind.CoinsPerMostCivilian:
                case EffectKind.CoinsPerMostWonder:
                case EffectKind.CoinsPerRichestThreeCoins:
                    return new Color(0.62f, 0.46f, 0.14f, 0.92f);
                case EffectKind.Points:
                    return new Color(0.14f, 0.33f, 0.58f, 0.92f);
                case EffectKind.Military:
                case EffectKind.OpponentLoseCoins:
                case EffectKind.OpponentDiscardRaw:
                case EffectKind.OpponentDiscardManufactured:
                    return new Color(0.56f, 0.18f, 0.16f, 0.92f);
                case EffectKind.Science:
                case EffectKind.ProgressFromRemoved:
                    return new Color(0.16f, 0.43f, 0.24f, 0.92f);
                case EffectKind.DiscountRaw:
                case EffectKind.DiscountManufactured:
                case EffectKind.Economy:
                case EffectKind.Architecture:
                case EffectKind.Masonry:
                case EffectKind.Urbanism:
                    return new Color(0.52f, 0.4f, 0.12f, 0.92f);
                case EffectKind.RepeatTurn:
                case EffectKind.BuildFromDiscard:
                case EffectKind.Theology:
                case EffectKind.Strategy:
                    return new Color(0.35f, 0.28f, 0.56f, 0.92f);
                default:
                    return new Color(0.32f, 0.32f, 0.34f, 0.92f);
            }
        }

        private static string Signed(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }

        private static string ResourceBundleText(ResourceBundle bundle)
        {
            var parts = new List<string>();
            foreach (var resource in ResourceBundle.AllTypes)
            {
                var amount = bundle.Get(resource);
                if (amount > 0)
                {
                    parts.Add(ResourceName(resource) + " " + amount);
                }
            }

            return parts.Count == 0 ? "-" : string.Join(", ", parts.ToArray());
        }

        private static string ResourceSummary(PlayerState player)
        {
            var parts = new List<string>();
            var fixedResources = player.Resources.ToShortString();
            if (fixedResources != "-")
            {
                parts.Add(fixedResources);
            }

            if (player.RawChoiceProduction > 0)
            {
                parts.Add("Raw choice " + player.RawChoiceProduction);
            }

            if (player.ManufacturedChoiceProduction > 0)
            {
                parts.Add("Goods choice " + player.ManufacturedChoiceProduction);
            }

            return parts.Count == 0 ? "-" : Fit(string.Join(" | ", parts.ToArray()), 38);
        }

        private static string ResourceName(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood:
                    return "Wood";
                case ResourceType.Clay:
                    return "Clay";
                case ResourceType.Stone:
                    return "Stone";
                case ResourceType.Glass:
                    return "Glass";
                case ResourceType.Papyrus:
                    return "Papyrus";
                default:
                    return type.ToString();
            }
        }

        private static string ScienceName(ScienceSymbol symbol)
        {
            switch (symbol)
            {
                case ScienceSymbol.Wheel:
                    return "Wheel";
                case ScienceSymbol.Mortar:
                    return "Mortar";
                case ScienceSymbol.Sundial:
                    return "Sundial";
                case ScienceSymbol.Quill:
                    return "Quill";
                case ScienceSymbol.Globe:
                    return "Globe";
                case ScienceSymbol.Compass:
                    return "Compass";
                case ScienceSymbol.Law:
                    return "Law";
                default:
                    return symbol.ToString();
            }
        }

        private static string ResourceShort(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood:
                    return "W";
                case ResourceType.Clay:
                    return "C";
                case ResourceType.Stone:
                    return "S";
                case ResourceType.Glass:
                    return "G";
                case ResourceType.Papyrus:
                    return "P";
                default:
                    return "?";
            }
        }

        private string ScienceText(PlayerState player)
        {
            if (player.ScienceSymbols.Count == 0)
            {
                return "-";
            }

            return Fit(string.Join(", ", player.ScienceSymbols.Select(s => s.ToString()).ToArray()), 24);
        }

        private string WonderText(PlayerState player)
        {
            if (player.ReservedWonders.Count == 0)
            {
                return "-";
            }

            var lines = new List<string>();
            foreach (var wonderId in player.ReservedWonders)
            {
                if (!catalog.WondersById.TryGetValue(wonderId, out var wonder))
                {
                    continue;
                }

                var built = player.BuiltWonders.Contains(wonderId) ? "Built " : "";
                lines.Add(built + ShortWonderName(wonder.Name));
            }

            return string.Join("\n", lines.ToArray());
        }

        private string TokenText(PlayerState player)
        {
            if (player.ProgressTokens.Count == 0)
            {
                return "-";
            }

            return string.Join("\n", player.ProgressTokens.Select(id => catalog.ProgressById.ContainsKey(id) ? catalog.ProgressById[id].Name : id).ToArray());
        }

        private static string ShortWonderName(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 18)
            {
                return value;
            }

            return value.Substring(0, 17) + ".";
        }

        private static string Fit(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
            {
                return value;
            }

            return value.Substring(0, Math.Max(0, max - 1)) + ".";
        }

        private static string ColorLabel(CardColor color)
        {
            switch (color)
            {
                case CardColor.RawMaterial:
                    return "Raw Material";
                case CardColor.Manufactured:
                    return "Manufactured";
                case CardColor.Civilian:
                    return "Civilian";
                case CardColor.Science:
                    return "Science";
                case CardColor.Commercial:
                    return "Commercial";
                case CardColor.Military:
                    return "Military";
                case CardColor.Guild:
                    return "Guild";
                default:
                    return color.ToString();
            }
        }

        private static Color CardColorValue(CardColor color)
        {
            switch (color)
            {
                case CardColor.RawMaterial:
                    return new Color(0.48f, 0.3f, 0.16f);
                case CardColor.Manufactured:
                    return new Color(0.5f, 0.51f, 0.52f);
                case CardColor.Civilian:
                    return new Color(0.14f, 0.36f, 0.64f);
                case CardColor.Science:
                    return new Color(0.15f, 0.45f, 0.25f);
                case CardColor.Commercial:
                    return new Color(0.65f, 0.5f, 0.15f);
                case CardColor.Military:
                    return new Color(0.58f, 0.18f, 0.15f);
                case CardColor.Guild:
                    return new Color(0.42f, 0.28f, 0.58f);
                default:
                    return Color.gray;
            }
        }

        private static Color ResourceColor(ResourceType type, float alpha = 0.94f)
        {
            switch (type)
            {
                case ResourceType.Wood:
                    return new Color(0.34f, 0.22f, 0.12f, alpha);
                case ResourceType.Clay:
                    return new Color(0.58f, 0.25f, 0.14f, alpha);
                case ResourceType.Stone:
                    return new Color(0.42f, 0.43f, 0.42f, alpha);
                case ResourceType.Glass:
                    return new Color(0.18f, 0.45f, 0.48f, alpha);
                case ResourceType.Papyrus:
                    return new Color(0.66f, 0.56f, 0.34f, alpha);
                default:
                    return new Color(0.25f, 0.25f, 0.25f, alpha);
            }
        }

        private RectTransform Glow(Transform parent, string name, Rect anchor, Color color, float phase)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            SetRect(rect, anchor);
            var image = go.GetComponent<Image>();
            image.sprite = glowSprite;
            image.color = color;
            image.raycastTarget = false;
            var pulse = go.AddComponent<UiPulseGlow>();
            pulse.BaseColor = color;
            pulse.Phase = phase;
            return rect;
        }

        private static Texture2D CreateGlowTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(1f - distance);
                    alpha = alpha * alpha * alpha;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private void ClearRoot()
        {
            if (root == null)
            {
                return;
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        private Rect Anchor(float xMin, float yMin, float xMax, float yMax)
        {
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private RectTransform Panel(Transform parent, string name, Rect anchor, Color color, Color outline, float outlineSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            if (anchor != default(Rect))
            {
                SetRect(rect, anchor);
            }

            var image = go.GetComponent<Image>();
            image.color = color;

            if (outlineSize > 0f)
            {
                var border = go.AddComponent<Outline>();
                border.effectColor = outline;
                border.effectDistance = new Vector2(outlineSize, -outlineSize);
            }

            go.AddComponent<UiAppearAnimator>();

            return rect;
        }

        private RectTransform DecorativePanel(Transform parent, string name, Rect anchor, Color color, Color outline, float outlineSize)
        {
            var rect = Panel(parent, name, anchor, color, outline, outlineSize);
            var image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }

            return rect;
        }

        private void AddGlassChrome(RectTransform parent, Color glowColor)
        {
            DecorativePanel(parent, "Glass Top Wash", Anchor(0.018f, 0.78f, 0.982f, 0.985f), new Color(1f, 0.94f, 0.68f, 0.035f), new Color(0, 0, 0, 0), 0f);
            DecorativePanel(parent, "Glass Bottom Shade", Anchor(0.018f, 0.015f, 0.982f, 0.18f), new Color(0f, 0f, 0f, 0.075f), new Color(0, 0, 0, 0), 0f);
            DecorativePanel(parent, "Glass Left Edge", Anchor(0.012f, 0.035f, 0.018f, 0.965f), new Color(1f, 0.92f, 0.64f, 0.28f), new Color(0, 0, 0, 0), 0f);
            DecorativePanel(parent, "Glass Right Edge", Anchor(0.982f, 0.035f, 0.988f, 0.965f), new Color(0f, 0f, 0f, 0.09f), new Color(0, 0, 0, 0), 0f);
            DecorativePanel(parent, "Glass Center Sheen", Anchor(0.06f, 0.47f, 0.94f, 0.505f), new Color(1f, 1f, 1f, 0.045f), new Color(0, 0, 0, 0), 0f);
            Glow(parent, "Glass Soft Glow", Anchor(-0.08f, -0.14f, 1.08f, 1.14f), glowColor, 0.19f).SetAsFirstSibling();
        }

        private Text Text(Transform parent, string value, int size, TextAnchor alignment, Color color, Rect anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            SetRect(rect, anchor);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.text = value ?? "";
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Math.Min(size, Math.Max(8, size - 8));
            text.resizeTextMaxSize = size;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            var shadow = go.AddComponent<Shadow>();
            var luminance = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
            shadow.effectColor = luminance > 0.45f ? new Color(0f, 0f, 0f, 0.58f) : new Color(1f, 0.92f, 0.68f, 0.22f);
            shadow.effectDistance = new Vector2(1.2f, -1.2f);
            return text;
        }

        private Button StyledButton(Transform parent, string label, Action clicked, Color color, Rect anchor, int textSize = BaseTextSize)
        {
            var glassColor = new Color(color.r, color.g, color.b, 0.46f);
            var button = FramedButton(parent, "Button " + label, clicked, glassColor, Color.Lerp(color, Color.white, 0.45f), anchor);
            AddButtonChrome(button.GetComponent<RectTransform>(), color);
            Text(button.transform, label, textSize, TextAnchor.MiddleCenter, Color.white, Anchor(0.06f, 0.1f, 0.94f, 0.9f));
            return button;
        }

        private void AddButtonChrome(RectTransform parent, Color color)
        {
            DecorativePanel(parent, "Button Drop Shade", Anchor(0.025f, 0.02f, 0.975f, 0.18f), new Color(0f, 0f, 0f, 0.22f), new Color(0, 0, 0, 0), 0f);
            DecorativePanel(parent, "Button Top Shine", Anchor(0.035f, 0.62f, 0.965f, 0.94f), new Color(1f, 1f, 1f, 0.1f), new Color(0, 0, 0, 0), 0f);
            DecorativePanel(parent, "Button Gold Edge", Anchor(0.045f, 0.08f, 0.955f, 0.13f), new Color(0.96f, 0.77f, 0.38f, 0.32f), new Color(0, 0, 0, 0), 0f);
            DecorativePanel(parent, "Button Glass Line", Anchor(0.05f, 0.88f, 0.95f, 0.93f), new Color(1f, 1f, 1f, 0.18f), new Color(0, 0, 0, 0), 0f);
        }

        private Button FramedButton(Transform parent, string name, Action clicked, Color color, Color borderColor, Rect anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            if (anchor != default(Rect))
            {
                SetRect(rect, anchor);
            }

            var image = go.GetComponent<Image>();
            image.color = color;
            var outline = go.GetComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2f, -2f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => clicked?.Invoke());
            button.colors = ButtonColors(color);
            var hover = go.AddComponent<UiHoverAnimator>();
            hover.Target = rect;
            hover.Graphic = image;
            hover.BaseColor = color;
            return button;
        }

        private static ColorBlock ButtonColors(Color normal)
        {
            return new ColorBlock
            {
                normalColor = normal,
                highlightedColor = Color.Lerp(normal, Color.white, 0.18f),
                pressedColor = Color.Lerp(normal, Color.black, 0.18f),
                selectedColor = Color.Lerp(normal, Color.white, 0.1f),
                disabledColor = new Color(normal.r * 0.45f, normal.g * 0.45f, normal.b * 0.45f, 0.52f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
        }

        private void AddInput(Transform parent, string label, string value, Action<string> changed, Rect anchor)
        {
            var group = Panel(parent, "Input " + label, anchor, new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), 0f);
            Text(group, label, 14, TextAnchor.UpperLeft, Gold, Anchor(0, 0.58f, 1, 1));

            var field = Panel(group, "Input Field", Anchor(0, 0, 1, 0.58f), new Color(0.035f, 0.04f, 0.045f, 0.22f), new Color(0.86f, 0.68f, 0.34f, 0.58f), 1f);
            DecorativePanel(field, "Input Shine", Anchor(0.02f, 0.56f, 0.98f, 0.95f), new Color(1f, 1f, 1f, 0.06f), new Color(0, 0, 0, 0), 0f);
            var input = field.gameObject.AddComponent<InputField>();
            input.targetGraphic = field.GetComponent<Image>();

            var text = Text(field, value, 18, TextAnchor.MiddleLeft, Color.white, Anchor(0, 0, 1, 1));
            text.resizeTextForBestFit = false;
            text.rectTransform.offsetMin = new Vector2(14, 0);
            text.rectTransform.offsetMax = new Vector2(-14, 0);
            input.textComponent = text;
            input.placeholder = null;
            input.text = value;
            input.caretColor = Color.white;
            input.selectionColor = new Color(0.86f, 0.68f, 0.34f, 0.45f);
            input.onValueChanged.AddListener(v => changed?.Invoke(v));
        }

        private static void SetRect(RectTransform rect, Rect anchor)
        {
            rect.anchorMin = new Vector2(anchor.xMin, anchor.yMin);
            rect.anchorMax = new Vector2(anchor.xMax, anchor.yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public class UiAppearAnimator : MonoBehaviour
    {
        private CanvasGroup group;
        private RectTransform rect;
        private float age;

        private void Awake()
        {
            rect = transform as RectTransform;
            group = gameObject.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }

            group.alpha = 0f;
            if (rect != null)
            {
                rect.localScale = Vector3.one * 0.985f;
            }
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(age / 0.18f);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            group.alpha = eased;
            if (rect != null)
            {
                rect.localScale = Vector3.LerpUnclamped(Vector3.one * 0.985f, Vector3.one, eased);
            }

            if (t >= 1f)
            {
                Destroy(this);
            }
        }
    }

    public class UiHoverAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public RectTransform Target;
        public Graphic Graphic;
        public Color BaseColor;

        private bool hovered;
        private bool pressed;

        private void Awake()
        {
            if (Target == null)
            {
                Target = transform as RectTransform;
            }
        }

        private void Update()
        {
            if (Target != null)
            {
                var scale = pressed ? 0.97f : hovered ? 1.055f : 1f;
                Target.localScale = Vector3.Lerp(Target.localScale, Vector3.one * scale, Time.unscaledDeltaTime * 14f);
            }

            if (Graphic != null)
            {
                var targetColor = pressed ? Color.Lerp(BaseColor, Color.black, 0.16f) : hovered ? Color.Lerp(BaseColor, Color.white, 0.16f) : BaseColor;
                Graphic.color = Color.Lerp(Graphic.color, targetColor, Time.unscaledDeltaTime * 16f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
        }
    }

    public class UiFloatAnimator : MonoBehaviour
    {
        public float Amplitude = 2f;
        public float Speed = 1.6f;

        private RectTransform rect;
        private Vector2 basePosition;
        private float phase;

        private void Awake()
        {
            rect = transform as RectTransform;
            if (rect != null)
            {
                basePosition = rect.anchoredPosition;
            }

            phase = transform.GetSiblingIndex() * 0.37f;
        }

        private void Update()
        {
            if (rect == null)
            {
                return;
            }

            rect.anchoredPosition = basePosition + new Vector2(0f, Mathf.Sin(Time.unscaledTime * Speed + phase) * Amplitude);
        }
    }

    public class UiMoteAnimator : MonoBehaviour
    {
        public float Phase;
        public Vector2 Drift = new Vector2(4f, 3f);

        private RectTransform rect;
        private Image image;
        private Color baseColor;

        private void Awake()
        {
            rect = transform as RectTransform;
            image = GetComponent<Image>();
            if (image != null)
            {
                baseColor = image.color;
            }
        }

        private void Update()
        {
            var wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 0.75f + Phase * 6.28f);
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(
                    Mathf.Sin(Time.unscaledTime * 0.36f + Phase) * Drift.x,
                    Mathf.Cos(Time.unscaledTime * 0.29f + Phase * 1.7f) * Drift.y);
            }

            if (image != null)
            {
                image.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * Mathf.Lerp(0.35f, 1f, wave));
            }
        }
    }

    public class UiPulseGlow : MonoBehaviour
    {
        public Color BaseColor;
        public float Phase;

        private Image image;
        private RectTransform rect;
        private Vector3 baseScale;

        private void Awake()
        {
            image = GetComponent<Image>();
            rect = transform as RectTransform;
            baseScale = rect != null ? rect.localScale : Vector3.one;
        }

        private void Update()
        {
            var wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.2f + Phase * 10f);
            if (image != null)
            {
                image.color = new Color(BaseColor.r, BaseColor.g, BaseColor.b, BaseColor.a * Mathf.Lerp(0.62f, 1f, wave));
            }

            if (rect != null)
            {
                rect.localScale = baseScale * Mathf.Lerp(0.98f, 1.04f, wave);
            }
        }
    }

    public class SlowBackdropDrift : MonoBehaviour
    {
        private RawImage image;

        private void Awake()
        {
            image = GetComponent<RawImage>();
        }

        private void Update()
        {
            if (image == null)
            {
                return;
            }

            var x = Mathf.Sin(Time.unscaledTime * 0.045f) * 0.006f;
            var y = Mathf.Cos(Time.unscaledTime * 0.038f) * 0.006f;
            image.uvRect = new Rect(x, y, 1f, 1f);
        }
    }
}
