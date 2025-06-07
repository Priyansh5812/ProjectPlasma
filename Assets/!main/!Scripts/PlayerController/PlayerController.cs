using KinematicCharacterController;
using Pkay.Input;
using Pkay.Utils;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using static UnityEngine.LightAnchor;
using UnityEngine.InputSystem;


[RequireComponent(typeof(KinematicCharacterMotor))]
public class PlayerController : MonoBehaviour, ICharacterController
{
    [SerializeField] KinematicCharacterMotor motor;
    GameBindings inputActions;
    [Header("Horizontal Movement")]
    [SerializeField] float horizontalInput;
    private Vector3 lastHorizontalVelocity;
    private float HfinalSpeed;
    private float HinitialSpeed;
    public float Haccelaration;
    public float Hdeaccelaration;
    public float HmaxSpeed;

    [Header("Vertical Movement")]
    public float jumpImpulse;
    private bool IsJumpQueued = false; 
    public Vector3 JumpDirection
    {
        get => this.transform.TransformDirection(Vector3.up);
    }
    public Vector3 GravityDirection
    {
        get => this.transform.TransformDirection(Vector3.down);
    }

    const float gravity = 9.8f;
    public bool isGrounded;
    public bool hasStableGround;
    public float gravityModifier;
    private float lastGravityInfluence;
    private float jumpImpulseRemaining;
    private void OnEnable()
    {
        InitializeInputs();
    }

    private void Start()
    {
        motor ??= this.GetComponent<KinematicCharacterMotor>();
        motor.CharacterController = this;
    }
    private void InitializeInputs()
    { 
        inputActions ??= new GameBindings();
        inputActions.Player.Jump.performed += TriggerJump;
        inputActions.Enable();
    }

    private void Update()
    {
        horizontalInput = inputActions.Player.Move.ReadValue<float>();
        isGrounded = motor.GroundingStatus.FoundAnyGround;
        hasStableGround = motor.GroundingStatus.IsStableOnGround;
    }


    public void AfterCharacterUpdate(float deltaTime)
    {
        
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
        
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
        
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        //currentVelocity = SolveHorizontalVelocity(deltaTime) + SolveVerticalVelocity(deltaTime);
        Vector3 horizontalVelocity = SolveHorizontalVelocity(deltaTime);
        Vector3 verticalVelocity = SolveVerticalVelocity(deltaTime);

        if (isGrounded)
        {
            horizontalVelocity = Vector3.ProjectOnPlane(horizontalVelocity, motor.GroundingStatus.OuterGroundNormal);
        }

        currentVelocity = horizontalVelocity + verticalVelocity;

    }


    private Vector3 SolveHorizontalVelocity(float deltaTime)
    {
        
        Vector3 resVel = Vector3.zero;
        Vector3 desiredDir = this.transform.forward * horizontalInput; // What velocity are we trying to achive
        if (horizontalInput != 0)
        {   
            // If we are under the influence of the last applied velocity which is opposite to the desired velocity... then accelarate
            if (Vector3.Dot(lastHorizontalVelocity, desiredDir) < 0f)
            {
                HfinalSpeed -= Hdeaccelaration * deltaTime;
                HfinalSpeed = Mathf.Clamp(HfinalSpeed, 0, HmaxSpeed);
                resVel = lastHorizontalVelocity.normalized * HfinalSpeed * deltaTime;
            }
            // If last applied velocity and desired velocity have same direction... then accelarate
            else
            {
                HfinalSpeed = HinitialSpeed + Haccelaration * deltaTime;
                HfinalSpeed = Mathf.Clamp(HfinalSpeed, 0f, HmaxSpeed);
                resVel = this.transform.forward * HfinalSpeed * horizontalInput * deltaTime;
            }
        }
        else
        {   // If not input is there to apply then deaccelarate
            HfinalSpeed -= Hdeaccelaration * deltaTime;
            HfinalSpeed = Mathf.Clamp(HfinalSpeed, 0, HmaxSpeed);
            resVel = lastHorizontalVelocity.normalized * HfinalSpeed * deltaTime;
        }
        //store the last applied velocity
        lastHorizontalVelocity = resVel;
        // storet the last speed applied
        HinitialSpeed = HfinalSpeed;
        return resVel;
    }

    private Vector3 SolveVerticalVelocity(float deltaTime)
    {
        Vector3 verticalVelocity = Vector3.zero;
        // If we have jump queeud
        if (IsJumpQueued)
        {   
            // APply the jump velocity by deltaTime
            verticalVelocity += JumpDirection * GetJumpImpulse(deltaTime);
            IsJumpQueued = false;
            return verticalVelocity;
        }

        // If player is not on the ground then...
        if (!isGrounded)
        {   
            // apply the influence of the gravity
            Vector3 currentGravityInfluence = GravityDirection * (gravity * gravityModifier + lastGravityInfluence) * deltaTime;
            verticalVelocity += currentGravityInfluence;
            lastGravityInfluence += currentGravityInfluence.magnitude;
            // Also apply the remaining jump Impulse in the account
            if (jumpImpulseRemaining > 0.0f)
            {
                verticalVelocity += JumpDirection * GetJumpImpulse(deltaTime);
            }
        }
        else // upon being grounded... reset the past gravity influences
        {
            lastGravityInfluence = 0.0f;
        }


        return verticalVelocity;
    }

    #region Jump Handling
    private void TriggerJump(InputAction.CallbackContext ctx)
    {
        if (!isGrounded)
            return;

        IsJumpQueued = true;
        SetJumpBackup();
    }

    private void SetJumpBackup()
    {
        jumpImpulseRemaining = jumpImpulse;
    }

    private float GetJumpImpulse(float deltaTime)
    {
        if (jumpImpulseRemaining <= 0.0f)
            return 0.0f;

        float resJumpImpulse = jumpImpulseRemaining * deltaTime;

        jumpImpulseRemaining -= resJumpImpulse;

        jumpImpulseRemaining = Mathf.Clamp(jumpImpulseRemaining, 0.0f, jumpImpulse);

        return resJumpImpulse;
    }

    #endregion


    private void OnDisable()
    {
        inputActions.Player.Jump.performed -= TriggerJump;
        inputActions?.Disable();
    }
}
