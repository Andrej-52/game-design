using UnityEngine;

public class BusDriveZ : MonoBehaviour
{
    public float distance = 60f;
    public float speed = 8f;
    public bool turnAround = true;

    public Transform[] wheels;
    public Vector3 wheelAxis = Vector3.forward;
    public float wheelRadius = 1.2f;

    private float startZ;
    private float timer;
    private float lastOffset;
    private int direction = 1;

    void Start()
    {
        startZ = transform.position.z;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float offset = Mathf.PingPong(timer * speed, distance);

        int newDirection = offset >= lastOffset ? 1 : -1;
        if (turnAround && newDirection != direction)
            transform.Rotate(0f, 180f, 0f, Space.World);
        direction = newDirection;
        lastOffset = offset;

        Vector3 p = transform.position;
        transform.position = new Vector3(p.x, p.y, startZ + offset);

        float spin = speed / wheelRadius * Mathf.Rad2Deg * Time.deltaTime;
        if (!turnAround) spin *= direction;
        foreach (Transform w in wheels)
            if (w) w.Rotate(wheelAxis, spin, Space.Self);
    }
}