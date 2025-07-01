using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRDoor : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float maxSwingAngle = 90f;      // Degrees in both directions
    public float closeThreshold = 5f;      // Degrees within this angle will trigger auto-close
    public float closeSpeed = 180f;        // Degrees per second
    public bool isLocked = false;

    [Header("Handle")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable doorHandle;

    private Quaternion startRotation;
    private Transform controllerTransform = null;
    private bool isGrabbed = false;

    void Start()
    {
        startRotation = transform.localRotation;
        doorHandle.selectEntered.AddListener(OnGrab);
        doorHandle.selectExited.AddListener(OnRelease);
    }

    void OnDestroy()
    {
        doorHandle.selectEntered.RemoveListener(OnGrab);
        doorHandle.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (!isLocked)
            controllerTransform = args.interactorObject.transform;
        isGrabbed = true;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
        controllerTransform = null;
    }

    void Update()
    {
        if (isGrabbed && controllerTransform && !isLocked)
        {
            // Direction to controller
            Vector3 localControllerDir = transform.parent.InverseTransformPoint(controllerTransform.position);
            float angle = Mathf.Atan2(localControllerDir.x, localControllerDir.z) * Mathf.Rad2Deg;

            // Clamp to swing limits
            float clamped = Mathf.Clamp(angle, -maxSwingAngle, maxSwingAngle);
            transform.localRotation = Quaternion.Euler(0, clamped, 0);
        }
        else
        {
            // Auto-close
            float currentY = transform.localEulerAngles.y;
            currentY = (currentY > 180) ? currentY - 360 : currentY;

            if (Mathf.Abs(currentY) < closeThreshold)
            {
                transform.localRotation = Quaternion.RotateTowards(
                    transform.localRotation,
                    startRotation,
                    closeSpeed * Time.deltaTime
                );

                if (Quaternion.Angle(transform.localRotation, startRotation) < 0.5f)
                {
                    transform.localRotation = startRotation;
                    isLocked = true;
                }
            }
        }
    }

    public void UnlockDoor()
    {
        isLocked = false;
    }

    public void LockDoor()
    {
        isLocked = true;
    }
}
