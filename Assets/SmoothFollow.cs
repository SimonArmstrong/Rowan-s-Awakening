using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothFollow : MonoBehaviour {
    public float horizontalSpeed = 5;
    public float verticalSpeed = 5;
    public Transform target;

    private void OnValidate() {
        if(target != null)
            transform.position = target.position;
    }

    private void FixedUpdate()
    {
        float x = Mathf.Lerp(transform.position.x, target.position.x, Time.deltaTime * horizontalSpeed);
        float y = Mathf.Lerp(transform.position.y, target.position.y, Time.deltaTime * verticalSpeed);
        float z = Mathf.Lerp(transform.position.z, target.position.z, Time.deltaTime * horizontalSpeed);
        Vector3 targetPos = new Vector3(x, y, z);
        transform.position = targetPos;
    }
}
