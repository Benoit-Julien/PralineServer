using System;

namespace PA.Networking.Types {
    public class Vector3 {
        public float x;
        public float y;
        public float z;

        public Vector3() {
            this.x = 0;
            this.y = 0;
            this.z = 0;
        }

        public Vector3(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /// <summary>
        ///   <para>Returns the length of this vector (Read Only).</para>
        /// </summary>
        public float magnitude {
            get {
                double x2 = (double) this.x * (double) this.x;
                double y2 = (double) this.y * (double) this.y;
                double z2 = (double) this.z * (double) this.z;
                return (float) Math.Sqrt(x2 + y2 + z2);
            }
        }
    }
}