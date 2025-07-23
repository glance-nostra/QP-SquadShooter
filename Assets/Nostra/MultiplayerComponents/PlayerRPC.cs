using System.Collections.Generic;
using Fusion;
using Newtonsoft.Json;
using nostra.character;
using nostra.models.response;
using nostra.multiplayer;
using nostra.quickplay;
using nostra.utils;
using UnityEngine;

public class PlayerRPC : NetworkBehaviour, IPlayerRPCInterface 
{
	[SerializeField] GameObject characterGraphics;
	[SerializeField] PlayerToken m_token;
	MultiplayerService m_multiplayerService;
	MultiplayerRoomInfo m_multiplayerRoomInfo;
	
	bool m_initialized;
	private bool m_visibility;
	public PlayerToken Token => m_token;

	public void Init()
	{
		m_multiplayerService = FindAnyObjectByType<MultiplayerService>();
		m_initialized = true;
		m_multiplayerRoomInfo = FindAnyObjectByType<MultiplayerRoomInfo>();
	}
	public void ApplyPlayerTokenAndBroadcast(PlayerToken token)
	{
		if(!HasStateAuthority) return;
		RPC_ApplyPlayerToken(token.ToBytes());
	}
	public void ApplyCustomizationAndBroadcast(List<SlotsDto> token)
	{
		if(!HasStateAuthority) return;
		var jsonToken = JsonConvert.SerializeObject(token);
		RPC_BroadcastCharacterTokenApplication(GzipUtils.Compress(jsonToken));
	}
	public void HidePlayerAndBroadcast()
	{
		if(!HasStateAuthority) return;
		RPC_ChangeVisibility(false);
	}

	public void ShowPlayerAndBoradcast()
	{
		if(!HasStateAuthority) return;
		RPC_ChangeVisibility(true);
	}

	public void SetRoomCreationTimeAndBroadcast(long creationEpochTime)
	{
		RPC_SetRoomCreationTime(creationEpochTime);
	}

	public void RequestRoomCreationTimeBroadcast()
	{
		Debug.Log("RequestRoomCreationTimeBroadcast");
		RPC_RequestRoomCreationTimeBroadcast();
	}

	public void StartMpGameBroadcast()
	{
		RPC_StartMpGameBroadcast();
	}

	public override void Despawned(NetworkRunner runner, bool hasState)
	{
		base.Despawned(runner,hasState);
		Debug.Log("Announcing leave");
		AnnounceLeave();	
	}
	public void BroadCastVisibilityState()
	{
		if(!HasStateAuthority) return;
		RPC_ChangeVisibility(m_visibility);

	}
	void AnnounceLeave()
	{
		if(!m_initialized) Init();
		var leftMessage = new PlayerLeftMessage() {token = m_token};
		QuickPlay.Messaging.Publish(leftMessage, this);
	}
	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	void RPC_ApplyPlayerToken(byte[] tokenRaw)
	{
		if(!m_initialized) Init();
		var token = PlayerToken.FromBytes(tokenRaw);
		m_token = token;
		var joinedMessage = new PlayerJoinedMessage() {token = token};
		QuickPlay.Messaging.Publish(joinedMessage,this);
	}
	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_BroadcastCharacterTokenApplication(byte[] tokenRaw)
    {
		if(!m_initialized) Init();
		var slotsJson = GzipUtils.Decompress(tokenRaw);
		var slots = JsonConvert.DeserializeObject<List<SlotsDto>>(slotsJson);
		var message = new MultiplayerCustomizeMessage()
		{
			playerRefId = Object.StateAuthority.PlayerId,
			data = slots.ToArray()
		};
		QuickPlay.Messaging.Publish(message, this);
    }
	[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
	void RPC_ChangeVisibility(bool val)
	{
		if(characterGraphics == null) return;
		m_visibility = val;
		characterGraphics.SetActive(val);	
	}
	
	[Rpc(RpcSources.All, RpcTargets.All)]
	void RPC_SetRoomCreationTime(long creationEpochTime)
	{
		if (!m_multiplayerRoomInfo)
		{
			m_multiplayerRoomInfo = FindAnyObjectByType<MultiplayerRoomInfo>();
		}

		if (m_multiplayerRoomInfo)
		{
			Debug.Log("J:RPC Set Room Creation Time: " + creationEpochTime);
			m_multiplayerRoomInfo.nRoomCreationEpochTime = creationEpochTime;
			var timeSetMessage = new RoomCreationTimeSetMessage() {RoomCreationEpochTime = creationEpochTime};
			QuickPlay.Messaging.Publish(timeSetMessage,this);
		}
	}
	
	[Rpc(RpcSources.All, RpcTargets.All)]
	void RPC_RequestRoomCreationTimeBroadcast()
	{
		Debug.Log("J: RPC RequestRoomCreationTimeBroadcast");
		if (!Runner.IsSharedModeMasterClient)
		{
			return;
		}
		Debug.Log("J: RPC RequestRoomCreationTimeBroadcast - Master Client");
		if (!m_multiplayerRoomInfo)
		{
			m_multiplayerRoomInfo = FindAnyObjectByType<MultiplayerRoomInfo>();
		}

		if (m_multiplayerRoomInfo)
		{
			Debug.Log("J:RPC replying by setting Room Creation Time to : " + m_multiplayerRoomInfo.nRoomCreationEpochTime);
			RPC_SetRoomCreationTime(m_multiplayerRoomInfo.nRoomCreationEpochTime);
		}
	}

	[Rpc(RpcSources.All, RpcTargets.All)]
	void RPC_StartMpGameBroadcast()
	{
		Debug.Log("J: RPC StartMpGameBroadcast");
		QuickPlay.Messaging.Publish(new MultiplayerGameStartMessage(),this);
	}
}
