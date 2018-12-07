using System.Collections.Generic;
using LiteNetLib;
using PralineServer.Player;

namespace PralineServer {
    public class InGameServer : MyNetworkServer {
        public Dictionary<NetPeer, InGamePlayer> Players;
        private List<NetPeer> _unknownPlayers;

        public delegate void OnPlayerDisconnectedDelegate(InGamePlayer player);
        public OnPlayerDisconnectedDelegate OnDisconnect;

        public InGameServer(int maxPlayer) : base(maxPlayer) {
            _unknownPlayers = new List<NetPeer>();
            Players = new Dictionary<NetPeer, InGamePlayer>();
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

        public void RegisterPlayer(InGamePlayer player) {
            _unknownPlayers.Remove(player.Peer);
            Players.Add(player.Peer, player);
        }

        public delegate void CustomNetworkMessageDelegate(InGamePlayer player, NetworkMessage msg);

        public void RegisterHandler(short msgType, CustomNetworkMessageDelegate handler) {
            base.RegisterHandler(msgType, (peer, msg) =>  {
                InGamePlayer player = null;

                if (Players.ContainsKey(peer))
                    player = Players[peer];
                handler.Invoke(player, msg);
            });
        }
    }
}