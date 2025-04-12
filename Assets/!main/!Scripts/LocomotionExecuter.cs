using Pkay.Input;
using Pkay.Utils;
using UnityEngine;


[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class LocomotionExecuter : MonoBehaviour
{
    private LocomotionSolver solver;
    private GameBindings inputActions;
    private bool isSolverInitialized = false;
    private float lastCustomInterpolationStartTime = 0f;
    private float lastCustomInterpolationdeltaTime = 0f;
    //------------- Inputs ------------------------
    private void Awake()
    {
        if (this.TryGetComponent<LocomotionSolver>(out solver))
        {
            isSolverInitialized = true;
            
        }
        else
            Utils.Print("Solver not Initialized", Color.red, PrintStream.ERROR, true);

        //---------------------------------------------------------------------------------------------------------


        InitializeInputSystem();
    }


    private void Update()
    {
        
    }


    private void InitializeInputSystem()
    {
        inputActions ??= new();
        inputActions.Enable();
        if(isSolverInitialized)
            solver.SetInputActions(inputActions);
    }

    private void DeInitializeInputSystem()
    {
        inputActions.Disable();
        inputActions.Dispose();
    }




    private void FixedUpdate()
    {
        if (!isSolverInitialized)
            return;

        PreSimulationUpdate(Time.fixedDeltaTime);
        Simulate(Time.fixedDeltaTime);
        PostSimulationUpdate(Time.fixedDeltaTime);
    }


    private void LateUpdate()
    {   
        InterpolationUpdate();
    }


    private void PreSimulationUpdate(float deltaTime)
    {
        solver.initialPosition = solver.transientPosition;
        solver.initialRotation = solver.transientRotation;

        //-------------------------------------------------------------

        solver.transform.SetPositionAndRotation(solver.transientPosition, solver.transientRotation);
    }


    private void Simulate(float deltaTime)
    {
        solver.UpdatePhase_1(deltaTime);
        solver.UpdatePhase_2(deltaTime);
        
    }


    private void PostSimulationUpdate(float deltaTime)
    {
        lastCustomInterpolationStartTime = Time.time;
        lastCustomInterpolationdeltaTime = deltaTime;        
    }


    private void InterpolationUpdate()
    {
        float interpolationIndex = Mathf.Clamp01((Time.time - lastCustomInterpolationStartTime) / lastCustomInterpolationdeltaTime);
        Vector3 interpolatedPosition = Vector3.Lerp(solver.initialPosition, solver.transientPosition, interpolationIndex); 
        Quaternion interpolatedRotation = Quaternion.Lerp(solver.initialRotation, solver.transientRotation, interpolationIndex);


        
        solver.transform.SetPositionAndRotation(interpolatedPosition, interpolatedRotation);
    }




    private void OnDisable()
    {
        DeInitializeInputSystem();
    }


}
