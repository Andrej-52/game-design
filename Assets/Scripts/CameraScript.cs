using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 5f;

    private float offsetX;

    void Start()
    {
        offsetX = transform.position.x - player.position.x;
    }

    void LateUpdate()
    {
        Vector3 targetPosition = new Vector3(
            player.position.x + offsetX,
            transform.position.y,
            transform.position.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}