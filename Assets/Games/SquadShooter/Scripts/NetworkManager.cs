using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        public static NetworkManager Instance { get; private set; }
        public NetworkRunner runner;
        public GameObject tileManagerPrefab;
        public GameObject playerPrefab;

        private NetworkSceneManagerDefault sceneManager;

        private bool hasStartedSession = false;
        private bool waitingForRoom = false;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        public async void StartGame(GameMode mode)
        {
            if (runner == null)
            {
                runner = gameObject.AddComponent<NetworkRunner>();
                runner.ProvideInput = mode == GameMode.Shared;
                runner.AddCallbacks(this);
                sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
            }

            var result = await runner.JoinSessionLobby(SessionLobby.ClientServer);
            if (result.Ok)
            {
                Debug.Log("[NetworkManager] ✅ Joined Fusion lobby.");
            }
            else
            {
                Debug.LogError($"[NetworkManager] ❌ Failed to join lobby: {result.ShutdownReason}");
                string fallback = $"Room_{UnityEngine.Random.Range(1000, 9999)}";
                JoinOrCreateSession(fallback);
            }
        }

        public async void JoinOrCreateSession(string sessionName)
        {
            // int totalPlayers = GameManager.Instance.totalPlayers;

            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName,
                //    PlayerCount = runner.,
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
                IsOpen = true,
                IsVisible = true,
                SceneManager = sceneManager,
                SessionProperties = new Dictionary<string, SessionProperty>
            {
                { "IsGameStarted", false }
            }
            });

            if (result.Ok)
            {

                Debug.Log($"[NetworkManager] ✅ Session started: {sessionName}");
            }
            else
            {
                Debug.LogError($"[NetworkManager] ❌ Failed to start session: {result.ShutdownReason}");
            }
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            Debug.Log($"[NetworkManager] Session list updated. Found: {sessionList.Count}");

            foreach (var session in sessionList)
            {
                // Skip full or closed sessions
                if (session.PlayerCount >= session.MaxPlayers || !session.IsOpen)
                    continue;

                // Check if session is marked as started
                if (session.Properties.TryGetValue("IsGameStarted", out var isStartedProp))
                {
                    if ((bool)isStartedProp == true)
                    {
                        Debug.Log($"[NetworkManager] Skipping started session: {session.Name}");
                        continue;
                    }
                }

                Debug.Log($"[NetworkManager] Joining valid session: {session.Name}");
                hasStartedSession = true;
                JoinOrCreateSession(session.Name);
                return;
            }

            if (!hasStartedSession && !waitingForRoom)
            {
                StartCoroutine(DelayedRoomCreation());
            }
        }

        private IEnumerator DelayedRoomCreation()
        {
            waitingForRoom = true;
            yield return new WaitForSeconds(2f);

            if (!hasStartedSession)
            {
                string sessionName = $"Room_{UnityEngine.Random.Range(1000, 9999)}";
                Debug.Log($"[NetworkManager] ➕ Creating new session: {sessionName}");
                hasStartedSession = true;
                JoinOrCreateSession(sessionName);
            }
        }
        public int MAXPLAYER;
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[NetworkManager] Player {player.PlayerId} joined.");

            // Only spawn the prefab on the host:
            if (player == runner.LocalPlayer)
            {
                //// Enforce max‐players:
                //if (MAXPLAYER < )
                //{
                //    Vector3 spawnPos = Vector3.zero; // or your spawn‐logic
                //    Quaternion rot = Quaternion.identity;

                //    // Spawn the networked PlayerManager prefab
                //    var netObj = runner.Spawn(
                //        GameManager.Instance.GetPlayerPrefab().GetComponent<NetworkObject>(),
                //        spawnPos,
                //        rot,
                //        player // gives input authority
                //    );

                //    // Assign a unique ID on the host
                //    // int id = GameManager.Instance.AssignPlayerId();

                //    // // Call our new RPC to broadcast it
                //    // var pm = netObj.GetComponent<PlayerManager>();
                //    // pm.RPC_SetPlayerId(id);

                //    // Register with GameManager
                //    // GameManager.Instance.allPlayerManagers[id] = pm;
                //}
                //else
                //{
                //    Debug.LogWarning("[NetworkManager] Max players reached. Rejecting join.");
                //    runner.Disconnect(player);
                //}
            }
        }


        private IEnumerator DisconnectLatePlayerNextFrame(PlayerRef player)
        {
            yield return null;
            runner.Disconnect(player);
            Debug.Log($"[NetworkManager] 🚫 Disconnected late joiner: Player {player.PlayerId}");
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) => Debug.Log($"[NetworkManager] Player {player.PlayerId} left.");

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            //if (MatchmakingManager.Instance != null && MatchmakingManager.Instance.matchmakingActive == false)
            //{
            //    request.Refuse();
            //    Debug.LogWarning("[NetworkManager] ❌ Refused join: game already started.");
            //}
            //else
            //{
            //    request.Accept();
            //}
        }

        // Fusion required callbacks (no-op)
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken migrationToken) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    }
}