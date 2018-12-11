using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteNetLib;
using PA.Networking.Server.Player;
using PA.Networking.Server.Room;

namespace PA.Networking.Server {
    public class ServerManager {
        private MyNetworkServer<GlobalPlayer> _server;

        public int Port = 5555;

        public Dictionary<int, GameInstance> Rooms;
        public Dictionary<int, GlobalPlayer> AllPlayers;

        public ServerManager() {
            _server = new MyNetworkServer<GlobalPlayer>();
            _server.OnDisconnect += OnPlayerDisconnect;

            /* TCP Protocol */
            _server.RegisterHandler(GlobalProtocol.ClientToServer.ConnectionConfirm, ConnectionConfirmMessage);
            _server.RegisterHandler(GlobalProtocol.ClientToServer.PlayerName, PlayerNameMessage);
            _server.RegisterHandler(GlobalProtocol.ClientToServer.ConnectToRoom, ConnectToRoomMessage);

            Rooms = new Dictionary<int, GameInstance>();
            AllPlayers = new Dictionary<int, GlobalPlayer>();
        }

        public void Update() {
            _server.PollEvents();
        }

        public void StartServer() {
            _server.Start(Port);
        }

        public void StopServer() {
            _server.Stop();
            foreach (var room in Rooms) {
                Logger.WriteLine("Room instance {0} removed because server stopped.", room.Key);
                room.Value.StopRoomInstance();
            }

            Rooms.Clear();
            AllPlayers.Clear();
        }

        public GameInstance CreateRoom(int maxPlayer, int minPlayerToStart, int timeBeforeStart) {
            var room = new GameInstance(this, maxPlayer, minPlayerToStart, timeBeforeStart);
            Rooms.Add(room.Id, room);
            return room;
        }

        public GameInstance GetRoom(int index) {
            int i = 0;
            foreach (var r in Rooms) {
                if (i == index) return r.Value;
                i++;
            }
            return null;
        }

        public void PrintRoom(int index) {
            var room = GetRoom(index);
            
            Console.Write("Room {0} : {1}/{2}", room.Id, room.AlivePlayerCount, room.MaxPlayer);
            Console.WriteLine(room.GameStarted ? " --> Game Started !!" : "");
        }

        public void StartRoom(int index) {
            var room = GetRoom(index);
            room.GameStarted = true;
        }

        public void DeleteRoom(int index) {
            var room = GetRoom(index);
            room.StopRoomInstance();
            Rooms.Remove(room.Id);
        }

        /****************************************************************************/

        private void OnPlayerDisconnect(GlobalPlayer player) {
            Logger.WriteLine("Player {0} disconnected.", player.Id);
            AllPlayers.Remove(player.Id);
        }

        private void ConnectionConfirmMessage(GlobalPlayer player, NetworkMessage msg) {
            if (msg.AvailableBytes > 0) {
                int playerId = msg.GetInt();
                if (AllPlayers.ContainsKey(playerId))
                    player = AllPlayers[playerId];
                else
                    player = new GlobalPlayer(msg.Peer, playerId);
            }
            else
                player = new GlobalPlayer(msg.Peer);

            _server.RegisterPlayer(player);
            if (!AllPlayers.ContainsKey(player.Id)) AllPlayers.Add(player.Id, player);
            
            Logger.WriteLine("Confirmation Connection for player {0}", player.Id);

            var writer = new NetworkWriter(GlobalProtocol.ServerToClient.ConnectionConfirm);
            writer.Put(player.Id);

            player.SendWriter(writer, DeliveryMethod.ReliableOrdered);
        }

        private void PlayerNameMessage(GlobalPlayer player, NetworkMessage msg) {
            string newName = msg.GetString();
            player.Name = newName;
            Logger.WriteLine("Player {0} changed its name to {1}", player.Id, newName);

            var writer = new NetworkWriter(GlobalProtocol.ServerToClient.PlayerNameChanged);

            player.SendWriter(writer, DeliveryMethod.ReliableOrdered);
        }

        private void ConnectToRoomMessage(GlobalPlayer player, NetworkMessage msg) {
            GameInstance tojoin = null;
            foreach (var room in Rooms) {
                if (room.Value.PlayerCount < room.Value.MaxPlayer && !room.Value.GameStarted) {
                    tojoin = room.Value;
                    break;
                }
            }

            if (tojoin == null) {
                tojoin = new GameInstance(this);
                Rooms.Add(tojoin.Id, tojoin);
            }

            tojoin.AddExpectedPlayer(player);

            NetworkWriter writer = new NetworkWriter(GlobalProtocol.ServerToClient.ConnectToRoom);
            writer.Put(tojoin.ListenPort);
            player.SendWriter(writer, DeliveryMethod.ReliableOrdered);
        }

        public void PlayerQuitRoom(InGamePlayer player, GameInstance room) {
            if (room.PlayerCount == 0 && Rooms.ContainsKey(room.Id)) {
                Logger.WriteLine("Room instance {0} removed because all players quit.", room.Id);
                room.StopRoomInstance();
                Rooms.Remove(room.Id);
            }
        }

        public void PlayerDisconnected(InGamePlayer player, GameInstance room) {
            if (room.PlayerCount == 0 && Rooms.ContainsKey(room.Id)) {
                Logger.WriteLine("Room instance {0} removed because all players quit.", room.Id);
                room.StopRoomInstance();
                Rooms.Remove(room.Id);
            }

            AllPlayers.Remove(player.Id);
        }
    }
}