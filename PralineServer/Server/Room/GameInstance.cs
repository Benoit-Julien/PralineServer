using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LiteNetLib;
using PA.Networking.Server.Player;
using PA.Networking.Types;

namespace PA.Networking.Server.Room {
    public class GameInstance {
        public int MaxPlayer;
        public int MinPlayerToStart;
        public int TimeBeforeStart;

        public int Id;
        public int ListenPort;
        public int PlayerCount;

        public Dictionary<int, InGamePlayer> PlayerList;
        public Dictionary<int, InGamePlayer> AlivePlayerList;
        private Dictionary<int, APlayer> _expectedPlayers;

        public bool GameStarted;
        public bool GameEnded;
        public bool Joinable;

        private Thread _roomLoop;
        private Thread _updater;
        private MyNetworkServer<InGamePlayer> _server;
        private bool _stopRoom;

        private ServerManager _manager;
        private Dictionary<int, ItemGenerator.Item> _itemList;
        private Dictionary<int, EnigmasGenerator.Enigmas> _enigmasList;

        public GameInstance(ServerManager manager, int maxPlayer = 32, int minPlayerToStart = 12, int timeBeforeStart = 60) {
            MaxPlayer = maxPlayer;
            MinPlayerToStart = minPlayerToStart;
            TimeBeforeStart = timeBeforeStart;
            _manager = manager;

            PlayerCount = 0;
            Id = IDGenerator.getInstance().GenerateUniqueID();
            PlayerList = new Dictionary<int, InGamePlayer>();
            AlivePlayerList = null;
            _expectedPlayers = new Dictionary<int, APlayer>();
            GameStarted = false;
            GameEnded = false;
            Joinable = true;

            _server = new MyNetworkServer<InGamePlayer>(MaxPlayer);
            _server.OnDisconnect += OnPlayerDisconnect;

            /* TCP Protocol */
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.ConnectionConfirm, ConnectionConfirmMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.QuitRoom, QuitGameMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.Jump, PlayerJumpMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.Crouch, PlayerCrouchMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.DropTrain, DropTrainMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.Reloading, PlayerReloadingMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.EnigmaOpened, EnigmaOpenedMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.OpenCrate, OpenCrateMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.TakeItem, TakeItemMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.DropItem, DropItemMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.SwitchItem, SwitchItemMessage);
            _server.RegisterHandler(InGameProtocol.TCPClientToServer.SwitchKnife, SwitchKnifeMessage);
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

            _stopRoom = false;
            _roomLoop = new Thread(RoomLoop);
            _roomLoop.Start();

            _updater = new Thread(Updater);
            _updater.Start();
        }

        ~GameInstance() {
            IDGenerator.getInstance().RemoveUniqueID(Id);
        }

        public void StopRoomInstance() {
            _stopRoom = true;
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
            if (PlayerCount == MaxPlayer)
                Joinable = false;
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

                if (PlayerList[playerId].IsAlive && AlivePlayerList != null)
                    AlivePlayerList.Remove(playerId);
                PlayerList.Remove(playerId);
                PlayerCount -= 1;
                return true;
            }

            return false;
        }

        private void OnPlayerDisconnect(InGamePlayer player) {
            Logger.WriteLine("Room {0} : Player {1} disconnected to room", Id, player.Id);
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
            Logger.WriteLine("Room {0} : Player {1} quit.", Id, player.Id);
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

        private void DropTrainMessage(InGamePlayer player, NetworkMessage msg) {
            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.DropTrain);
            writer.Put(player.Id);

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
            Logger.WriteLine("Room {0} : Player {1} open enigma {2}.", Id, player.Id, enigmaID);

            var enigmas = _enigmasList[enigmaID];
            enigmas.EnigmaOpened = true;

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.EnigmaOpened);
            writer.Put(player.Id);
            writer.Put(enigmaID);
            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void OpenCrateMessage(InGamePlayer player, NetworkMessage msg) {
            int crateID = msg.GetInt();
            Logger.WriteLine("Room {0} : Player {1} open crate {2}.");

            var itemList = ItemGenerator.GenerateCrateItem();
            
            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.OpenCrate);
            writer.Put(player.Id);
            writer.Put(crateID);
            writer.Put(itemList.Count);

            foreach (var item in itemList) {
                writer.Put(item.ID);
                writer.Put(item.Type);
                writer.Put(item.Rarity);
                writer.Put(item.Quantity);
                _itemList.Add(item.ID, item);
                
                Logger.WriteLine("Room {0} : Item generated on crate {1} -> id = {2}\tquantity = {3}\ttype = {4}",
                    Id,
                    crateID,
                    item.ID,
                    item.Quantity,
                    (ItemTypes.ItemEnum) item.Type);
            }
            
            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void TakeItemMessage(InGamePlayer player, NetworkMessage msg) {
            int itemID = msg.GetInt();
            int quantity = msg.GetInt();

            if (!_itemList.ContainsKey(itemID))
                return;

            var item = _itemList[itemID];

            if (player.TakeItem(ref item, quantity)) {
                Logger.WriteLine("Room {0} : Remove item {1} because there is no more quantity.", Id, itemID);
                _itemList.Remove(itemID);
                quantity = item.Quantity;
            }

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

            Logger.WriteLine("Room {0} : Player {1} drop item {2}", Id, player.Id, itemID);

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.DropItem);
            writer.Put(player.Id);
            writer.Put(itemID);
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

        private void SwitchKnifeMessage(InGamePlayer player, NetworkMessage msg) {
            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.SwitchKnife);
            writer.Put(player.Id);
            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void UseItemMessage(InGamePlayer player, NetworkMessage msg) {
            int itemID = msg.GetInt();
            int quantity = msg.GetInt();

            player.UseItem(itemID, quantity);

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.UseItem);
            writer.Put(player.Id);
            writer.Put(itemID);
            writer.Put(quantity);
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
            if (!GameStarted)
                return;

            int hitPlayerID = msg.GetInt();
            short damage = msg.GetShort();

            InGamePlayer hitPlayer = PlayerList[hitPlayerID];
            if (!hitPlayer.IsAlive)
                return;

            if (PlayerTakeDamage(hitPlayer, damage, true)) {
                Logger.WriteLine("Room {0} : Player {1} killed by Player {2}", Id, hitPlayer.Id, player.Id);
                player.KillCounter += 1;

                var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.PlayerKill);
                writer.Put(hitPlayer.Id);
                writer.Put(player.Id);

                _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
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

        private bool PlayerTakeDamage(InGamePlayer player, short damage, bool affectShield) {
            player.TakeDamage(damage, affectShield);

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.HitPlayer);
            writer.Put(player.Id);
            writer.Put(damage);
            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
            if (!player.IsAlive) {
                foreach (var item in player.Inventory) {
                    writer = new NetworkWriter(InGameProtocol.TCPServerToClient.DropItem);
                    writer.Put(player.Id);
                    writer.Put(item.Value.ID);
                    writer.Put(item.Value.ID);
                    writer.Put(item.Value.Quantity);
                    _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
                    _itemList.Add(item.Value.ID, item.Value);
                }

                player.Inventory.Clear();

                AlivePlayerList.Remove(player.Id);

                if (AlivePlayerList.Count == 1) {
                    var winPlayer = AlivePlayerList.First().Value;

                    writer = new NetworkWriter(InGameProtocol.TCPServerToClient.PlayerWin);
                    writer.Put(winPlayer.Id);
                    _server.SendAll(writer, DeliveryMethod.ReliableOrdered);

                    GameEnded = true;
                    Logger.WriteLine("Room {0} : Player {1} win the game", Id, winPlayer.Id);
                }

                return true;
            }

            return false;
        }

        private void Updater() {
            while (!_stopRoom)
                _server.PollEvents();
        }

        private void GenerateMap() {
            /*********** Generate Items ***********/
            Logger.WriteLine("Room {0} : Generating items", Id);
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
                Logger.WriteLine("Room {0} : Item generated -> id = {1}\tquantity = {2}\ttype = {3}",
                    Id,
                    item.Value.ID,
                    item.Value.Quantity,
                    (ItemTypes.ItemEnum) item.Value.Type);
            }

            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
            Logger.WriteLine("Room {0} : Generating items finish", Id);

            /*********** Generate Enigmas ***********/
            Logger.WriteLine("Room {0} : Generating enigmas", Id);
            EnigmasGenerator enigmasGenerator = new EnigmasGenerator();
            enigmasGenerator.Generate();
            _enigmasList = enigmasGenerator.EnigmasList;

            writer = new NetworkWriter(InGameProtocol.TCPServerToClient.EnigmasList);
            writer.Put(enigmasGenerator.EnigmasList.Count);

            foreach (var enimga in enigmasGenerator.EnigmasList) {
                writer.Put(enimga.Value.SpawnIndex);
                writer.Put(enimga.Value.EnigmaID);
                writer.Put(enimga.Value.EnigmaType);
                Logger.WriteLine("Room {0} : Enigma generated -> id = {1}\ttype = {2}",
                    Id,
                    enimga.Value.EnigmaID,
                    enimga.Value.EnigmaType);
            }

            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
            Logger.WriteLine("Room {0} : Generating enigmas finish", Id);
        }

        private void RoomLoop() {
            int counter = TimeBeforeStart;
            NetworkWriter writer = null;

            while (!GameStarted && PlayerCount < MinPlayerToStart) {
                Thread.Sleep(1000);
                if (_stopRoom)
                    return;
            }

            Logger.WriteLine("Room {0} : Enough player as join, start counter.", Id);

            while (!GameStarted && counter > 0) {
                Thread.Sleep(1000);
                if (_stopRoom)
                    return;
                if (PlayerCount == 32 && counter > 10)
                    counter = 10;
                else
                    counter -= 1;

                if (counter <= 10)
                    Joinable = false;

                writer = new NetworkWriter(InGameProtocol.TCPServerToClient.Counter);
                writer.Put(counter);
                _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
            }

            Logger.WriteLine("Room {0} : Game started", Id);
            GameStarted = true;
            writer = new NetworkWriter(InGameProtocol.TCPServerToClient.GameStart);
            _server.SendAll(writer, DeliveryMethod.ReliableOrdered);
            AlivePlayerList = PlayerList;

            GenerateMap();

            MapEvent mapEvent = new MapEvent();
            mapEvent.ItemList = _itemList;
            mapEvent.EnigmasList = _enigmasList;
            mapEvent.Server = _server;

            while (!GameEnded) {
                if (_stopRoom)
                    break;
                mapEvent.Update();
                foreach (var player in PlayerList) {
                    if (player.Value.IsAlive && mapEvent.CheckPlayerInPlasma(player.Value.Position))
                        PlayerTakeDamage(player.Value, 1, false);
                }

                Thread.Sleep(1000);
            }

            Logger.WriteLine("Room {0} : Game ended", Id);

            while (!_stopRoom)
                Thread.Sleep(1000);
            mapEvent.Stop();
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