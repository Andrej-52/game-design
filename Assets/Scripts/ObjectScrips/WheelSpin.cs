using UnityEngine;

public class WheelSpin : MonoBehaviour
{
    public Transform[] wheels;
    public float wheelRadius = 1f;
    public Vector3 axle = Vector3.right;

    Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    void LateUpdate()
    {
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;

        float distance = Vector3.Dot(delta, transform.forward);
        float degrees = distance / (2f * Mathf.PI * wheelRadius) * 360f;

        foreach (Transform wheel in wheels)
            wheel.Rotate(axle, degrees, Space.Self);
    }
}