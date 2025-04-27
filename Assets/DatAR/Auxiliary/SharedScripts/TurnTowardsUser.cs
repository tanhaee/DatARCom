using UnityEngine;
using UniRx;

public class TurnTowardsUser : MonoBehaviour
{
    // Set object that the point should face. Regularly, the camera / user.
    public Transform pointTargetToFace;

    // Angular speed in degrees per sec. for turning to ObjectToFace.
    [SerializeField] private float pointRotateSpeed = 2f;

    public bool lockX;
    public bool lockY;
    
    void Awake()
    {
        pointTargetToFace = Camera.main.transform;
    }
    
    private void LateUpdate()
    {
        if (!pointTargetToFace)
        {
            return;
        }

        var lookDir = transform.position - pointTargetToFace.position;
        if (lockX)
            lookDir.x = 0;
        if (lockY)
            lookDir.y = 0;
        var targetRotation = Quaternion.LookRotation(lookDir);
        var str = Mathf.Min (pointRotateSpeed * Time.deltaTime, 5);
        transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, str);
    }

}
