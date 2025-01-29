using System;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

namespace DefaultNamespace
{
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; } = null;
        private FacepunchTransport _transport;
        public Lobby? CurrentLobby { get; private set; } = null;
        public ulong HostId { get; private set; } = 0;


        private void Awake()
        {
            Instance ??= this;
            _transport = GetComponent<FacepunchTransport>();
        }

        private void OnEnable()
        {
            SteamMatchmaking.OnLobbyCreated += LobbyCreatedCallback;
            SteamMatchmaking.OnLobbyEntered += LobbyEnteredCallback;
            SteamMatchmaking.OnLobbyMemberJoined += LobbyMemberJoinedCallback;
            SteamMatchmaking.OnLobbyMemberDataChanged += LobbyMemberDataChangedCallback;
            SteamMatchmaking.OnLobbyMemberLeave += LobbyMemberLeaveCallback;
            SteamMatchmaking.OnLobbyMemberDisconnected += LobbyMemberDisconnectedCallback;
            SteamMatchmaking.OnLobbyInvite += LobbyInviteCallback;
            
            SteamFriends.OnGameLobbyJoinRequested += OnLobbyJoinRequestCallback;
        }
        
        private void OnDisable()
        {
            SteamMatchmaking.OnLobbyCreated -= LobbyCreatedCallback;
            SteamMatchmaking.OnLobbyEntered -= LobbyEnteredCallback;
            SteamMatchmaking.OnLobbyMemberJoined -= LobbyMemberJoinedCallback;
            SteamMatchmaking.OnLobbyMemberDataChanged -= LobbyMemberDataChangedCallback;
            SteamMatchmaking.OnLobbyMemberLeave -= LobbyMemberLeaveCallback;
            SteamMatchmaking.OnLobbyMemberDisconnected -= LobbyMemberDisconnectedCallback;
            SteamMatchmaking.OnLobbyInvite -= LobbyInviteCallback;

            SteamFriends.OnGameLobbyJoinRequested -= OnLobbyJoinRequestCallback;

            if (NetworkManager.Singleton == null) return;

            NetworkManager.Singleton.OnServerStarted -= OnServerStartedCallback;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedCallback;

        }
        
        private void OnApplicationQuit()
        {
            Disconnect();
        }

        public async void StartHost(int maxMembers)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStartedCallback;
            NetworkManager.Singleton.StartHost();
            CurrentLobby = await SteamMatchmaking.CreateLobbyAsync(maxMembers);
        }

        public void StartClient(SteamId steamId)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;
            
            _transport.targetSteamId = steamId;

            bool clientStarted = NetworkManager.Singleton.StartClient();
            if(clientStarted) Debug.Log("Client Started");
        }

        public void Disconnect()
        {
            CurrentLobby?.Leave();

            if (NetworkManager.Singleton == null) return;
            
            if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.OnServerStarted -= OnServerStartedCallback;
            }

            if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedCallback;
            }
            
            NetworkManager.Singleton.Shutdown();
            Debug.LogWarning("Client disconnected");
        }
        
        private async void OnLobbyJoinRequestCallback(Lobby lobby, SteamId steamId)
        {
            RoomEnter lobbyToJoin = await lobby.Join();

            if (lobbyToJoin == RoomEnter.Success)
            {
                Debug.Log($"Joined {lobby.Owner} lobby");
                return;
            }
            Debug.LogError($"Failed to join lobby {lobby.Owner}");
        } 
        private void OnClientDisconnectedCallback(ulong clientId)
        {
            Debug.LogError($"Client {clientId} disconnected");
        }

        private void OnClientConnectedCallback(ulong clientId)
        {
            Debug.Log($"Client {clientId} connected");
        }

        private void OnServerStartedCallback()
        {
            Debug.LogWarning("Host Created");
        }


        
        private void LobbyInviteCallback(Friend friend, Lobby lobby)
        {
            Debug.Log($"Invited to {lobby.Id} from {friend.Name}");
        }

        private void LobbyMemberDisconnectedCallback(Lobby lobby, Friend friend)
        {
            
        }

        private void LobbyMemberLeaveCallback(Lobby lobby, Friend friend)
        {
            
        }

        private void LobbyMemberDataChangedCallback(Lobby lobby, Friend friend)
        {
            
        }

        private void LobbyMemberJoinedCallback(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} Joined lobby");
        }
        private void LobbyEnteredCallback(Lobby lobby)
        {
            if (NetworkManager.Singleton.IsHost) return;
            
            if(CurrentLobby != null) StartClient(CurrentLobby.Value.Owner.Id);
        }
        
        private void LobbyCreatedCallback(Result result, Lobby lobby)
        {
            if (result == Result.OK)
            {
                Debug.LogWarning("LobbyCreatedCallback");
                lobby.SetPublic();
                lobby.SetJoinable(true);
                lobby.SetGameServer(lobby.Owner.Id);
                return;
            }
            Debug.LogError("Failed to create lobby");
        }
    }
}