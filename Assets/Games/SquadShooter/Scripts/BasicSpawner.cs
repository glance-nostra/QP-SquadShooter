using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
    {
        private NetworkRunner _runner;

        [SerializeField] GameController gameManager;
        [SerializeField] Joystick joystick;


        [SerializeField] private NetworkPrefabRef _playerPrefab;
        [SerializeField] private NetworkPrefabRef _botPrefab;
        private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new();
        public Transform[] spwnpoint;
        public string RoomCode { get; private set; }  // Expose this to show in UI

        private const int MaxPlayers = 2;

        private bool gameStarted = false;
        [SerializeField] GameObject obj;

        private NetworkSceneManagerDefault sceneManager;
        //[SerializeField] GameObject _uiPanel;
        public bool createdroom;

        private bool hasStartedSession = false;
        private bool waitingForRoom = false;
        public void JoinranodmRoom()
        {
            createdroom = false;
            string randomRoomCode = "Room_" + UnityEngine.Random.Range(1000, 9999);
            StartGame(GameMode.Shared);
            gameManager.StatsText.text = "Trying random room...";
        }
        public void CreateRoom()
        {
            createdroom = true;
            string randomRoomCode = GenerateRoomCode();
            StartGame(GameMode.Shared);
            gameManager.StatsText.text = "creating room...";
        }
        public void CreatedJoinedroom()
        {
            createdroom = true;
            StartGame(GameMode.Shared);
            // JoinSessiongame(gameManager.roomCodeInput.text);

        }
        private void StartActualGame() //start the Game
        {

            gameStarted = true;
            Debug.Log("All players joined. Starting Game!");
            var SessionProperties = new Dictionary<string, SessionProperty>
            {
                { "CreatedRoom", createdroom },
                { "GameStarted",true },



            };
            _runner.SessionInfo.UpdateCustomProperties(SessionProperties);

            gameManager.StartGame();
            StartCoroutine(gameManager.StartGameAnim());
        }
        public void OnObjectSpawned(NetworkObject obj)
        {
            Debug.Log("Spawoned");
            // Check if it's a player
            if (obj.CompareTag("Player")) // Ensure your player prefab has the "Player" tag
            {
                Debug.Log("Object Spawned with tag Player: " + obj.name);

                if (!gameManager.allcharacter.Contains(obj.gameObject.GetComponent<Entity>()))
                {
                    gameManager.allcharacter.Add(obj.gameObject.GetComponent<Entity>());
                }
            }
        }



        public async void StartGame(GameMode mode)
        {
            if (_runner == null)
            {
                gameManager.StatsText.text = "Connecting";
                _runner = gameObject.AddComponent<NetworkRunner>();
                _runner.ProvideInput = mode == GameMode.Shared;
                _runner.AddCallbacks(this);
                sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
            }

            var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
            if (result.Ok)
            {
                Debug.Log("[NetworkManager] ✅ Joined Fusion lobby.");
            }
            else
            {
                Debug.LogError($"[NetworkManager] ❌ Failed to join lobby: {result.ShutdownReason}");
                string fallback = $"Room_{UnityEngine.Random.Range(1000, 9999)}";
                if (createdroom)
                {
                    fallback = GenerateRoomCode();
                    gameManager.displayRoomCode.text = fallback;
                }
                if (gameManager.roomCodeInput.text.Length <= 1)
                    JoinOrCreateSession(fallback);
                else
                {
                    JoinOrCreateSession(gameManager.roomCodeInput.text);
                }
            }
        }
        public async void JoinOrCreateSession(string sessionName)
        {
            // int totalPlayers = GameManager.Instance.totalPlayers;

            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,

                SessionName = sessionName,
                PlayerCount = MaxPlayers,
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
                IsOpen = true,
                IsVisible = true,
                SceneManager = sceneManager,
                SessionProperties = new Dictionary<string, SessionProperty>
            {
                { "CreatedRoom", createdroom },
                { "GameStarted",false },



            }
            });

            if (result.Ok)
            {
                lastSessionName = sessionName;
                gameManager._runner = _runner;
                LeaveButton.SetActive(true);
                joinButton.SetActive(false);
                Debug.Log($"[NetworkManager] ✅ Session started: {sessionName}");
            }
            else
            {
                Debug.LogError($"[NetworkManager] ❌ Failed to start session: {result.ShutdownReason}");
            }
        }

        public async void JoinSessiongame(string sessionName)
        {
            lastSessionName = sessionName;

            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName,

                PlayerCount = MaxPlayers,
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
                IsOpen = true,
                IsVisible = true,
                SceneManager = sceneManager,
                SessionProperties = new Dictionary<string, SessionProperty>
            {
                { "CreatedRoom", createdroom },
                { "GameStarted",false }

            }
            });

            if (result.Ok)
            {
                gameManager._runner = _runner;
                LeaveButton.SetActive(true);
                joinButton.SetActive(false);
                lastSessionName = sessionName;
                Debug.Log($"[NetworkManager] ✅ Session started: {sessionName}");
            }
            else
            {
                Debug.LogError($"[NetworkManager] ❌ Failed to start session: {result.ShutdownReason}");
            }
        }


        private string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            System.Random rand = new();
            return new string(Enumerable.Repeat(chars, 2).Select(s => s[rand.Next(s.Length)]).ToArray());
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) // called when player is joined
        {

            if (runner.SessionInfo.IsOpen)
            {
                if (player == runner.LocalPlayer)
                {
                    gameManager.DiconnectPanle.SetActive(false);
                    gameManager.StatsText.text = "Joined Room";
                    if (runner.SessionInfo.Properties.TryGetValue("GameStarted", out SessionProperty value))
                    {
                        if (value == false)
                        {
                            Vector3 spawnPos = spwnpoint[UnityEngine.Random.Range(0, spwnpoint.Length)].position;
                            var obj = runner.Spawn(_playerPrefab, spawnPos, Quaternion.identity, player);
                            //    obj.name = obj.name + _spawnedCharacters.Count;
                            obj.GetComponent<Player_Shooting>().Rpc_AnnouncePlayer("gameManager");

                            _spawnedCharacters.Add(player, obj);
                        }
                        else
                        {

                            Vector3 spawnPos = new Vector3(UnityEngine.Random.Range(-3, 4), spwnpoint[0].transform.position.y, UnityEngine.Random.Range(-3, 4));
                            var obj = runner.Spawn(_playerPrefab, spawnPos, Quaternion.identity, player);
                            //    obj.name = obj.name + _spawnedCharacters.Count;
                            obj.GetComponent<Player_Shooting>().Rpc_AnnouncePlayer("gameManager");

                            _spawnedCharacters.Add(player, obj);
                        }
                    }
                }


                if (!gameStarted && _runner.ActivePlayers.Count() >= MaxPlayers)
                {
                    // runner.SessionInfo.IsOpen = false;
                    //need to claose the room so  no one can join later
                    StartActualGame();
                }
            }

        }






        private IEnumerator FindOrCreateGame()
        {

            if (_runner == null)
            {
                gameManager.StatsText.text = "Connecting";
                Debug.Log("runner is emty");
                GameObject obj = new GameObject();
                _runner = obj.gameObject.AddComponent<NetworkRunner>();

                _runner.ProvideInput = true;
            }
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);

            // Join default lobby to get room list
            var result = _runner.JoinSessionLobby(SessionLobby.Shared);
            while (!result.IsCompleted) yield return null;
            Debug.Log("runner is completed");
            // Wait for session list callback to be triggered
            yield return new WaitForSeconds(2); // Give it time to fetch sessions
        }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log("Disconnected");
            if (_spawnedCharacters.TryGetValue(player, out var obj))
            {

                runner.Despawn(obj);
                _spawnedCharacters.Remove(player);

                // gameManager.gameWinpanel.SetActive(true);
            }
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            Vector2 move = new Vector2(joystick.Horizontal, joystick.Vertical);
            Vector2 shoot = new Vector2(gameManager.joystickShoot.Horizontal, gameManager.joystickShoot.Vertical);

            input.Set(new PlayerInputData
            {
                Horizontal = move.x,
                Vertical = move.y,
                ShootDirection = shoot,
                ShootPressed = shoot.magnitude > .01f,
            });
        }

        // Unused (required for interface)
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            Debug.Log("Input Missing");
        }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) ///game closed
        {
            if (shutdownReason == ShutdownReason.GameIsFull || shutdownReason == ShutdownReason.GameClosed || shutdownReason == ShutdownReason.Ok)
            {
                Debug.Log("creating New room");
            }
            else
            {


                gameManager.DiconnectPanle.SetActive(true);
                //JoinOrCreateSession(lastSessionName);
                Debug.Log("Shut Down" + shutdownReason);
            }

        }
        public void OnConnectedToServer(NetworkRunner runner)
        {
            gameManager.StatsText.text = "Connected to Server";


        }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.Log("Disconnected From Server: " + reason);
            gameManager.StatsText.text = "Lost connection... trying to reconnect";
            Debug.Log(lastSessionName);
            /// lastSessionName = runner.SessionInfo.Name;
            StartCoroutine(JoinPreviousSession());
        }
        public string lastSessionName;
        private IEnumerator JoinPreviousSession()
        {
            yield return new WaitForSeconds(5f); // small delay

            if (!string.IsNullOrEmpty(lastSessionName))
            {
                JoinOrCreateSession(lastSessionName);
            }
            else
            {
                StartGame(GameMode.Shared); // fallback
            }
        }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) => request.Accept();
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            gameManager.DiconnectPanle.SetActive(true);
            Debug.Log("Connect Faild");
        }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
            Debug.Log("Simulation message");
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            Debug.Log($"[NetworkManager] Session list updated. Found: {sessionList.Count}");


            foreach (var session in sessionList)
            {
                // Skip full or closed sessions
                if (session.PlayerCount >= session.MaxPlayers || !session.IsOpen)
                    continue;

                Debug.Log($"[NetworkManager] Joining valid session: {session.Name}");
                hasStartedSession = true;
                JoinOrCreateSession(session.Name);
                return;
            }

            if (!hasStartedSession && !waitingForRoom)
            {
                gameManager.waringtext.text = "No room Found Creating New one";
                StartCoroutine(DelayedRoomCreation());
            }
        }

        private IEnumerator DelayedRoomCreation()
        {
            waitingForRoom = true;
            yield return new WaitForSeconds(2f);
            gameManager.waringtext.text = "";
            if (!hasStartedSession)
            {
                string sessionName = $"Room_{UnityEngine.Random.Range(1000, 9999)}";
                if (createdroom)
                {
                    sessionName = GenerateRoomCode();
                    gameManager.displayRoomCode.text = sessionName;
                }
                Debug.Log($"[NetworkManager] ➕ Creating new session: {sessionName}");
                hasStartedSession = true;
                JoinOrCreateSession(sessionName);
            }
        }
        public GameObject joinButton;
        public GameObject LeaveButton;


        private async System.Threading.Tasks.Task JoinSession(string sessionName)
        {
            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName,
                PlayerCount = MaxPlayers,
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
                SceneManager = _runner.gameObject.AddComponent<NetworkSceneManagerDefault>()

            });

            if (result.Ok)
            {
                lastSessionName = sessionName;
                Debug.Log("Successfully joined session.");
            }
            else
            {
                Debug.LogError("Failed to join session: " + result.ShutdownReason);
            }
        }
        //private IEnumerator HandleSessionListUpdated(List<SessionInfo> sessionList)
        //{
        //    yield return new WaitForSeconds(1f); // Small delay to ensure stability

        //    if (sessionList.Count > 0)
        //    {
        //        string sessionName = sessionList.Last().Name;
        //        Debug.Log("Found session: " + sessionName);
        //        await JoinSession(sessionName);
        //    }
        //    else
        //    {
        //        Debug.Log("No sessions found. Creating one.");
        //        RoomCode = GenerateRoomCode();
        //        await StartGame(GameMode.Shared, RoomCode);
        //        gameManager.StatsText.text = "Room created";
        //    }
        //}
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner)
        {

        }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

      
    }
}