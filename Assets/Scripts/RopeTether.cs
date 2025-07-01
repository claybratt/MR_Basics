using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeTether : MonoBehaviour
{
    [Header("Rope Settings")]
    public Transform startPoint;
    public Transform endPoint;
    public int segmentCount = 20;
    public float ropeLength = 5f;
    public float segmentMass = 0.2f;
    public float jointSpring = 100f;

    private LineRenderer lineRenderer;
    private Rigidbody[] ropeSegments;

    void Start()
    {
        BuildRope();
    }

    void Update()
    {
        if (lineRenderer && ropeSegments != null)
        {
            for (int i = 0; i < ropeSegments.Length; i++)
            {
                lineRenderer.SetPosition(i, ropeSegments[i].position);
            }

            // Keep first and last segments aligned with start and end points (if they move)
            if (startPoint != null)
            {
                ropeSegments[0].MovePosition(startPoint.position);
            }

            if (endPoint != null)
            {
                ropeSegments[segmentCount - 1].MovePosition(endPoint.position);
            }
        }
    }

    void BuildRope()
    {
        if (!startPoint || !endPoint)
        {
            Debug.LogError("StartPoint and EndPoint must be assigned!");
            return;
        }

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = segmentCount;

        ropeSegments = new Rigidbody[segmentCount];

        float segmentLength = ropeLength / (segmentCount - 1);
        Vector3 direction = (endPoint.position - startPoint.position).normalized;

        Rigidbody previousRb = null;

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segment = new GameObject($"RopeSegment_{i}");
            segment.transform.position = startPoint.position + direction * segmentLength * i;

            Rigidbody rb = segment.AddComponent<Rigidbody>();
            rb.mass = segmentMass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            CapsuleCollider col = segment.AddComponent<CapsuleCollider>();
            col.radius = 0.05f;
            col.height = segmentLength;
            col.direction = 2; // Z axis

            // Lock first and last segment to start/end transform via kinematic or motion update
            if (i == 0)
            {
                rb.isKinematic = true; // We'll manually move it in Update
            }
            else if (i == segmentCount - 1)
            {
                rb.isKinematic = true; // Also moved in Update
            }

            if (previousRb != null)
            {
                ConfigurableJoint joint = segment.AddComponent<ConfigurableJoint>();
                joint.connectedBody = previousRb;
                joint.axis = Vector3.forward;
                joint.xMotion = ConfigurableJointMotion.Locked;
                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Limited;

                SoftJointLimit limit = new SoftJointLimit { limit = segmentLength };
                joint.linearLimit = limit;

                JointDrive drive = new JointDrive
                {
                    positionSpring = jointSpring,
                    positionDamper = 5f,
                    maximumForce = Mathf.Infinity
                };
                joint.zDrive = drive;
                joint.configuredInWorldSpace = false;
                joint.autoConfigureConnectedAnchor = true;
            }

            ropeSegments[i] = rb;
            previousRb = rb;
        }
    }
}
