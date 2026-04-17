using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraFollowPlayer : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Player that the camera is following.")]
    private Transform target;

    [SerializeField]
    [Tooltip("How fast the camera follows the player.")]
    private float smoothSpeed = 5f;

    [SerializeField]
    [Tooltip("How much the camera is offset.")]
    private Vector3 offset = new Vector3(0, 0, -10f);

    [Header("Pixel stability")]
    [Tooltip("Match your tile / character Pixels Per Unit (e.g. Floor_Connecting uses 100). Used when screen-pixel snap is off.")]
    [SerializeField] private float referencePixelsPerUnit = 100f;

    [Tooltip("For orthographic cameras: snap to whole SCREEN pixels (world units per pixel). Strong fix for horizontal shimmer when panning.")]
    [SerializeField] private bool snapToScreenPixelGrid = true;

    [Tooltip("Round camera X/Y to world units of 1/referencePixelsPerUnit (only used if screen-pixel snap is off).")]
    [SerializeField] private bool snapCameraPositionToPixelGrid = true;

    [Tooltip("Off = camera snaps directly to the pixel-locked target each frame (best for static tiles). On = smooth then snap.")]
    [SerializeField] private bool useSmoothedFollow = false;

    [Tooltip("Snap target+offset before follow (same grid as final snap).")]
    [SerializeField] private bool snapDesiredPositionToPixelGrid = true;

    [Tooltip("If this GameObject has URP Pixel Perfect Camera, configure assets PPU and grid snapping on Awake.")]
    [SerializeField] private bool configurePixelPerfectCameraIfPresent = true;

    [Tooltip("URP allows one Grid Snapping mode. Upscale Render Texture is usually best for crisp fullscreen pixel art.")]
    [SerializeField] private bool pixelPerfectUseUpscaleRenderTexture = true;

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();

        if (!configurePixelPerfectCameraIfPresent)
            return;

        PixelPerfectCamera ppc = GetComponent<PixelPerfectCamera>();
        if (ppc == null)
            return;

        int ap = Mathf.Max(1, Mathf.RoundToInt(referencePixelsPerUnit));
        ppc.assetsPPU = ap;
        ppc.gridSnapping = pixelPerfectUseUpscaleRenderTexture
            ? PixelPerfectCamera.GridSnapping.UpscaleRenderTexture
            : PixelPerfectCamera.GridSnapping.PixelSnapping;
    }

    private static void SnapXYToArtPpu(ref Vector3 v, float ppu)
    {
        if (ppu < 0.0001f)
            return;
        v.x = Mathf.Round(v.x * ppu) / ppu;
        v.y = Mathf.Round(v.y * ppu) / ppu;
    }

    private static void SnapXYToScreenPixels(Camera cam, ref Vector3 v)
    {
        if (cam == null || !cam.orthographic)
            return;
        int w = Screen.width;
        int h = Screen.height;
        if (w < 1 || h < 1)
            return;
        float halfHeight = cam.orthographicSize;
        float unitsPerPixelY = (2f * halfHeight) / h;
        float unitsPerPixelX = (2f * halfHeight * cam.aspect) / w;
        if (unitsPerPixelX < 1e-8f || unitsPerPixelY < 1e-8f)
            return;
        v.x = Mathf.Round(v.x / unitsPerPixelX) * unitsPerPixelX;
        v.y = Mathf.Round(v.y / unitsPerPixelY) * unitsPerPixelY;
    }

    private void ApplyPositionSnapping(ref Vector3 v)
    {
        if (snapToScreenPixelGrid)
        {
            SnapXYToScreenPixels(_camera, ref v);
            return;
        }

        if (snapCameraPositionToPixelGrid && referencePixelsPerUnit > 0.01f)
            SnapXYToArtPpu(ref v, referencePixelsPerUnit);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        if (snapDesiredPositionToPixelGrid)
            ApplyPositionSnapping(ref desiredPosition);

        Vector3 nextPosition;
        if (useSmoothedFollow)
        {
            nextPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            nextPosition.z = desiredPosition.z;
        }
        else
            nextPosition = desiredPosition;

        ApplyPositionSnapping(ref nextPosition);

        transform.position = nextPosition;
    }
}
