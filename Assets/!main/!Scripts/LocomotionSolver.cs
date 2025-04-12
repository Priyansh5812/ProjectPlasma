using UnityEngine;
using Pkay.Input;
using Pkay.Utils;
using UnityEngine.InputSystem;
using KinematicCharacterController;
using System;
using UnityEngine.Pool;
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
    Collider[] buffer = null;
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
        SolveOverlapResolution(transientPosition , transientRotation , true);
    }



    private OverlapResolutionReport SolveOverlapResolution(Vector3 atPosition, Quaternion atRotation, bool overridePosition = false)
    {
        OverlapResolutionReport report = default(OverlapResolutionReport);
        Collider[] overlapColliders = InternalCastCharacterVolumeOverlap();
        report.colliders = overlapColliders;
        if (overlapColliders == null)
            return report;
        Vector3 finalDepenetrationVector = Vector3.zero;
        float finalMagnitude = 0.0f;
        for(int i = 0; i < overlapColliders.Length; i++)
        {   
            Vector3 depenetrationVector = Vector3.zero;
            float distance = 0.0f;

            if (overlapColliders[i] == null || overlapColliders[i] == col)
                continue;

            if (Physics.ComputePenetration(
                col,
                transientPosition,
                transientRotation,
                overlapColliders[i],
                overlapColliders[i].transform.position,
                overlapColliders[i].transform.rotation,
                out depenetrationVector,
                out distance
                ))
            {
                finalDepenetrationVector += depenetrationVector;
                finalMagnitude += distance;
            }
        }

        report.overlapDirection = finalDepenetrationVector.normalized;
        report.correctionMagnitude = finalMagnitude;
        if (overridePosition)
        {
            transientPosition += report.overlapDirection * report.correctionMagnitude;
        }
        return report;
    }


    public void UpdatePhase_2(float deltaTime)
    {
        SolvePosition(deltaTime);
    }







    private void SolvePosition(float deltaTime)
    {
        Vector3 HorizontalVel = SolveHorizontalVelocity(deltaTime); // Caution! ==> Velocity is not from Capsule Center
        
        #region Horizontal Velocity Overlap Check
        ///Checking for if applied the velocity horizontally, will there be an overlap ??
        ///If there will be then refrain from applying to that position
        ///Otherwise simply apply the velocity;
        Vector3 temp_transientPosition = transientPosition + HorizontalVel;
        OverlapResolutionReport report = SolveOverlapResolution(temp_transientPosition, transientRotation);
        Utils.Print($"{Vector3.Dot(HorizontalVel.normalized, report.overlapDirection.normalized)}", Color.white, PrintStream.LOG, true);
        if (Vector3.Dot(HorizontalVel.normalized, report.overlapDirection.normalized) >= 0)
        {
            transientPosition += HorizontalVel;
        }
        #endregion
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


    #region Internal
    private Collider[] InternalCastCharacterVolumeOverlap()
    {
        buffer??= new Collider[10];
        Physics.OverlapCapsuleNonAlloc(Collider_GetTopHemisphereCenter(),Collider_GetBottomHemisphereCenter(),col.radius,buffer);
        return buffer;

    }
    #endregion




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
