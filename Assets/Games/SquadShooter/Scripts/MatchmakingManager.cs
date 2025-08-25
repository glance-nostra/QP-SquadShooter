using System.Collections;
using Fusion;
using TMPro;
using UnityEngine;

namespace nostra.SarvotamSolutions.SquardShooterMultiplayer
{
    public class MatchmakingManager : NetworkBehaviour
    {
        //public static MatchmakingManager Instance;

        //[Header("Matchmaking Settings")]
        //public TMP_Text matchmakingText;
        //public float matchmakingTime = 50f;
        //public int requiredPlayers = 4;

        //[Networked] private float timer { get; set; } = 0f;
        //[Networked] public NetworkBool matchmakingActive { get; set; } = false;

        //private bool ISpawned = false;
        //public GameObject matchmackingPanel;

        //void Awake()
        //{
        //    if (Instance == null) Instance = this;
        //    else Destroy(gameObject);
        //}

        //void Start()
        //{
        //    // matchmackingPanel.SetActive(false);
        //    // Begin matchmaking
        //    NetworkManager.Instance.StartGame(GameMode.Shared);
        //}

        //public override void Spawned()
        //{
        //    Debug.Log($"[MatchmakingManager] My NetworkObject.Id is {Object.Id}");
        //    Debug.Log("✅ MatchmakingManager Spawned");
        //    timer = matchmakingTime;
        //    ISpawned = true;
        //    matchmakingActive = true;
        //    matchmackingPanel.SetActive(true);
        //    StartCoroutine(MatchmakingCountdown());
        //}

        //void Update()
        //{
        //    if (!ISpawned || !matchmakingActive) return;
        //   // int realPlayers = GameManager.Instance.GetRealPlayerCount();
        //    string timeRem = $"{Mathf.FloorToInt(timer / 60f):00}:{Mathf.FloorToInt(timer % 60f):00}";
        //    matchmakingText.text = $"Matchmaking: {realPlayers}/{requiredPlayers}\nTime: {timeRem}";
        //}

        //IEnumerator MatchmakingCountdown()
        //{
        //    if (Runner.IsSharedModeMasterClient)
        //    {
        //        while (GameManager.Instance.GetRealPlayerCount() < 1)
        //            yield return null;

        //        while (timer > 0)
        //        {
        //            yield return new WaitForSeconds(1f);
        //            timer -= 1f;
        //        }

        //        int realPlayers = GameManager.Instance.GetRealPlayerCount();
        //        if (realPlayers < requiredPlayers)
        //        {
        //            int missing = requiredPlayers - realPlayers;
        //            GameManager.Instance.SpawnAIBots(missing);
        //        }

        //        // Close room
        //        Runner.SessionInfo.IsOpen = false;
        //        Runner.SessionInfo.IsVisible = false;
        //        Debug.Log("[MatchmakingManager] Room locked.");

        //        RPC_DeactivateMatchmakingText();
        //    }
        //}

        //[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        //void RPC_DeactivateMatchmakingText(RpcInfo info = default)
        //{
        //    matchmakingActive = false;
        //    matchmackingPanel.SetActive(false);
        //    matchmakingText.gameObject.SetActive(false);
        //}
    }
}