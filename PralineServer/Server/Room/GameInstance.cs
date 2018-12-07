using System;
using System.Collections.Generic;
using System.Threading;
using LiteNetLib;
using PA.Server.Player;

namespace PA.Server.Room {
    public class GameInstance {
        public static readonly int MaxPlayer = 32;
        public static readonly int MinPlayerToStart = 1; //8;

        public int Id;
        public int ListenPort;
        public int PlayerCount;
        public int AlivePlayerCount;

        public Dictionary<int, InGamePlayer> PlayerList;
        private Dictionary<int, APlayer> _expectedPlayers;

        public bool GameStarted;
        public bool GameEnded;

        private Thread _roomLoop;
        private MyNetworkServer<InGamePlayer> _server;
        private bool _stopRoomLoop;

        private ServerManager _manager;
        private Dictionary<int, ItemGenerator.Item> _itemList;
        private Dictionary<int, EnigmasGenerator.Enigmas> _enigmasList;

        public GameInstance(ServerManager manager) {
            _manager = manager;

            PlayerCount = 0;
            Id = IDGenerator.getInstance().GenerateUniqueID();
            PlayerList = new Dictionary<int, InGamePlayer>();
            _expectedPlayers = new Dictionary<int, APlayer>();
            GameStarted = false;
            GameEnded = false;

            _server = new MyNetworkServer<InGamePlayer>(MaxPlayer);
            _server.OnDisconnect = OnPlayerDisconnect;

            /* TCP Protocol */
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.ConnectionConfirm, ConnectionConfirmMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.QuitRoom, QuitGameMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.Jump, PlayerJumpMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.Crouch, PlayerCrouchMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.Reloading, PlayerReloadingMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.EnigmaOpened, EnigmaOpenedMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.TakeItem, TakeItemMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.DropItem, DropItemMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.SwitchItem, SwitchItemMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.UseItem, UseItemMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.Shoot, PlayerShootMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.HitPlayer, PlayerHitMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.StartThrowing, StartThrowingMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.Throwing, ThrowingMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.ThrowableEnd, ThrowableEndMessage);

            /* UDP Protocol */
            _server.RegisterHandler(InGameProtocol.UDPClientToServer.Movement, PlayerMovementMessage);
            _server.RegisterHandler(InGameProtocol.UDPClientToServer.Turn, PlayerTurnMessage);
            _server.RegisterHandler(InGameProtocol.UDPClientToServer.ThrowableMove, ThrowableMoveMessage);

            _server.Start();
            ListenPort = _server.Port;

            _stopRoomLoop = false;
            _roomLoop = new Thread(RoomLoop);
            _roomLoop.Start();
        }

        ~GameInstance() {
            IDGenerator.getInstance().RemoveUniqueID(Id);
        }
        
        public void StopRoomInstance() {
            _stopRoomLoop = true;
            _roomLoop.Join();
            _server.Stop();
        }

        public bool AddExpectedPlayer(APlayer player) {
            if (_expectedPlayers.ContainsKey(player.Id)
                || PlayerList.ContainsKey(player.Id)
                || PlayerCount == MaxPlayer)
                return false;

            PlayerCount += 1;
            _expectedPlayers.Add(player.Id, player);
            return true;
        }

        public bool RemovePlayer(int playerId) {
            if (_expectedPlayers.ContainsKey(playerId)) {
                _expectedPlayers.Remove(playerId);
                PlayerCount -= 1;
                return true;
            }

            if (PlayerList.ContainsKey(playerId)) {
                var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.PlayerQuit);
                writer.Put(playerId);

                _server.SendAll(writer, DeliveryMethod.ReliableOrdered);

                if (PlayerList[playerId].IsAlive)
                    AlivePlayerCount -= 1;
                PlayerList.Remove(playerId);
                PlayerCount -= 1;
                return true;
            }

            return false;
        }

        private void OnPlayerDisconnect(InGamePlayer player) {
            Console.WriteLine("Player {0} disconnected to room", player.Id);
            RemovePlayer(player.Id);
            _manager.PlayerDisconnected(player, this);
        }

        private void ConnectionConfirmMessage(InGamePlayer p, NetworkMessage msg) {
            int playerId = msg.GetInt();

            if (_expectedPlayers.ContainsKey(playerId)) {
                var oldPlayer = _expectedPlayers[playerId];
                _expectedPlayers.Remove(playerId);
                var player = new InGamePlayer(msg.Peer, playerId);
                player.Name = oldPlayer.Name;

                var writerOther = new NetworkWriter(InGameProtocol.TCPServerToClient.ConectedToRoom);
                writerOther.Put(oldPlayer.Id);
                writerOther.Put(oldPlayer.Name);
                _server.SendAll(writerOther, DeliveryMethod.ReliableOrdered);

                var writerNew = new NetworkWriter(InGameProtocol.TCPServerToClient.ListConnectedPlayer);
                writerNew.Put(PlayerList.Count);

                foreach (var connectedPlayer in PlayerList) {
                    writerNew.Put(connectedPlayer.Value.Id);
                    writerNew.Put(connectedPlayer.Value.Name);
                    writerNew.Put(connectedPlayer.Value.Position);
                    writerNew.Put(connectedPlayer.Value.Rotation);
                }

                player.SendWriter(writerNew, DeliveryMethod.ReliableOrdered);

                writerNew = new NetworkWriter(InGameProtocol.TCPServerToClient.Registered);
                player.SendWriter(writerNew, DeliveryMethod.ReliableOrdered);
                _server.RegisterPlayer(player);

                PlayerList.Add(player.Id, player);
            }
        }

        private void QuitGameMessage(InGamePlayer player, NetworkMessage msg) {
            RemovePlayer(player.Id);
            _manager.PlayerQuitRoom(player, this);
        }

        private void PlayerJumpMessage(InGamePlayer player, NetworkMessage msg) {
            short state = msg.GetShort();

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.Jump);
            writer.Put(player.Id);
            writer.Put(state);

            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void PlayerCrouchMessage(InGamePlayer player, NetworkMessage msg) {
            bool state = msg.GetBool();

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.Crouch);
            writer.Put(player.Id);
            writer.Put(state);

            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void PlayerReloadingMessage(InGamePlayer player, NetworkMessage msg) {
            bool state = msg.GetBool();

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.Reloading);
            writer.Put(player.Id);
            writer.Put(state);

            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void EnigmaOpenedMessage(InGamePlayer player, NetworkMessage msg) {
            int enigmaID = msg.GetInt();

            var enigmas = _enigmasList[enigmaID];
            enigmas.EnigmaOpened = true;

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.EnigmaOpened);
            writer.Put(player.Id);
            writer.Put(enigmaID);
            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void TakeItemMessage(InGamePlayer player, NetworkMessage msg) {
            int itemID = msg.GetInt();
            int quantity = msg.GetInt();

            if (!_itemList.ContainsKey(itemID))
                return;

            var item = _itemList[itemID];

            if (quantity == item.Quantity)
                _itemList.Remove(itemID);
            else {
                item.Quantity -= quantity;
                item = new ItemGenerator.Item(item, quantity);
            }

            player.TakeItem(item);

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.TakeItem);
            writer.Put(player.Id);
            writer.Put(item.ID);
            writer.Put(quantity);
            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void DropItemMessage(InGamePlayer player, NetworkMessage msg) {
            int itemID = msg.GetInt();
            int quantity = msg.GetInt();

            var item = player.DropItem(itemID, quantity);
            _itemList.Add(item.ID, item);

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.DropItem);
            writer.Put(player.Id);
            writer.Put(item.ID);
            writer.Put(quantity);
            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void SwitchItemMessage(InGamePlayer player, NetworkMessage msg) {
            int itemID = msg.GetInt();

            player.SwitchCurrentItem(itemID);

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.SwitchItem);
            writer.Put(player.Id);
            writer.Put(itemID);
            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void UseItemMessage(InGamePlayer player, NetworkMessage msg) {
            int itemID = msg.GetInt();
            int quantity = msg.GetInt();

            player.UseItem(itemID, quantity);

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.UseItem);
            writer.Put(player.Id);
            writer.Put(itemID);
            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void PlayerShootMessage(InGamePlayer player, NetworkMessage msg) {
            int state = msg.GetInt();
            bool isShooting = msg.GetBool();

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.Shoot);
            writer.Put(player.Id);
            writer.Put(state);
            writer.Put(isShooting);

            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void PlayerHitMessage(InGamePlayer player, NetworkMessage msg) {
            int hitPlayerID = msg.GetInt();
            short damage = msg.GetShort();

            InGamePlayer hitPlayer = PlayerList[hitPlayerID];
            if (!hitPlayer.IsAlive)
                return;

            hitPlayer.TakeDamage(damage);

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.HitPlayer);
            writer.Put(damage);

            hitPlayer.SendWriter(writer, DeliveryMethod.ReliableOrdered);
            if (!hitPlayer.IsAlive) {
                AlivePlayerCount -= 1;
                player.KillCounter += 1;

                writer = new NetworkWriter(InGameProtocol.TCPServerToClient.PlayerKill);
                writer.Put(hitPlayer.Id);
                writer.Put(player.Id);

                _server.SendAll(writer, DeliveryMethod.ReliableOrdered);

                if (AlivePlayerCount == 1) {
                    GameEnded = true;

                    writer = new NetworkWriter(InGameProtocol.TCPServerToClient.PlayerWin);
                    writer.Put(player.Id);

                    _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        private void StartThrowingMessage(InGamePlayer player, NetworkMessage msg) {
            short type = msg.GetShort();

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.StartThrowing);
            writer.Put(player.Id);
            writer.Put(type);
            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void ThrowingMessage(InGamePlayer player, NetworkMessage msg) {
            int itemID = msg.GetInt();
            short type = msg.GetShort();
            int index = msg.GetInt();

            player.Throwing(itemID, type, index);

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.Throwing);
            writer.Put(player.Id);
            writer.Put(itemID);
            writer.Put(type);
            writer.Put(index);
            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void ThrowableEndMessage(InGamePlayer player, NetworkMessage msg) {
            int index = msg.GetInt();

            player.ThrowingEnd(index);

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.ThrowableEnd);
            writer.Put(player.Id);
            writer.Put(index);
            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        /*********************************************************************************/

        private void PlayerMovementMessage(InGamePlayer player, NetworkMessage msg) {
            Vector3 position = msg.GetVector3();
            float speed_h = msg.GetFloat();
            float speed_v = msg.GetFloat();

            player.Position = position;

            var writer = new NetworkWriter(InGameProtocol.UDPServerToClient.Movement);
            writer.Put(player.Id);
            writer.Put(position);
            writer.Put(speed_h);
            writer.Put(speed_v);

            _server.SendAll(writer, DeliveryMethod.Unreliable);
        }

        private void PlayerTurnMessage(InGamePlayer player, NetworkMessage msg) {
            Quaternion orientation = msg.GetQuaternion();

            player.Rotation = orientation;

            var writer = new NetworkWriter(InGameProtocol.UDPServerToClient.Turn);
            writer.Put(player.Id);
            writer.Put(orientation);

            _server.SendAll(writer, DeliveryMethod.Unreliable);
        }

        private void ThrowableMoveMessage(InGamePlayer player, NetworkMessage msg) {
            int index = msg.GetInt();
            Vector3 pos = msg.GetVector3();
            Quaternion rot = msg.GetQuaternion();

            player.ThrowableMove(index, pos, rot);

            var writer = new NetworkWriter(InGameProtocol.UDPServerToClient.ThrowableMove);
            writer.Put(player.Id);
            writer.Put(index);
            writer.Put(pos);
            writer.Put(rot);
            _server.SendAll(writer, DeliveryMethod.Unreliable);
        }

        /*************************************************************************************/

        private void GenerateMap() {
            /*********** Generate Items ***********/
            ItemGenerator itemGenerator = new ItemGenerator();
            itemGenerator.Generate();
            _itemList = itemGenerator.ItemList;

            NetworkWriter writer = new NetworkWriter(InGameProtocol.TCPServerToClient.ItemList);
            writer.Put(itemGenerator.ItemList.Count);

            foreach (var item in itemGenerator.ItemList) {
                writer.Put(item.Value.SpawnIndex);
                writer.Put(item.Value.ID);
                writer.Put(item.Value.Type);
                writer.Put(item.Value.Rarity);
                writer.Put(item.Value.Quantity);
            }

            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);

            /*********** Generate Enigmas ***********/
            EnigmasGenerator enigmasGenerator = new EnigmasGenerator();
            enigmasGenerator.Generate();
            _enigmasList = enigmasGenerator.EnigmasList;

            writer = new NetworkWriter(InGameProtocol.TCPServerToClient.EnigmasList);
            writer.Put(enigmasGenerator.EnigmasList.Count);

            foreach (var enimgas in enigmasGenerator.EnigmasList) {
                writer.Put(enimgas.Value.SpawnIndex);
                writer.Put(enimgas.Value.EnigmaID);
                writer.Put(enimgas.Value.EnigmaType);
            }

            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void RoomLoop() {
            int counter = 20;
            NetworkWriter writer = null;

            while (PlayerCount < MinPlayerToStart) {
                Thread.Sleep(1000);
                if (_stopRoomLoop)
                    return;
            }

            while (counter > 0) {
                Thread.Sleep(1000);
                if (_stopRoomLoop)
                    return;
                if (PlayerCount == 32 && counter > 10)
                    counter = 10;
                else
                    counter -= 1;

                writer = new NetworkWriter(InGameProtocol.TCPServerToClient.Counter);
                writer.Put(counter);
                _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
            }

            Console.WriteLine("Game started");
            GameStarted = true;
            writer = new NetworkWriter(InGameProtocol.TCPServerToClient.GameStart);
            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
            AlivePlayerCount = PlayerCount;

            GenerateMap();

            MapEvent mapEvent = new MapEvent();
            mapEvent.ItemList = _itemList;
            mapEvent.EnigmasList = _enigmasList;
            mapEvent.Server = _server;

            while (!GameEnded) {
                Thread.Sleep(1000);
                if (_stopRoomLoop)
                    return;
                mapEvent.Update();
            }

            while (!_stopRoomLoop)
                Thread.Sleep(1000);
        }

        /*************************************************************************************/
        private sealed class IdEqualityComparer : IEqualityComparer<GameInstance> {
            public bool Equals(GameInstance x, GameInstance y) {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Id == y.Id;
            }

            public int GetHashCode(GameInstance obj) {
                return obj.Id;
            }
        }

        private static readonly IEqualityComparer<GameInstance> IdComparerInstance = new IdEqualityComparer();

        public static IEqualityComparer<GameInstance> IdComparer {
            get { return IdComparerInstance; }
        }
    }
}