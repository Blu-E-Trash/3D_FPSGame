using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField]
    private Transform _target;

    private void Awake()
    {
        if (_target == null)
        {
            Debug.LogError("CameraFollow: Target is not assigned.");
        }
    }

    private void LateUpdate()
    {
        if (_target != null)
        {
            transform.position = _target.position;
        }
    }
}
