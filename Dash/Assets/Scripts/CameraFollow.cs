using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private GameObject _target;

    void FixedUpdate()
    {
        transform.position = new Vector3(_target.transform.position.x, _target.transform.position.y, transform.position.z);
    }
}
