using LiteNetLib.Utils;

namespace PA.Networking {
    public class NetworkWriter : NetDataWriter {
        public NetworkWriter(short msgType) {
            Put(msgType);
        }

        public void Put(Types.Vector3 vec) {
            Put(vec.x);
            Put(vec.y);
            Put(vec.z);
        }

        public void Put(Types.Quaternion quat) {
            Put(quat.x);
            Put(quat.y);
            Put(quat.z);
            Put(quat.w);
        }
    }
}