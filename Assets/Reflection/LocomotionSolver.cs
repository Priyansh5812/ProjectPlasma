using UnityEngine;
using Pkay.Input;
using Pkay.Utils;
using UnityEngine.InputSystem;
using KinematicCharacterController;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using static Unity.VisualScripting.AnnotationUtility;
using UnityEngine.UIElements;
[RequireComponent(typeof(CapsuleCollider))]
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
    public Vector3 lastHorizontalVelocity;
    public float HfinalSpeed = 0f;
    public float HinitialSpeed = 0f;
    public float Haccelaration = 0f;
    public float Hdeaccelaration = 1f;
    public float HmaxSpeed = 0f;
    public bool is2D = false;
    public bool wasObstructed = false;
    [Header("Overlap Resolution")]
    [SerializeField] List<OverlapResolutionReport> currOverlapReports;
    [Header("Volume Sweep")]
    [SerializeField] List<CharacterSweepReport> currSweepReports;
    [Header("Ground Probing")]
    [SerializeField] List<CharacterSweepReport> currGroundingReports;
    public float probingDistance;
    public float YOffset = 0.0f;
    public LayerMask groundMask;
    public bool isGrounded = false;
    public bool wasGrounded = false;
    private const float gravity = 9.8f;
    public float lastGravityInfluence = 0.0f;
    public float maxGravityInfluence = 2.5f;
    [SerializeField, Range(0.01f, 10f)] float gravityModifier = 1f;
    [Header("Vertical Velocity")]
    public bool IsjumpQueued = false;
    public float jumpImpulse;
    public float jumpImpulseRemaining;
    public Vector3 JumpDirection
    {
        get
        {
            return this.transform.TransformDirection(Vector3.up);
        }
    }
    
    public Vector3 GravityDirection
    {
        get
        {
            return this.transform.TransformDirection(Vector3.down);
        }
    }
    public float VfinalSpeed = 0.0f;
    public float VinitialSpeed = 0.0f;
    //public float Vdeaccelaration = 1f; 
    public float VmaxSpeed = 0f;
    // other refs
    Collider[] buffer = null;
    RaycastHit[] hits = null;
    Vector3 lastVerticalVel = Vector3.zero;



    private void Awake()
    {
        col ??= this.GetComponent<CapsuleCollider>();
        //rb ??= this.GetComponent<Rigidbody>();

        Application.targetFrameRate = -1;
        InitializeRigidbody();
        initialPosition = transientPosition = this.transform.position;
        initialRotation = transientRotation = this.transform.rotation;
    }

    private void InitializeRigidbody()
    {
/*        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.isKinematic = true;
        rb.detectCollisions = false;*/


    }


    public void SetInputActions(GameBindings inputActions)
    {
        this.inputActions = inputActions;
        this.inputActions.Player.Jump.started += TriggerJump;
    }

    #region Collider Abstractions
    private Vector3 Collider_GetTopHemisphereCenter() => this.transform.TransformPoint(Vector3.zero) + (Vector3.up * ((col.height / 2) - col.radius));
    private Vector3 Collider_GetBottomHemisphereCenter() => this.transform.TransformPoint(Vector3.zero) + (Vector3.down * ((col.height / 2) - col.radius));
    #endregion


    public void UpdatePhase_1(float deltaTime)
    {
        SolveOverlapResolution(transientPosition, transientRotation);
        HandleGroundingState();
    }




    private void SolveOverlapResolution(Vector3 atPosition, Quaternion atRotation)
    {
        // current Overlap Reports are updated
        GenOverlapResolutionReport(atPosition, atRotation);

        foreach (var i in currOverlapReports)
        {
            if (is2D)
            {
                Vector3 restrictionDir = i.overlapDirection;
                restrictionDir.z = 0.0f;
                transientPosition += restrictionDir * (i.correctionMagnitude + 0.1f);
            }
            else
            {
                transientPosition += i.overlapDirection * (i.correctionMagnitude + 0.1f);
            }
        }
    }

    private void HandleGroundingState()
    {
        Vector3 probeDirection = this.transform.TransformDirection(Vector3.down);
        GenCharacterSweepReports(probeDirection, probingDistance, groundMask, ref currGroundingReports, YOffset);
        isGrounded = currGroundingReports.Count > 0;
    }


    public void UpdatePhase_2(float deltaTime)
    {
        SolvePosition(deltaTime);
        wasGrounded = isGrounded;
    }

    private void SolvePosition(float deltaTime)
    {
        Vector3 HorizontalVel = SolveHorizontalVelocity(deltaTime); // Caution! ==> Velocity is not from Capsule Center
        Vector3 VerticalVel = SolveVerticalVelocity(deltaTime);

        Vector3 t_transientPosition = transientPosition;
        #region Horizontal Sweep Check
        GenCharacterSweepReports(HorizontalVel.normalized, HorizontalVel.magnitude, ~(groundMask), ref currSweepReports); // From horizontal Sweep remove the groundMask.
        if (currSweepReports.Count > 0)
        {
            if (!wasObstructed)
            {
                CharacterSweepReport closest = currSweepReports[0];
                foreach (var i in currSweepReports)
                {
                    closest = i.obs_Distance < closest.obs_Distance ? i : closest;
                }

                t_transientPosition += HorizontalVel.normalized * closest.obs_Distance;
               
            }

            wasObstructed = true;

        }
        else
        {
            t_transientPosition += HorizontalVel;
          
            wasObstructed = false;
        }
        Debug.DrawRay(t_transientPosition, HorizontalVel, Color.blue);

        #endregion
        #region Vertical Sweep Check
        #endregion


        t_transientPosition += VerticalVel;
        // ------------- Writing to the main Attribute(s) ---------------

        transientPosition = t_transientPosition;
    }

    private Vector3 SolveHorizontalVelocity(float deltaTime)
    {
       float horizontalInput = inputActions.Player.Move.ReadValue<float>();
       Vector3 resVel = Vector3.zero;
       Vector3 desiredDir = Vector3.right * horizontalInput;
        if (horizontalInput != 0)
        {
            if (Vector3.Dot(lastHorizontalVelocity, desiredDir) < 0f)
            {
                HfinalSpeed -= Hdeaccelaration * deltaTime;
                HfinalSpeed = Mathf.Clamp(HfinalSpeed, 0, HmaxSpeed);
                resVel = lastHorizontalVelocity.normalized * HfinalSpeed * deltaTime;
            }
            else
            {
                HfinalSpeed = HinitialSpeed + Haccelaration * deltaTime;
                HfinalSpeed = Mathf.Clamp(HfinalSpeed, 0f, HmaxSpeed);
                resVel = Vector3.right * HfinalSpeed * horizontalInput * deltaTime;
            }
        }
        else
        {
            HfinalSpeed -= Hdeaccelaration * deltaTime;
            HfinalSpeed = Mathf.Clamp(HfinalSpeed, 0, HmaxSpeed);
            resVel = lastHorizontalVelocity.normalized * HfinalSpeed * deltaTime;
        }
        lastHorizontalVelocity = resVel;
        HinitialSpeed = HfinalSpeed;
        return resVel;
    }

    private Vector3 SolveVerticalVelocity(float deltaTime)
    {
        Vector3 verticalVelocity = Vector3.zero;

        if (IsjumpQueued)
        {
            verticalVelocity += JumpDirection * GetJumpImpulse(deltaTime);
            IsjumpQueued = false;
        }

        if (!isGrounded)
        {
            Vector3 currentGravityInfluence = GravityDirection * (gravity * gravityModifier + lastGravityInfluence) * deltaTime;
            verticalVelocity += currentGravityInfluence;
            lastGravityInfluence = currentGravityInfluence.magnitude;

            if (jumpImpulseRemaining > 0.0f)
            {
                verticalVelocity += JumpDirection * GetJumpImpulse(deltaTime);
            }
        }
        else
        {
            lastGravityInfluence = 0.0f;
        }


        return verticalVelocity;
    }

    private void TriggerJump(InputAction.CallbackContext ctx)
    {   
        if(!isGrounded)
            return;

        IsjumpQueued = true;
        SetJumpBackup();
    }

    private void SetJumpBackup()
    {
        jumpImpulseRemaining = jumpImpulse;
    }

    private float GetJumpImpulse(float deltaTime)
    { 
       if(jumpImpulseRemaining <= 0.0f)
            return 0.0f;

        float resJumpImpulse = jumpImpulseRemaining * deltaTime;

        jumpImpulseRemaining -= resJumpImpulse;

        jumpImpulseRemaining = Mathf.Clamp(jumpImpulseRemaining, 0.0f, jumpImpulse);

        return resJumpImpulse;
    }



    private List<OverlapResolutionReport> GenOverlapResolutionReport(Vector3 atPosition, Quaternion atRotation)
    {
        currOverlapReports ??= new List<OverlapResolutionReport>();
        currOverlapReports.Clear();
        Collider[] overlapColliders = InternalOverlapCharacterVolume();
        if (overlapColliders == null)
            return currOverlapReports;
        for (int i = 0; i < overlapColliders.Length; i++)
        {
            Vector3 direction = Vector3.zero;
            float correctionMag = 0.0f;

            if (overlapColliders[i] == null || overlapColliders[i] == col)
                continue;

            if (Physics.ComputePenetration(
                col,
                atPosition,
                atRotation,
                overlapColliders[i],
                overlapColliders[i].transform.position,
                overlapColliders[i].transform.rotation,
                out direction,
                out correctionMag
                ))
            {
                // If correction Magnitude is not a '0.0' then consider that as a successful overlap
                if (correctionMag != 0f)
                {
                    OverlapResolutionReport report = new(overlapColliders[i], direction, correctionMag);
                    currOverlapReports.Add(report);
                }
            }
        }
        return currOverlapReports;
    }

    private List<CharacterSweepReport> GenCharacterSweepReports(Vector3 direction, float maxDistance, LayerMask sweepMask, ref List<CharacterSweepReport> report, float YOffset = 0.0f)
    {
        report ??= new List<CharacterSweepReport>();
        report.Clear();

        if (direction == Vector3.zero) // Invalid Direction to cast into...
            return report;

        RaycastHit[] hits = InternalCastCharacterVolume(direction, maxDistance, YOffset, sweepMask); // Get all the Colliders
        if (hits == null)
            return report;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider != null && hits[i].collider != col) // Not a valid collider
            {
                

                if (hits[i].point == Vector3.zero) // Invalid contact. Redo scan...
                    continue;

                CharacterSweepReport tempObj = new CharacterSweepReport();
                tempObj.otherCollider = hits[i].collider;
                tempObj.otherNormal = hits[i].normal;
                tempObj.contactPoint = hits[i].point;
                tempObj.obs_Distance = hits[i].distance;
                report.Add(tempObj);
            }
        }

        return report;
    }

    #region Internal
    private Collider[] InternalOverlapCharacterVolume()
    {
        buffer ??= new Collider[10];

        for (int i = 0; i < 10; i++)
        {
            buffer[i] = null;
        }

        Physics.OverlapCapsuleNonAlloc(Collider_GetTopHemisphereCenter(), Collider_GetBottomHemisphereCenter(), col.radius, buffer);
        return buffer;
    }

    private RaycastHit[] InternalCastCharacterVolume(Vector3 direction, float maxDistance, float YOffset, LayerMask sweepmask)
    {
        hits ??= new RaycastHit[10];

        for (int i = 0; i < 10; i++)
        {
            hits[i] = default;
        }

        Vector3 TopHemispherePoint = Collider_GetTopHemisphereCenter() + Vector3.up * YOffset;
        Vector3 BottomHemispherePoint = Collider_GetBottomHemisphereCenter() + Vector3.up * YOffset;

        int totalHits = Physics.CapsuleCastNonAlloc(TopHemispherePoint, BottomHemispherePoint, col.radius, direction, hits, maxDistance, sweepmask);
        
        return hits;
    }

    

    #endregion


    
    private void OnDisable()
    {
        this.inputActions.Player.Jump.started -= TriggerJump;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(Collider_GetBottomHemisphereCenter(), 0.25f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Collider_GetTopHemisphereCenter(), 0.25f);
            Gizmos.DrawRay(this.transform.position, this.transform.up * -1 * probingDistance);
        }
    }


}
