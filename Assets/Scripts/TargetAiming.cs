using System;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class TargetAiming : MonoBehaviour
{

    public float targetDistance = 100f;

    [SerializeField]
    private Transform target;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera != null && target != null)
        {
            Debug.DrawRay(mainCamera.transform.position, mainCamera.transform.forward * targetDistance, Color.red);
            target.position =
                new Vector3(target.position.x, target.position.y, mainCamera.transform.forward.z * targetDistance);
        }
    }

    private void FixedUpdate()
    {
       if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out RaycastHit hitInfo, 100f)) {
            targetDistance = hitInfo.distance;
       }
    }
}
