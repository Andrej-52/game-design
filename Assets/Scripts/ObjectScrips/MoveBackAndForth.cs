using UnityEngine;

public class MoveBackAndForth : MonoBehaviour
{
    public float distance = 10f;
    public float speed = 2f;

    private float startZ;

    void Start()
    {
        startZ = transform.position.z;
    }

    void Update()
    {
        float z = startZ + Mathf.PingPong(Time.time * speed, distance);

        transform.position = new Vector3(
            transform.position.x,
            transform.position.y,
            z
        );
    }
}