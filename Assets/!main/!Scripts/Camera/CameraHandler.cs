using UnityEngine;

public class CameraHandler : MonoBehaviour
{
    [SerializeField] Transform Target;
    [SerializeField] float distance;
    [SerializeField] Vector3 localDirection;
    [SerializeField] float followSpeed;
    private void Start()
    {
        this.transform.LookAt(Target);
        this.transform.position = Target.position + Target.TransformDirection(localDirection) * distance;
    }

    private void Update()
    {   
        Vector3 calculatedPosition = Target.position + Target.TransformDirection(localDirection) * distance;
        this.transform.position = Vector3.Lerp(this.transform.position, calculatedPosition, Time.deltaTime * followSpeed);
        
    }
}
