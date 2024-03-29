using GameFramework.Core;
using GameFramework.Core.Data;
using GameFramework.Core.GameFramework.Manager;
using GameFramework.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine.SceneManagement;

namespace Game
{
    public class GameLobbyManager : Singleton<GameLobbyManager>
    {
        public List<LobbyPlayerData> _lobbyPLayerData = new();

        private LobbyPlayerData localLobbyPlayerData;

        private LobbyData _lobbyData;

        public int _maxPlayerSize = 30;

        public bool _inGame;
        private bool _wasDiscconected;
        private string prevRelayCode;

        public bool IsHost => localLobbyPlayerData.Id == LobbyManager.Instance.GetHostId();
        private void OnEnable()
        {
            LobbyEvents.OnLobbyUpdated += OnLobbyUpdated;
        }

        private void OnDisable()
        {
            LobbyEvents.OnLobbyUpdated -= OnLobbyUpdated;
        }
        public async Task<bool> HasActiveLobbies()
        {
            return await LobbyManager.Instance.HasActiveLobbies();
        }
        // M�todo para obtener el c�digo del lobby desde el LobbyManager
        public string GetLobbyCode()
        {
            return LobbyManager.Instance.GetLobbyCode();
        }

        // M�todo para crear un lobby de juego de forma as�ncrona
        public async Task<bool> CreateLobby()
        {
            // Se crea un objeto LobbyPlayerData que representa los datos del jugador anfitri�n.
            localLobbyPlayerData = gameObject.AddComponent<LobbyPlayerData>();
            localLobbyPlayerData.Initialize(AuthenticationService.Instance.PlayerId, "HostPlayer");
            _lobbyData = new LobbyData();
            _lobbyData.Initialize(0);

            // Se llama al m�todo CreateLobby del LobbyManager para crear el lobby con los siguientes par�metros:
            // - Tiempo l�mite del lobby: 30 segundos
            // - Se permite iniciar el juego autom�ticamente cuando todos los jugadores est�n listos (true)
            // - Los datos del jugador anfitri�n se serializan y se env�an al lobby para que los dem�s jugadores puedan acceder a ellos.
            bool succeeded = await LobbyManager.Instance.CreateLobby(_maxPlayerSize, true, localLobbyPlayerData.Serialize(), _lobbyData.Serialize() );

            // Se devuelve el resultado de la creaci�n del lobby.
            return succeeded;
        }

        // M�todo para unirse a un lobby existente de forma as�ncrona
        public async Task<bool> JoinLobby(string code)
        {

            localLobbyPlayerData = gameObject.AddComponent<LobbyPlayerData>();
            localLobbyPlayerData.Initialize(AuthenticationService.Instance.PlayerId, "JoinPlayer");
            // Se llama al m�todo JoinLobby del LobbyManager para unirse al lobby con el c�digo proporcionado y los datos del jugador.
            bool succeeded = await LobbyManager.Instance.JoinLobby(code, localLobbyPlayerData.Serialize());

            // Se devuelve el resultado de unirse al lobby.
            return succeeded;
        }

        private async void OnLobbyUpdated(Lobby lobby)
        {
            // Obtiene los datos de los jugadores en el lobby
            List<Dictionary<string, PlayerDataObject>> playerData = LobbyManager.Instance.GetPlayersData();
            _lobbyPLayerData.Clear();

            int numberOfPlayerReady = 0;

            foreach (Dictionary<string, PlayerDataObject> data in playerData)
            {
                LobbyPlayerData lobbyPlayerData = new();
                lobbyPlayerData.Initialize(data);

                if (lobbyPlayerData.IsReady)
                {
                    numberOfPlayerReady++;
                }

                // Comprueba si el jugador es el jugador local y lo almacena
                if (lobbyPlayerData.Id == AuthenticationService.Instance.PlayerId)
                {
                    localLobbyPlayerData = lobbyPlayerData;
                }

                _lobbyPLayerData.Add(lobbyPlayerData);
            }
            _lobbyData = new LobbyData();
            _lobbyData.Initialize(lobby.Data);

            Events.LobbyEvents.OnLobbyUpdated?.Invoke();

            if(numberOfPlayerReady == lobby.Players.Count)
            {
                Events.LobbyEvents.OnLobbyReady?.Invoke();
            }

            if(_lobbyData.RelayJoinCode != default && !_inGame)
            {
                if (_wasDiscconected)
                {
                    if(_lobbyData.RelayJoinCode != prevRelayCode)
                    {
                        await JoinRelayServer(_lobbyData.RelayJoinCode);
                        SceneManager.LoadSceneAsync(_lobbyData.SceneName);
                    }
                }
                else
                {
                    await JoinRelayServer(_lobbyData.RelayJoinCode);
                    SceneManager.LoadSceneAsync(_lobbyData.SceneName);
                }
               
            }

            Console.WriteLine($"Nombre de la escena actual: {_lobbyData.SceneName}");

        }



        public List<LobbyPlayerData> GetPlayers()
        {
            return _lobbyPLayerData;
        }

        public async Task<bool> SetPlayerReady()
        {
            localLobbyPlayerData.IsReady = true;
            return await LobbyManager.Instance.UpdatePlayerData(localLobbyPlayerData.Id,
                localLobbyPlayerData.Serialize());
        }

        public int GetMapIndex()
        {
            return _lobbyData.MapIndex;
        }

        public async Task<bool> SetSelectedMap(int currentMapIndex, string sceneName)
        {
            _lobbyData.MapIndex = currentMapIndex;
            _lobbyData.SceneName = sceneName;

            return await LobbyManager.Instance.UpdateLobbyData(_lobbyData.Serialize());
        }

        public async Task StartGame()
        {
            string RelayJoinCode = await RelayManager.Instance.CreateRelay(_maxPlayerSize);
            _inGame = true;

            _lobbyData.RelayJoinCode = RelayJoinCode;
            await LobbyManager.Instance.UpdateLobbyData(_lobbyData.Serialize());

            string allocationId = RelayManager.Instance.GetAllocationId();
            string connectionData = RelayManager.Instance.GetConnectionData();
            //Borrar en caso de que falle

            localLobbyPlayerData.IsReady = false;
            await LobbyManager.Instance.UpdatePlayerData(localLobbyPlayerData.Id, localLobbyPlayerData.Serialize(), allocationId, connectionData);

            SceneManager.LoadSceneAsync(_lobbyData.SceneName);
            Console.WriteLine($"Nombre de la escena actual: {_lobbyData.SceneName}");

        }

        private async Task<bool> JoinRelayServer(string relayJoinCode)
        {
                _inGame = true;
                await RelayManager.Instance.JoinRelay(relayJoinCode);
                string allocationId = RelayManager.Instance.GetAllocationId();
                string connectionData = RelayManager.Instance.GetConnectionData();
            //Borrar en caso de que falle
                localLobbyPlayerData.IsReady = false;
                await LobbyManager.Instance.UpdatePlayerData(localLobbyPlayerData.Id, localLobbyPlayerData.Serialize(), allocationId, connectionData);
                return true;
        }

        public async Task<bool> RejoinGame()
        {
            return await LobbyManager.Instance.RejoinLobby();
        }

        public async Task<bool> LeaveAllLobby()
        {
            return await LobbyManager.Instance.LeaveAllLobby();
        }

        public async void GoBackToLobby(bool disconnected)
        {
            _inGame = false;
            _wasDiscconected = disconnected;

            if (_wasDiscconected)
            {
                prevRelayCode = _lobbyData.RelayJoinCode;
            }

            localLobbyPlayerData.IsReady = false;
            await LobbyManager.Instance.UpdatePlayerData(localLobbyPlayerData.Id, localLobbyPlayerData.Serialize());
            SceneManager.LoadSceneAsync("Lobby");
        }
    }

}