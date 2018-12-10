using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;

#if UNITY || UNITY_EDITOR
using UnityEngine;
#endif

namespace PA.Networking {
    public class NetworkWriter : NetDataWriter {
        public NetworkWriter(short msgType) {
            Put(msgType);
        }

        public void Put(Vector3 vec) {
            Put(vec.x);
            Put(vec.y);
            Put(vec.z);
        }

        public void Put(Quaternion quat) {
            Put(quat.x);
            Put(quat.y);
            Put(quat.z);
            Put(quat.w);
        }
    }
}