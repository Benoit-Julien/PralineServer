using LiteNetLib;

namespace PA.Server.Player {
    public class GlobalPlayer : APlayer {
        public GlobalPlayer(NetPeer peer) : base(peer) { }

        public GlobalPlayer(NetPeer peer, int id) : base(peer, id) { }
    }
}