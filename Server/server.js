const http = require("http");
const crypto = require("crypto");

const port = Number(process.env.PORT || 3000);
const rooms = new Map();
let waitingRoomId = null;

function makeId(bytes = 12) {
  return crypto.randomBytes(bytes).toString("hex");
}

function makeCode() {
  const alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
  let code = "";
  for (let i = 0; i < 5; i++) {
    code += alphabet[Math.floor(Math.random() * alphabet.length)];
  }
  return code;
}

function createRoom(playerName, isPrivate) {
  let code = makeCode();
  while ([...rooms.values()].some((room) => room.code === code)) {
    code = makeCode();
  }

  const playerId = makeId();
  const room = {
    id: makeId(),
    code,
    isPrivate,
    seed: Math.floor(Math.random() * 2147483647),
    createdAt: Date.now(),
    updatedAt: Date.now(),
    started: false,
    players: [
      {
        id: playerId,
        name: playerName || "Player 1",
        index: 0
      }
    ],
    actions: []
  };

  rooms.set(room.id, room);
  return { room, playerId, playerIndex: 0 };
}

function joinRoom(room, playerName) {
  if (!room || room.players.length >= 2) {
    return null;
  }

  const playerId = makeId();
  room.players.push({
    id: playerId,
    name: playerName || "Player 2",
    index: 1
  });
  room.started = true;
  room.updatedAt = Date.now();
  return { room, playerId, playerIndex: 1 };
}

function publicSnapshot(room, playerId = null) {
  const player = room.players.find((candidate) => candidate.id === playerId);
  return {
    roomId: room.id,
    roomCode: room.code,
    playerId,
    playerIndex: player ? player.index : -1,
    seed: room.seed,
    started: room.started,
    players: room.players.map((candidate) => ({
      name: candidate.name,
      index: candidate.index
    })),
    actions: room.actions
  };
}

function readBody(request) {
  return new Promise((resolve, reject) => {
    let body = "";
    request.on("data", (chunk) => {
      body += chunk;
      if (body.length > 1024 * 128) {
        request.destroy();
        reject(new Error("Request body is too large."));
      }
    });
    request.on("end", () => {
      if (!body) {
        resolve({});
        return;
      }

      try {
        resolve(JSON.parse(body));
      } catch (error) {
        reject(error);
      }
    });
    request.on("error", reject);
  });
}

function send(response, status, payload) {
  response.writeHead(status, {
    "content-type": "application/json; charset=utf-8",
    "access-control-allow-origin": "*",
    "access-control-allow-methods": "GET,POST,OPTIONS",
    "access-control-allow-headers": "content-type"
  });
  response.end(JSON.stringify(payload));
}

function findRoomByCode(code) {
  const normalized = String(code || "").trim().toUpperCase();
  return [...rooms.values()].find((room) => room.code === normalized);
}

function pruneRooms() {
  const now = Date.now();
  const maxAgeMs = 1000 * 60 * 60 * 8;
  for (const [id, room] of rooms.entries()) {
    if (now - room.updatedAt > maxAgeMs) {
      rooms.delete(id);
      if (waitingRoomId === id) {
        waitingRoomId = null;
      }
    }
  }
}

async function handle(request, response) {
  if (request.method === "OPTIONS") {
    send(response, 204, {});
    return;
  }

  const url = new URL(request.url, `http://${request.headers.host}`);
  pruneRooms();

  try {
    if (request.method === "GET" && url.pathname === "/health") {
      send(response, 200, { ok: true, rooms: rooms.size });
      return;
    }

    if (request.method === "POST" && url.pathname === "/api/matchmake") {
      const body = await readBody(request);
      if (waitingRoomId) {
        const waitingRoom = rooms.get(waitingRoomId);
        if (waitingRoom && waitingRoom.players.length === 1) {
          waitingRoomId = null;
          const joined = joinRoom(waitingRoom, body.playerName);
          send(response, 200, publicSnapshot(joined.room, joined.playerId));
          return;
        }

        waitingRoomId = null;
      }

      const created = createRoom(body.playerName, false);
      waitingRoomId = created.room.id;
      send(response, 200, publicSnapshot(created.room, created.playerId));
      return;
    }

    if (request.method === "POST" && url.pathname === "/api/rooms") {
      const body = await readBody(request);
      const created = createRoom(body.playerName, true);
      send(response, 200, publicSnapshot(created.room, created.playerId));
      return;
    }

    const joinMatch = url.pathname.match(/^\/api\/rooms\/([^/]+)\/join$/);
    if (request.method === "POST" && joinMatch) {
      const body = await readBody(request);
      const room = findRoomByCode(joinMatch[1]);
      const joined = joinRoom(room, body.playerName);
      if (!joined) {
        send(response, 409, { error: "Room is missing or full." });
        return;
      }

      send(response, 200, publicSnapshot(joined.room, joined.playerId));
      return;
    }

    const actionMatch = url.pathname.match(/^\/api\/rooms\/([^/]+)\/actions$/);
    if (request.method === "POST" && actionMatch) {
      const body = await readBody(request);
      const room = rooms.get(actionMatch[1]);
      if (!room) {
        send(response, 404, { error: "Room not found." });
        return;
      }

      const player = room.players.find((candidate) => candidate.id === body.playerId);
      if (!player) {
        send(response, 403, { error: "Unknown player token." });
        return;
      }

      if (!room.started) {
        send(response, 409, { error: "Waiting for another player." });
        return;
      }

      if (body.sequence !== room.actions.length) {
        send(response, 409, {
          error: "Action sequence mismatch.",
          expected: room.actions.length,
          snapshot: publicSnapshot(room, body.playerId)
        });
        return;
      }

      const action = body.action || {};
      if (typeof action.PlayerIndex === "number" && action.PlayerIndex !== player.index) {
        send(response, 403, { error: "Action player does not match token." });
        return;
      }

      room.actions.push(action);
      room.updatedAt = Date.now();
      send(response, 200, publicSnapshot(room, body.playerId));
      return;
    }

    const roomMatch = url.pathname.match(/^\/api\/rooms\/([^/]+)$/);
    if (request.method === "GET" && roomMatch) {
      const room = rooms.get(roomMatch[1]);
      if (!room) {
        send(response, 404, { error: "Room not found." });
        return;
      }

      send(response, 200, publicSnapshot(room, url.searchParams.get("playerId")));
      return;
    }

    send(response, 404, { error: "Not found." });
  } catch (error) {
    send(response, 500, { error: error.message || "Server error." });
  }
}

http.createServer(handle).listen(port, () => {
  console.log(`Duel match server listening on ${port}`);
});
