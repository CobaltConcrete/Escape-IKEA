using UnityEngine;

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

    void LateUpdate()
    {
        if (target == null) return;

        // Target position with offset
        Vector3 desiredPosition = target.position + offset;

        // Smoothly interpolate to target
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
    }
}