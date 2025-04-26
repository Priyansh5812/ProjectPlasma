using UnityEngine;
using Pkay.Input;
using Pkay.Utils;
using UnityEngine.InputSystem;
using KinematicCharacterController;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    public bool is2D = false;
    public bool wasObstructed = false;
    [Header("Overlap Resolution")]
    [SerializeField] List<OverlapResolutionReport> currOverlapReports;
    [Header("Volume Sweep")]
    [SerializeField] List<CharacterSweepReport> currSweepReports;

    public float probingDistance;
    public LayerMask groundMask;
    Collider[] buffer = null;
    RaycastHit[] hits = null;

    

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
        rb.detectCollisions = false;


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
        SolveOverlapResolution(transientPosition , transientRotation);
    }




    private void SolveOverlapResolution(Vector3 atPosition, Quaternion atRotation)
    {   
        // current Overlap Reports are updated
        GenOverlapResolutionReport(atPosition, atRotation);

        foreach (var i in currOverlapReports)
        {   if (is2D)
            {
                Vector3 restrictionDir = i.overlapDirection;
                restrictionDir.z = 0.0f;
                transientPosition += restrictionDir * i.correctionMagnitude;
            }
            else 
            {
              transientPosition += i.overlapDirection * i.correctionMagnitude;
            }
        }
    }


    public void UpdatePhase_2(float deltaTime)
    {
        SolvePosition(deltaTime);
    }

    private void SolvePosition(float deltaTime)
    {
        Vector3 HorizontalVel = SolveHorizontalVelocity(deltaTime); // Caution! ==> Velocity is not from Capsule Center
        GenCharacterSweepReports(HorizontalVel.normalized, HorizontalVel.magnitude , ~(groundMask)); // From horizontal Sweep remove the groundMask.
        
        if (currSweepReports.Count > 0)
        {

            if (!wasObstructed)
            {
                CharacterSweepReport closest = currSweepReports[0];
                foreach (var i in currSweepReports)
                {
                    closest = i.obs_Distance < closest.obs_Distance ? i : closest;
                }

                transientPosition += HorizontalVel.normalized * closest.obs_Distance;
            }

            wasObstructed = true;
                   
        }
        else
        {
            transientPosition += HorizontalVel;
            wasObstructed = false;
        }

        Debug.DrawRay(transientPosition, HorizontalVel,Color.blue);
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

    private List<CharacterSweepReport> GenCharacterSweepReports(Vector3 direction , float maxDistance , LayerMask sweepMask)
    {
        currSweepReports ??= new List<CharacterSweepReport>();
        currSweepReports.Clear();

        if(direction == Vector3.zero) // Invalid Direction to cast into...
            return currSweepReports;
        
        RaycastHit[] hits = InternalCastCharacterVolume(direction, maxDistance , sweepMask); // Get all the Colliders
        if(hits == null)
            return currSweepReports;
        for (int i = 0; i < hits.Length; i++)
        {   
            if (hits[i].collider != null && hits[i].collider != col) // Not a valid collider
            {

                if (hits[i].point == Vector3.zero) // Invalid contact. Redo scan...
                    continue;

                CharacterSweepReport report = new CharacterSweepReport();
                report.otherCollider = hits[i].collider;
                report.otherNormal = hits[i].normal;
                report.contactPoint = hits[i].point;
                report.obs_Distance = hits[i].distance;
                currSweepReports.Add(report);
            }
        }

        return currSweepReports;
    }

    #region Internal
    private Collider[] InternalOverlapCharacterVolume()
    {
        buffer??= new Collider[10];

        for (int i = 0; i < 10; i++)
        {
            buffer[i] = null;
        }

        Physics.OverlapCapsuleNonAlloc(Collider_GetTopHemisphereCenter(),Collider_GetBottomHemisphereCenter(),col.radius,buffer);
        return buffer;
    }

    private RaycastHit[] InternalCastCharacterVolume(Vector3 direction, float maxDistance,LayerMask sweepmask)
    {   
        hits ??= new RaycastHit[10];

        for (int i = 0; i < 10; i++)
        {
            hits[i] = default;
        }

        int totalHits = Physics.CapsuleCastNonAlloc(Collider_GetTopHemisphereCenter(), Collider_GetBottomHemisphereCenter(), col.radius, direction, hits, maxDistance, sweepmask);
        return hits;
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
