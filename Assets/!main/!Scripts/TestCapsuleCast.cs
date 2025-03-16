using UnityEngine;

public class TestCapsuleCast : MonoBehaviour
{
    public CapsuleCollider collider;

    public float upperOffset, lowerOffset;
    public Vector3 upperPoint, lowerPoint;
    public GameObject hitObject;
    public LayerMask LayerMask;
    RaycastHit hit;
    public float radius;
    public bool isHit = false;
    public float distance;
    private void Update()
    {
         upperPoint = this.transform.TransformPoint(collider.center) + Vector3.up * collider.height /2 * upperOffset;
         lowerPoint = this.transform.TransformPoint(collider.center) + Vector3.down * collider.height /2 * lowerOffset;


        isHit = Physics.CapsuleCast(upperPoint, lowerPoint, radius, this.transform.up * -1, out hit, distance, LayerMask);
        //isHit = Physics.SphereCast(this.transform.TransformPoint(Vector3.zero), radius, Vector3.down,out hit, distance, LayerMask);
        

        if(isHit)
           hitObject = hit.transform.gameObject;

    }

    public void OnDrawGizmos()
    {   
        if(Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(upperPoint, 0.125f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lowerPoint, 0.125f);



            if (isHit)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(hit.point, hit.normal * hit.distance);
                
            }
        }
    }
}
