using System;
using System.Collections.Generic;
using LiteNetLib;

namespace PA.Server {
    public class MyNetworkServer<PlayerType> where PlayerType : Player.APlayer {
        private NetManager _server;
        private EventBasedNetListener _listener;
        private Dictionary<short, List<NetworkMessageDelegate>> _msgHandler;

        private List<NetPeer> _unknownPlayers;

        public delegate void NetworkMessageDelegate(PlayerType player, NetworkMessage msg);
        public static readonly string AcceptKey = "Praline's Network";
        public int Port;
        public int MaxPeer;

        public delegate void OnPlayerDisconnectedDelegate(PlayerType player);
        public OnPlayerDisconnectedDelegate OnDisconnect;

        public Dictionary<NetPeer, PlayerType> Players;

        public MyNetworkServer() {
            Init(ushort.MaxValue - 1);
        }

        public MyNetworkServer(int maxPeer) {
            Init(maxPeer);
        }

        private void Init(int maxPeer) {
            _listener = new EventBasedNetListener();
            _server = new NetManager(_listener);
            _msgHandler = new Dictionary<short, List<NetworkMessageDelegate>>();

            _listener.ConnectionRequestEvent += ConnectionRequest;
            _listener.PeerConnectedEvent += PeerConnected;
            _listener.PeerDisconnectedEvent += PeerDisconnected;
            _listener.NetworkReceiveEvent += NetworkReceive;

            MaxPeer = maxPeer;
        }

        public bool Start() {
            if (_server.Start()) {
                Port = _server.LocalPort;
                return true;
            }

            return false;
        }

        public bool Start(int port) {
            if (_server.Start(port)) {
                Port = port;
                return true;
            }

            return false;
        }

        public void Stop() {
            _server.Stop();
            _unknownPlayers.Clear();
            Players.Clear();
        }

        public void PollEvents() {
            _server.PollEvents();
        }

        public void RegisterHandler(short msgType, NetworkMessageDelegate handler) {
            if (!_msgHandler.ContainsKey(msgType))
                _msgHandler.Add(msgType, new List<NetworkMessageDelegate>());
            _msgHandler[msgType].Add(handler);
        }

        public void SendAll(NetworkWriter writer, DeliveryMethod method) {
            foreach (var player in Players)
                player.Value.SendWriter(writer, method);
        }

        public void RegisterPlayer(PlayerType player) {
            _unknownPlayers.Remove(player.Peer);
            Players.Add(player.Peer, player);
        }

        private void ConnectionRequest(ConnectionRequest request) {
            if (_server.PeersCount < MaxPeer /* max connections */)
                request.AcceptIfKey(AcceptKey);
            else
                request.Reject();
        }

        private void PeerConnected(NetPeer peer) {
            _unknownPlayers.Add(peer);
        }

        private void PeerDisconnected(NetPeer peer, DisconnectInfo info) {
            var p = Players[peer];
            if (OnDisconnect != null)
                OnDisconnect(p);
            Players.Remove(peer);
        }

        private void NetworkReceive(NetPeer fromPeer, NetPacketReader msg, DeliveryMethod method) {
            try {
                short msgType = msg.GetShort();

                if (_msgHandler.ContainsKey(msgType)) {
                    var handlers = _msgHandler[msgType];
                    var reader = new NetworkMessage(msg);
                    reader.Peer = fromPeer;
                    PlayerType player = null;

                    if (Players.ContainsKey(fromPeer))
                        player = Players[fromPeer];
                    foreach (var h in handlers) h.Invoke(player, reader);
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }

            msg.Recycle();
        }
    }
}