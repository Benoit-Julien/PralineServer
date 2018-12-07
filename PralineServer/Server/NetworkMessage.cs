using LiteNetLib;
using LiteNetLib.Utils;

namespace PA.Server {
    public class NetworkMessage : NetDataReader {
        public NetPeer Peer;
        
        public NetworkMessage(NetDataReader reader) {
            SetSource(reader.RawData);
        }

        public Vector3 GetVector3() {
            float x = GetFloat();
            float y = GetFloat();
            float z = GetFloat();
            
            return new Vector3(x, y, z);
        }

        public Quaternion GetQuaternion() {
            float x = GetFloat();
            float y = GetFloat();
            float z = GetFloat();
            float w = GetFloat();
            
            return new Quaternion(x, y, z, w);
        }
    }
}