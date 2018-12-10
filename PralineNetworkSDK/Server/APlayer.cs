using System.Collections.Generic;
using LiteNetLib;

namespace PA.Networking.Server.Player {
    public class APlayer {
        public NetPeer Peer;

        public int Id;
        public string Name;

        public APlayer(NetPeer peer) {
            Peer = peer;
            Id = IDGenerator.getInstance().GenerateUniqueID();
        }

        public APlayer(NetPeer peer, int id) {
            Peer = peer;
            Id = id;
            IDGenerator.getInstance().SaveUniqueID(id);
        }

        ~APlayer() {
            IDGenerator.getInstance().RemoveUniqueID(Id);
        }

        public void SendWriter(NetworkWriter writer, DeliveryMethod method) {
            Peer.Send(writer, method);
        }

        private sealed class IdEqualityComparer : IEqualityComparer<APlayer> {
            public bool Equals(APlayer a, APlayer b) {
                if (ReferenceEquals(a, b)) return true;
                if (ReferenceEquals(a, null)) return false;
                if (ReferenceEquals(b, null)) return false;
                if (a.GetType() != b.GetType()) return false;
                return a.Id == b.Id;
            }

            public int GetHashCode(APlayer obj) {
                return obj.Id;
            }
        }

        private static readonly IEqualityComparer<APlayer> IdComparerInstance = new IdEqualityComparer();

        public static IEqualityComparer<APlayer> IdComparer {
            get { return IdComparerInstance; }
        }
    }
}