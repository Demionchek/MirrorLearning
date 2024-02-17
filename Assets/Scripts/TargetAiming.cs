using System;
using UnityEngine;

public class TargetAiming : MonoBehaviour {

    public float targetDistance = 100f;
    
    [SerializeField] private Transform target;
    private Camera mainCamera;

    private void Start() {
        mainCamera = Camera.main;
    }

    private void Update() {
        if (mainCamera != null && target != null) {
            Debug.DrawRay(mainCamera.transform.position,mainCamera.transform.forward * targetDistance, Color.red);
            target.position = mainCamera.transform.forward * targetDistance;
        }
    }
}
