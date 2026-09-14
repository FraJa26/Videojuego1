using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Vector2 offset = new Vector2(0f, 1f);
    [SerializeField] private float minX = float.NegativeInfinity;
    [SerializeField] private float maxX = float.PositiveInfinity;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null) return;

        float x = Mathf.Clamp(target.position.x + offset.x, minX, maxX);
        Vector3 desired = new Vector3(x, target.position.y + offset.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetBounds(float newMinX, float newMaxX)
    {
        minX = newMinX;
        maxX = newMaxX;
    }
}
