# Online Duel Game Setup

This Unity project now boots directly into a playable two-player duel client. The client has:

- local hotseat play
- HTTP matchmaking
- private room codes
- wonder drafting
- three ages of card structures
- build, discard, and build-wonder actions
- science progress-token choices
- military, science, and final-score victories
- generated original card frames and placeholder card/wonder data

The project does not include copied publisher artwork. Add any private assets you own later by replacing the generated card visuals in `Assets/Scripts/SevenWondersDuel/UI/DuelGameApp.cs`.

## Run Locally

1. Start the match server:

   ```powershell
   cd Server
   npm start
   ```

2. In Unity, press Play.

3. For local testing, choose `Local Hotseat`.

4. For online testing, keep the lobby server URL as:

   ```text
   http://localhost:3000
   ```

5. Open a second Unity player/editor instance and use `Find Match`, or create a private room and join with the displayed code.

## Deploy To Railway

1. Create a Railway service from this GitHub repo.

2. Railway can deploy from the repo root. The root `package.json` and `railway.json` start the match server with:

   ```bash
   npm start
   ```

3. Generate or copy the Railway public URL.

4. In the Unity lobby, paste the Railway URL into `Server URL`.

The server is in-memory. Restarting the Railway service clears active rooms, which is fine for private prototype play. For a public release you would add persistence, reconnect tokens, server-side rule validation, and moderation tools.

## Main Files

- `Assets/Scripts/SevenWondersDuel/Core/DuelTypes.cs`
- `Assets/Scripts/SevenWondersDuel/Core/DuelCatalog.cs`
- `Assets/Scripts/SevenWondersDuel/Core/DuelReducer.cs`
- `Assets/Scripts/SevenWondersDuel/Online/DuelOnlineClient.cs`
- `Assets/Scripts/SevenWondersDuel/UI/DuelGameApp.cs`
- `Server/server.js`
