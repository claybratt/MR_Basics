using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class RopeGenerator : MonoBehaviour
{
    public Transform player;
    public float maxDistance = 5f;
    public InputActionReference secondaryButtonAction;

    private LineRenderer lineRenderer;
    private static RopeGenerator currentRope;

    private bool ropeActive = false;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 2;

        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
                player = foundPlayer.transform;
        }
    }

    private void OnEnable()
    {
        if (secondaryButtonAction != null)
            secondaryButtonAction.action.performed += OnSecondaryPressed;
    }

    private void OnDisable()
    {
        if (secondaryButtonAction != null)
            secondaryButtonAction.action.performed -= OnSecondaryPressed;
    }

    private void OnSecondaryPressed(InputAction.CallbackContext ctx)
    {
        if (currentRope == this)
            DisableRope();
    }

    private void LateUpdate()
    {
        if (ropeActive && player != null)
        {
            if (Vector3.Distance(transform.position, player.position) > maxDistance)
            {
                Vector3 direction = (player.position - transform.position).normalized;
                player.position = transform.position + direction * maxDistance;
            }

            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, player.position + Vector3.up * 0.5f); // Adjust height for rope end
        }
    }

    public void GenerateRope()
    {
        if (player == null)
        {
            Debug.LogWarning("Player not found.");
            return;
        }

        if (currentRope != null && currentRope != this)
        {
            currentRope.DisableRope();
        }

        currentRope = this;
        ropeActive = true;
        lineRenderer.enabled = true;
    }

    public void DisableRope()
    {
        ropeActive = false;
        lineRenderer.enabled = false;

        if (currentRope == this)
            currentRope = null;
    }
}
