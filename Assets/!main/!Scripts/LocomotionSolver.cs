using UnityEngine;
using Pkay.Input;
using Pkay.Utils;
using UnityEngine.InputSystem;
using KinematicCharacterController;
[RequireComponent(typeof(Rigidbody) , typeof(CapsuleCollider))]
public class LocomotionSolver : MonoBehaviour
{

    public Rigidbody rb { get; private set; }
    public CapsuleCollider col { get; private set; }
    private GameBindings inputActions;
    [Header("Horizontal Movement")]
    public Vector3 initialPosition;
    public Vector3 transientPosition;
    public Quaternion initialRotation;
    public Quaternion transientRotation;
    public Vector3 moveDirection;
    public Vector3 lastmoveDirection;
    public float finalSpeed = 0f;
    public float initialSpeed = 0f;
    public float accelaration = 0f;
    public float deaccelaration = 1f;
    public float maxSpeed = 0f;
    [Header("GroundProbing")]
    [SerializeField] CharacterGroundingReport currGroundingReport;
    CharacterGroundingReport lastGroundingReport;
    public float probingDistance;
    public LayerMask groundMask;
    private void Awake()
    {
        col ??= this.GetComponent<CapsuleCollider>();
        rb ??= this.GetComponent<Rigidbody>();

        Application.targetFrameRate = -1;
        InitializeRigidbody();
        initialPosition = transientPosition = this.transform.position;
        initialRotation = transientRotation = this.transform.rotation;
    }

    private void InitializeRigidbody()
    {
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.isKinematic = true;
    }


    public void SetInputActions(GameBindings inputActions) 
    {
        this.inputActions = inputActions;
        

    }

    #region Collider Abstractions
    private Vector3 Collider_GetTopHemisphereCenter() => this.transform.TransformPoint(Vector3.zero) + (Vector3.up * ((col.height / 2)-col.radius));
    private Vector3 Collider_GetBottomHemisphereCenter() => this.transform.TransformPoint(Vector3.zero) + (Vector3.down * ((col.height / 2)-col.radius));
    #endregion


    public void UpdatePhase_1(float deltaTime)
    {
        GenerateGroundingReport(deltaTime);
    }

    public void UpdatePhase_2(float deltaTime)
    {
        SolvePosition(deltaTime);
    }





    private void SolvePosition(float deltaTime)
    {
        Vector3 HorizontalVel = SolveHorizontalVelocity(deltaTime); // Caution! ==> Velocity is not from Capsule Center
        transientPosition += HorizontalVel;
    }

    private Vector3 SolveHorizontalVelocity(float deltaTime)
    {
        float horizontalInput = inputActions.Player.Move.ReadValue<float>();
        Vector3 resVel;
        moveDirection = this.transform.TransformDirection(Vector3.right * horizontalInput);

        if (moveDirection != Vector3.zero)
        {
            finalSpeed = initialSpeed + accelaration * deltaTime;
            finalSpeed = Mathf.Clamp(finalSpeed, 0f, maxSpeed);
            //transientPosition += moveDirection * finalSpeed * deltaTime; //OLD CODE
            resVel = moveDirection * finalSpeed * deltaTime;
            lastmoveDirection = moveDirection;
        }
        else
        {
            finalSpeed -= deaccelaration * deltaTime;
            finalSpeed = Mathf.Max(finalSpeed, 0, finalSpeed);
            //transientPosition += lastmoveDirection * finalSpeed * deltaTime; // OLD CODE
            resVel = lastmoveDirection * finalSpeed * deltaTime;
        }

        initialSpeed = finalSpeed;
        return resVel;
    }

    private void SolveVerticalVelocity()
    { 
        
    }


    private void GenerateGroundingReport(float deltaTime)
    {
        InternalGroundSweep(ref this.currGroundingReport);
        lastGroundingReport = this.currGroundingReport;
    }


    private void InternalGroundSweep(ref CharacterGroundingReport report)
    {
        Vector3 probingPosition = transientPosition;
        Quaternion atRotation = transientRotation;
        Vector3 sweepDirection = this.transform.TransformDirection(Vector3.down);
        float probingDistance = this.probingDistance;

        RaycastHit hit = default;
        Vector3 probingCenterOffset = Vector3.up * 0.1f;
        // ------------------- CASTING CHARACTER'S VOLUME ------------------
        report.FoundAnyGround = Physics.CapsuleCast(Collider_GetTopHemisphereCenter(), Collider_GetBottomHemisphereCenter()+ probingCenterOffset, col.radius, sweepDirection, out hit ,probingDistance, groundMask);
        report.IsStableOnGround = true;
        report.SnappingPrevented = true;
        report.GroundNormal = hit.normal;
        report.InnerGroundNormal = hit.normal;
        report.OuterGroundNormal = Vector3.zero;
        report.GroundCollider = hit.collider;
        report.GroundPoint = hit.point; 
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {   Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(Collider_GetBottomHemisphereCenter(), 0.25f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Collider_GetTopHemisphereCenter(), 0.25f);
            Gizmos.DrawRay(this.transform.position,this.transform.up * -1 * probingDistance);
        }
    }


}
