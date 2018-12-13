using System;
using System.Collections.Generic;
using LiteNetLib;

namespace PA.Networking.Server {
    public class MyNetworkServer<PlayerType> where PlayerType : Player.APlayer {
        private NetManager _server;
        private EventBasedNetListener _listener;
        private Dictionary<short, NetworkMessageDelegate> _msgHandler;

        private List<NetPeer> _unknownPlayers;

        public delegate void NetworkMessageDelegate(PlayerType player, NetworkMessage msg);
        public static readonly string AcceptKey = "Praline's Network";
        public int Port;
        public int MaxPeer;

        public delegate void OnPlayerDisconnectedDelegate(PlayerType player);
        public event OnPlayerDisconnectedDelegate OnDisconnect;

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
            _msgHandler = new Dictionary<short, NetworkMessageDelegate>();

            _listener.ConnectionRequestEvent += ConnectionRequest;
            _listener.PeerConnectedEvent += PeerConnected;
            _listener.PeerDisconnectedEvent += PeerDisconnected;
            _listener.NetworkReceiveEvent += NetworkReceive;

            MaxPeer = maxPeer;

            _unknownPlayers = new List<NetPeer>();
            Players = new Dictionary<NetPeer, PlayerType>();
        }

        public bool Start() {
            if (_server.Start()) {
                Port = _server.LocalPort;
                Logger.WriteLine("Start server on port {0}", Port);
                return true;
            }

            return false;
        }

        public bool Start(int port) {
            if (_server.Start(port)) {
                Port = port;
                Logger.WriteLine("Start server on port {0}", Port);
                return true;
            }

            return false;
        }

        public void Stop() {
            _server.Stop();
            _unknownPlayers.Clear();
            Players.Clear();
            Logger.WriteLine("Stop server of port {0}", Port);
        }

        public void PollEvents() {
            _server.PollEvents();
        }

        public void RegisterHandler(short msgType, NetworkMessageDelegate handler) {
            if (!_msgHandler.ContainsKey(msgType))
                _msgHandler.Add(msgType, handler);
            else
                _msgHandler[msgType] = handler;
        }

        public void UnregisterHandler(short msgType) {
            if (_msgHandler.ContainsKey(msgType))
                _msgHandler.Remove(msgType);
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
            Logger.WriteLine("Server [{0}] : New peer connected.", Port);
            _unknownPlayers.Add(peer);
        }

        private void PeerDisconnected(NetPeer peer, DisconnectInfo info) {
            if (!Players.ContainsKey(peer))
                return;
            
            var p = Players[peer];
            OnDisconnect?.Invoke(p);
            Players.Remove(peer);
            Logger.WriteLine("Server [{0}] : Peer  disconnected.", Port);
        }

        private void NetworkReceive(NetPeer fromPeer, NetPacketReader msg, DeliveryMethod method) {
            short msgType;
            try {
                msgType = msg.GetShort();
            }
            catch (Exception e) {
                Logger.WriteLine(e);
                return;
            }

            if (_msgHandler.ContainsKey(msgType)) {
                var handlers = _msgHandler[msgType];
                var reader = new NetworkMessage(msg);
                reader.Peer = fromPeer;
                PlayerType player = null;

                if (Players.ContainsKey(fromPeer))
                    player = Players[fromPeer];
                handlers.Invoke(player, reader);
            }

            msg.Recycle();
        }
    }
}