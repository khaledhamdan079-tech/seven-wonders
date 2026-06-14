using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SevenWondersDuel.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace SevenWondersDuel.Online
{
    [Serializable]
    public class ServerPlayerDto
    {
        public string name;
        public int index;
    }

    [Serializable]
    public class RoomSnapshotDto
    {
        public string roomId;
        public string roomCode;
        public string playerId;
        public int playerIndex;
        public int seed;
        public bool started;
        public List<ServerPlayerDto> players = new List<ServerPlayerDto>();
        public List<DuelAction> actions = new List<DuelAction>();
        public string error;
    }

    [Serializable]
    public class MatchRequestDto
    {
        public string playerName;
    }

    [Serializable]
    public class SubmitActionDto
    {
        public string playerId;
        public int sequence;
        public DuelAction action;
    }

    [Serializable]
    public class ErrorDto
    {
        public string error;
    }

    public class DuelOnlineClient : MonoBehaviour
    {
        public const string DefaultBaseUrl = "https://seven-wonders-production.up.railway.app";

        public string BaseUrl = DefaultBaseUrl;
        public RoomSnapshotDto Snapshot { get; private set; }
        public string LastError { get; private set; }

        public int ActionCount
        {
            get { return Snapshot != null && Snapshot.actions != null ? Snapshot.actions.Count : 0; }
        }

        public IEnumerator Matchmake(string playerName, Action<RoomSnapshotDto> completed)
        {
            yield return Post("/api/matchmake", new MatchRequestDto { playerName = playerName }, completed);
        }

        public IEnumerator CreatePrivateRoom(string playerName, Action<RoomSnapshotDto> completed)
        {
            yield return Post("/api/rooms", new MatchRequestDto { playerName = playerName }, completed);
        }

        public IEnumerator JoinPrivateRoom(string code, string playerName, Action<RoomSnapshotDto> completed)
        {
            var safeCode = UnityWebRequest.EscapeURL((code ?? string.Empty).Trim().ToUpperInvariant());
            yield return Post("/api/rooms/" + safeCode + "/join", new MatchRequestDto { playerName = playerName }, completed);
        }

        public IEnumerator PollRoom(Action<RoomSnapshotDto> completed)
        {
            if (Snapshot == null || string.IsNullOrEmpty(Snapshot.roomId))
            {
                LastError = "No online room has been joined.";
                completed?.Invoke(null);
                yield break;
            }

            var path = "/api/rooms/" + Snapshot.roomId + "?playerId=" + UnityWebRequest.EscapeURL(Snapshot.playerId);
            yield return Get(path, completed);
        }

        public IEnumerator SubmitAction(DuelAction action, Action<RoomSnapshotDto> completed)
        {
            if (Snapshot == null || string.IsNullOrEmpty(Snapshot.roomId))
            {
                LastError = "No online room has been joined.";
                completed?.Invoke(null);
                yield break;
            }

            var body = new SubmitActionDto
            {
                playerId = Snapshot.playerId,
                sequence = ActionCount,
                action = action
            };
            yield return Post("/api/rooms/" + Snapshot.roomId + "/actions", body, completed);
        }

        private IEnumerator Get(string path, Action<RoomSnapshotDto> completed)
        {
            using (var request = UnityWebRequest.Get(Combine(BaseUrl, path)))
            {
                yield return request.SendWebRequest();
                HandleSnapshotResponse(request, completed);
            }
        }

        private IEnumerator Post<T>(string path, T payload, Action<RoomSnapshotDto> completed)
        {
            var json = JsonUtility.ToJson(payload);
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            using (var request = new UnityWebRequest(Combine(BaseUrl, path), "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();
                HandleSnapshotResponse(request, completed);
            }
        }

        private void HandleSnapshotResponse(UnityWebRequest request, Action<RoomSnapshotDto> completed)
        {
            var body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            if (request.result != UnityWebRequest.Result.Success)
            {
                LastError = ParseError(body, request.error);
                completed?.Invoke(null);
                return;
            }

            try
            {
                var snapshot = JsonUtility.FromJson<RoomSnapshotDto>(body);
                if (snapshot == null)
                {
                    LastError = "Server returned an empty response.";
                    completed?.Invoke(null);
                    return;
                }

                Snapshot = snapshot;
                LastError = string.Empty;
                completed?.Invoke(snapshot);
            }
            catch (Exception exception)
            {
                LastError = "Could not parse server response: " + exception.Message;
                completed?.Invoke(null);
            }
        }

        private static string ParseError(string body, string fallback)
        {
            if (!string.IsNullOrEmpty(body))
            {
                try
                {
                    var error = JsonUtility.FromJson<ErrorDto>(body);
                    if (error != null && !string.IsNullOrEmpty(error.error))
                    {
                        return error.error;
                    }
                }
                catch
                {
                    // The HTTP status already tells the UI this failed; fallback keeps the message useful.
                }
            }

            return string.IsNullOrEmpty(fallback) ? "Network request failed." : fallback;
        }

        private static string Combine(string baseUrl, string path)
        {
            var root = NormalizeBaseUrl(baseUrl);
            return root + path;
        }

        public static string NormalizeBaseUrl(string baseUrl)
        {
            var root = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.Trim();
            if (!root.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !root.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                root = "https://" + root;
            }

            return root.TrimEnd('/');
        }
    }
}
