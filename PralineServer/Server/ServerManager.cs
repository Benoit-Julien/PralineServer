using System;
using System.Threading;
using System.Collections.Generic;
using LiteNetLib;
using PA.Server.Player;

namespace PA.Server {
    public class ServerManager {
        private MyNetworkServer<GlobalPlayer> _server;

        public int Port = 5555;

        private List<Room.GameInstance> _rooms;
        private List<Room.GameInstance> _roomsToDelete;

        private Dictionary<int, InGamePlayer> _expectedPlayers;

        public ServerManager() {
            _server = new MyNetworkServer<GlobalPlayer>();
            _server.OnDisconnect = OnPlayerDisconnect;

            /* TCP Protocol */
            _server.RegisterHandler(GlobalProtocol.ClientToServer.ConnectionConfirm, ConnectionConfirmMessage);
            _server.RegisterHandler(GlobalProtocol.ClientToServer.PlayerName, PlayerNameMessage);
            _server.RegisterHandler(GlobalProtocol.ClientToServer.ConnectToRoom, ConnectToRoomMessage);

            _rooms = new List<Room.GameInstance>();
            _roomsToDelete = new List<Room.GameInstance>();

            _expectedPlayers = new Dictionary<int, InGamePlayer>();
        }

        public void Update() {
            _server.PollEvents();

            if (_roomsToDelete.Count > 0) {
                foreach (var room in _roomsToDelete) {
                    Console.WriteLine("Game instance {0} removed because all players quit.", room.Id);
                    _rooms.Remove(room);
                }

                _roomsToDelete.Clear();
            }
        }

        public void StartServer() {
            _server.Start(Port);
        }

        public void StopServer() {
            _server.Stop();
            foreach (var room in _rooms) {
                Console.WriteLine("Game instance {0} removed because server stoped.", room.Id);
                room.StopRoomInstance();
                _rooms.Remove(room);
            }

            _rooms.Clear();
            _roomsToDelete.Clear();
            _expectedPlayers.Clear();
        }

        /****************************************************************************/

        private void OnPlayerDisconnect(GlobalPlayer player) {
            Console.WriteLine("Player {0} disconnected", player.Id);
        }

        private void ConnectionConfirmMessage(GlobalPlayer player, NetworkMessage msg) {
            if (msg.AvailableBytes > 0) {
                int playerId = msg.GetInt();
                if (_expectedPlayers.ContainsKey(playerId)) {
                    var oldPlayer = _expectedPlayers[playerId];
                    _expectedPlayers.Remove(playerId);
                    player = new GlobalPlayer(msg.Peer, oldPlayer.Id);
                    player.Name = oldPlayer.Name;
                }
                else
                    player = new GlobalPlayer(msg.Peer);
            }
            else
                player = new GlobalPlayer(msg.Peer);

            _server.RegisterPlayer(player);

            Console.WriteLine("Confirmation Connection for player : {0}", player.Id);

            var writer = new NetworkWriter(GlobalProtocol.ServerToClient.ConnectionConfirm);
            writer.Put(player.Id);

            player.SendWriter(writer, DeliveryMethod.ReliableOrdered);
        }

        private void PlayerNameMessage(GlobalPlayer player, NetworkMessage msg) {
            string newName = msg.GetString();
            player.Name = newName;
            Console.WriteLine("Player ({0}) changed its name to {1}", player.Id, newName);

            var writer = new NetworkWriter(GlobalProtocol.ServerToClient.PlayerNameChanged);

            player.SendWriter(writer, DeliveryMethod.ReliableOrdered);
        }

        private void ConnectToRoomMessage(GlobalPlayer player, NetworkMessage msg) {
            Room.GameInstance tojoin = null;
            foreach (var room in _rooms) {
                if (room.PlayerCount < Room.GameInstance.MaxPlayer && !room.GameStarted) {
                    tojoin = room;
                    break;
                }
            }

            if (tojoin == null) {
                tojoin = new Room.GameInstance(this);
                _rooms.Add(tojoin);
            }

            tojoin.AddExpectedPlayer(player);

            NetworkWriter writer = new NetworkWriter(GlobalProtocol.ServerToClient.ConnectToRoom);
            writer.Put(tojoin.ListenPort);
            player.SendWriter(writer, DeliveryMethod.ReliableOrdered);
        }

        public void PlayerQuitRoom(InGamePlayer player, Room.GameInstance room) {
            Console.WriteLine("Player Quit Room {0}", player.Name);

            if (room.PlayerCount == 0)
                _roomsToDelete.Add(room);
            _expectedPlayers.Add(player.Id, player);
        }

        public void PlayerDisconnected(InGamePlayer player, Room.GameInstance room) {
            Console.WriteLine("Player Disconnected {0}", player.Name);

            if (room.PlayerCount == 0)
                _roomsToDelete.Add(room);
        }
    }
}