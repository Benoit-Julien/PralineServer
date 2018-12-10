namespace PA.Networking.Types {
    public class Quaternion {
        public float x;
        public float y;
        public float z;
        public float w;

        public Quaternion() {
            this.x = 0;
            this.y = 0;
            this.z = 0;
            this.w = 0;
        }

        public Quaternion(float x, float y, float z, float w) {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }
}