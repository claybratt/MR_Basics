using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Attach to a prefab to make it a climbable handhold with haptics, grip timeout, cooldown, and color feedback.
/// Each instance gets its own material so colors can vary independently.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshRenderer))]
public class SimpleClimbHandhold : ClimbInteractable
{
    [Header("Grip Settings")]
    public float gripTimeout = 3f;
    public float cooldownDuration = 2f;

    [Header("Haptics")]
    [Range(0f, 1f)] public float hapticAmplitudeStart = 0.2f;
    [Range(0f, 1f)] public float hapticAmplitudeEnd = 0.8f;
    [Range(0f, 1f)] public float hapticFrequencyStart = 0.05f;
    [Range(0f, 1f)] public float hapticFrequencyEnd = 0.1f;


    [Header("Color States")]
    public Color idleColor = Color.gray;
    public Color hoverColor = Color.yellow;
    public Color gripStartColor = Color.green;
    public Color gripEndColor = Color.red;
    public Color cooldownColor = Color.black;

    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    private Material instanceMat;
    private Coroutine gripTimeoutRoutine;
    private bool isCoolingDown = false;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        meshRenderer = GetComponent<MeshRenderer>();

        // Clone the shared material to create a unique instance for this GameObject
        instanceMat = Instantiate(meshRenderer.sharedMaterial);
        meshRenderer.material = instanceMat;

        SetColor(idleColor);

        if (climbProvider == null)
        {
            climbProvider = FindFirstObjectByType<ClimbProvider>();
#if UNITY_EDITOR
            Debug.Log(climbProvider != null
                ? $"ClimbProvider found and assigned to {name}"
                : $"No ClimbProvider found in scene for {name}");
#endif
        }
    }

    protected override void OnValidate()
    {
        SetColor(idleColor);
        base.OnValidate();
        if (climbTransform == null)
            climbTransform = transform;
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        return base.IsSelectableBy(interactor) && !isCoolingDown;
    }

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);
        if (!isCoolingDown) SetColor(hoverColor);
    }

    protected override void OnHoverExited(HoverExitEventArgs args)
    {
        base.OnHoverExited(args);
        if (!isCoolingDown) SetColor(idleColor);
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        if (gripTimeout > 0f)
        {
            if (gripTimeoutRoutine != null)
                StopCoroutine(gripTimeoutRoutine);

            gripTimeoutRoutine = StartCoroutine(GripTimeoutRoutine(args.interactorObject));
        }
        else
        {
            SetColor(gripStartColor);
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        if (gripTimeoutRoutine != null)
        {
            StopCoroutine(gripTimeoutRoutine);
            gripTimeoutRoutine = null;
        }

        rb.useGravity = true;
        StopHaptics(args.interactorObject);
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator GripTimeoutRoutine(IXRSelectInteractor interactor)
    {
        float elapsed = 0f;

        while (elapsed < gripTimeout)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / gripTimeout);

            // Color Lerp
            SetColor(Color.Lerp(gripStartColor, gripEndColor, t));

            // Haptic Lerp
            float amplitude = Mathf.Lerp(hapticAmplitudeStart, hapticAmplitudeEnd, t);
            float frequency = Mathf.Lerp(hapticFrequencyStart, hapticFrequencyEnd, t);
            SendHaptics(interactor, amplitude, frequency);

            yield return null;
        }

        interactionManager.SelectExit(interactor, this);
        StopHaptics(interactor);
    }


    private IEnumerator CooldownRoutine()
    {
        isCoolingDown = true;
        SetColor(cooldownColor);

        yield return new WaitForSeconds(cooldownDuration);

        isCoolingDown = false;
        SetColor(idleColor);
    }

    /// <summary>
    /// Sets the color on this handhold's unique material.
    /// </summary>
    private void SetColor(Color color)
    {
        if (instanceMat != null)
        {
            if (instanceMat.HasProperty("_Color"))
                instanceMat.color = color;
            else if (instanceMat.HasProperty("_BaseColor"))
                instanceMat.SetColor("_BaseColor", color);
        }
    }

    private void SendHaptics(IXRInteractor interactor, float amplitude, float duration)
    {
        if (interactor is XRBaseInputInteractor controllerInteractor)
            controllerInteractor.SendHapticImpulse(amplitude, duration);
    }

    private void StopHaptics(IXRInteractor interactor)
    {
        SendHaptics(interactor, 0f, 0.01f);
    }
}
