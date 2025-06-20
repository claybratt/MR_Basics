using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

public class FallRotationHandler : MonoBehaviour
{
    [Header("References")]
    public GravityProvider gravityProvider;
    public Transform playerVisuals; // visuals-only object

    [Header("Fall Settings")]
    public float fallTimeThreshold = 2f;

    [Header("Rotation During Fall")]
    public float backwardRotationSpeed = 30f;
    public float secondaryRotationSpeed = 20f;
    public Vector3 secondaryRotationAxis = new Vector3(0f, 1f, 1f);

    [Header("Reset Settings")]
    public float resetSpeed = 5f; // How fast to reset to default upright

    private float fallTimer = 0f;
    private bool isRotating = false;
    private Quaternion defaultRotation;

    void Start()
    {
        if (playerVisuals != null)
            defaultRotation = playerVisuals.localRotation;
    }

    void Update()
    {
        if (gravityProvider == null || playerVisuals == null)
            return;

        // Check falling state
        bool falling = !gravityProvider.isGrounded && gravityProvider.useGravity;

        if (falling)
        {
            fallTimer += Time.deltaTime;
            if (fallTimer >= fallTimeThreshold)
                isRotating = true;
        }
        else
        {
            fallTimer = 0f;

            if (isRotating)
            {
                // Begin reset on landing
                playerVisuals.localRotation = Quaternion.Slerp(
                    playerVisuals.localRotation,
                    defaultRotation,
                    Time.deltaTime * resetSpeed
                );

                // Stop once close enough
                if (Quaternion.Angle(playerVisuals.localRotation, defaultRotation) < 0.1f)
                {
                    playerVisuals.localRotation = defaultRotation;
                    isRotating = false;
                }
            }
        }

        // Apply rotation if falling and tumbling
        if (isRotating && falling)
        {
            float deltaTime = Time.deltaTime;
            playerVisuals.Rotate(Vector3.right, backwardRotationSpeed * deltaTime, Space.World);
            Vector3 axis = secondaryRotationAxis.normalized;
            playerVisuals.Rotate(axis, secondaryRotationSpeed * deltaTime, Space.World);
        }
    }
}
