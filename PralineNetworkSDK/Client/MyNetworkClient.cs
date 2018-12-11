using System;
using System.Collections.Generic;
using LiteNetLib;

namespace PA.Networking.Client {
    public class MyNetworkClient {
        private NetManager _client;
        private EventBasedNetListener _listener;
        private NetPeer _peer;
        private Dictionary<short, NetworkMessageDelegate> _msgHandler;

        public delegate void NetworkMessageDelegate(NetworkMessage msg);
        public static readonly string AcceptKey = "Praline's Network";

        public delegate void OnPlayerConnectDisconnectDelegate();
        public event OnPlayerConnectDisconnectDelegate OnConnect;
        public event OnPlayerConnectDisconnectDelegate OnDisconnect;

        public MyNetworkClient() {
            _msgHandler = new Dictionary<short, NetworkMessageDelegate>();

            _listener = new EventBasedNetListener();
            _client = new NetManager(_listener);

            _listener.PeerConnectedEvent += OnConnectEvent;
            _listener.PeerDisconnectedEvent += OnDisconnectEvent;
            _listener.NetworkReceiveEvent += OnNetworkReceive;
        }

        public void Connect(string addressIp, int port) {
            _client.Start();
            _client.Connect(addressIp, port, AcceptKey);
        }

        public void Disconnect() {
            _client.Stop();
        }

        public void PollEvents() {
            _client.PollEvents();
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

        public void SendWriter(NetworkWriter writer, DeliveryMethod method) {
            if (_peer == null)
                return;
            _peer.Send(writer, method);
        }

        private void OnConnectEvent(NetPeer peer) {
            _peer = peer;
            if (OnConnect != null) OnConnect.Invoke();
            Logger.WriteLine("Client connected to the server.");
        }

        private void OnDisconnectEvent(NetPeer peer, DisconnectInfo info) {
            _peer = null;
            if (OnDisconnect != null) OnDisconnect.Invoke();
            Logger.WriteLine("Client disconnected to the server.");
        }

        private void OnNetworkReceive(NetPeer peer, NetPacketReader msg, DeliveryMethod method) {
            try {
                short msgType = msg.GetShort();

                if (_msgHandler.ContainsKey(msgType)) {
                    var handlers = _msgHandler[msgType];
                    var reader = new NetworkMessage(msg);
                    reader.Peer = peer;
                    handlers.Invoke(reader);
                }
            }
            catch (Exception e) {
                Logger.WriteLine(e);
            }

            msg.Recycle();
        }
    }
}