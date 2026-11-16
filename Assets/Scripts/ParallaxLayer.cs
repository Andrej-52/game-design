using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public float scrollSpeed = 0.002f;
    public float autoScroll = 0f;
    public float distance = 500f;
    [Range(0.1f, 1f)] public float size = 1f;
    [Range(-0.5f, 0.5f)] public float verticalPosition = 0f;

    Camera cam;
    Material mat;
    float autoOffset;

    void Start()
    {
        cam = Camera.main;
        mat = GetComponent<Renderer>().material;

        transform.SetParent(cam.transform);
        transform.localRotation = Quaternion.identity;
    }

    void LateUpdate()
    {
        float viewHeight = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float viewWidth = viewHeight * cam.aspect;
        float layerHeight = viewHeight * size;

        Texture tex = mat.mainTexture;
        float tilingX = viewWidth / (layerHeight * tex.width / tex.height);

        transform.localPosition = new Vector3(0f, verticalPosition * viewHeight, distance);
        transform.localScale = new Vector3(viewWidth, layerHeight, 1f);

        autoOffset += autoScroll * Time.deltaTime;

        mat.mainTextureScale = new Vector2(tilingX, 1f);
        mat.mainTextureOffset = new Vector2((cam.transform.position.x * scrollSpeed + autoOffset) * tilingX, 0f);
    }
}