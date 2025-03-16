using UnityEngine;
using Pkay.Input;
using Pkay.Utils;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody) , typeof(CapsuleCollider))]
public class LocomotionSolver : MonoBehaviour
{

    private Rigidbody rb;
    private CapsuleCollider col;
    private GameBindings inputAction;

    public float moveValue;


    private void Awake()
    {
        col ??= this.GetComponent<CapsuleCollider>();
        rb ??= this.GetComponent<Rigidbody>();
        inputAction ??= new();
        inputAction?.Enable();
        inputAction.Player.Jump.performed += OnJump;
    }

    private void Update()
    {
        moveValue = inputAction.Player.Move.ReadValue<float>();
        
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        Utils.Print("Jumped" , Color.cyan, PrintStream.LOG, true);
    }


    private void OnDisable()
    {
        inputAction?.Disable();
    }



}
