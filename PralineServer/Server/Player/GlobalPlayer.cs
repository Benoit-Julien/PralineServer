using LiteNetLib;

namespace PA.Networking.Server.Player {
    public class GlobalPlayer : APlayer {
        public GlobalPlayer(NetPeer peer) : base(peer) { }

        public GlobalPlayer(NetPeer peer, int id) : base(peer, id) { }
    }
}