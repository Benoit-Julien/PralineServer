using System.Collections.Generic;
using LiteNetLib;
using PralineServer.Player;

namespace PralineServer {
    public class GlobalServer : MyNetworkServer {
        public Dictionary<NetPeer, GlobalPlayer> Players;
        private List<NetPeer> _unknownPlayers;

        public delegate void OnPlayerDisconnectedDelegate(GlobalPlayer player);
        public OnPlayerDisconnectedDelegate OnDisconnect;

        public GlobalServer() : base((int) ushort.MaxValue - 1) {
            _unknownPlayers = new List<NetPeer>();
            Players = new Dictionary<NetPeer, GlobalPlayer>();
        }

        protected override void StartServerEvent() { }

        protected override void StopServerEvent() {
            _unknownPlayers.Clear();
            Players.Clear();
        }

        protected override void ConnectEvent(NetPeer peer) {
            _unknownPlayers.Add(peer);
        }

        protected override void DisconnectEvent(NetPeer peer) {
            var p = Players[peer];
            if (OnDisconnect != null)
                OnDisconnect(p);
            Players.Remove(peer);
        }

        public override void SendAll(NetworkWriter writer, DeliveryMethod method) {
            foreach (var player in Players)
                player.Value.SendWriter(writer, method);
        }

        public void RegisterPlayer(GlobalPlayer player) {
            _unknownPlayers.Remove(player.Peer);
            Players.Add(player.Peer, player);
        }

        public delegate void CustomNetworkMessageDelegate(GlobalPlayer player, NetworkMessage msg);

        public void RegisterHandler(short msgType, CustomNetworkMessageDelegate handler) {
            base.RegisterHandler(msgType, (peer, msg) => {
                GlobalPlayer player = null;

                if (Players.ContainsKey(peer))
                    player = Players[peer];
                handler.Invoke(player, msg);
            });
        }
    }
}