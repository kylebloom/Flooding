using UnityEngine;

public sealed class RockingContainer : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Maximum rotation in degrees to either side.")]
    [Range(0f, 90f)]
    private float amplitude = 25f;

    [SerializeField]
    [Tooltip("Seconds required for one complete rocking cycle.")]
    [Min(0.1f)]
    private float period = 4f;

    private Quaternion startingRotation;

    private void Awake()
    {
        startingRotation = transform.localRotation;
    }

    private void Update()
    {
        float phase = Time.time * Mathf.PI * 2f / period;
        float zAngle = Mathf.Sin(phase) * amplitude;

        transform.localRotation =
            startingRotation * Quaternion.AngleAxis(zAngle, Vector3.forward);
    }
}