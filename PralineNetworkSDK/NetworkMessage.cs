using LiteNetLib;
using LiteNetLib.Utils;

namespace PA.Networking {
    public class NetworkMessage : NetDataReader {
        public NetPeer Peer;

        public NetworkMessage(NetDataReader reader) {
            SetSource(reader.RawData, reader.Position);
        }

        public Types.Vector3 GetVector3() {
            float x = GetFloat();
            float y = GetFloat();
            float z = GetFloat();

            return new Types.Vector3(x, y, z);
        }

        public Types.Quaternion GetQuaternion() {
            float x = GetFloat();
            float y = GetFloat();
            float z = GetFloat();
            float w = GetFloat();

            return new Types.Quaternion(x, y, z, w);
        }
    }
}