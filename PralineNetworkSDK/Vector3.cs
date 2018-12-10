#if UNITY || UNITY_EDITOR
#else
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
}
#endif