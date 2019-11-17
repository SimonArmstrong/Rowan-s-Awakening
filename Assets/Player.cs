using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private float moveSpeed = 5;
    [SerializeField] private float rotateSpeed = 30;
    [SerializeField] private Transform cameraTransform;

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement() {
        Vector3 movement = Joystick();

        float m = Mathf.Abs(movement.x) + Mathf.Abs(movement.z);
        anim.SetFloat(StaticStrings.anim_moveSpeed, Mathf.Clamp01(m));
        anim.SetFloat(StaticStrings.anim_horizontal, movement.normalized.x);
        anim.SetFloat(StaticStrings.anim_vertical, movement.normalized.y);

        Vector3 lookPos = transform.position + movement;

        Quaternion tr = Quaternion.LookRotation(lookPos);
        Quaternion targetRot = Quaternion.Slerp(transform.rotation, tr, Time.deltaTime * m * rotateSpeed);
        transform.rotation = targetRot;

        transform.position += movement.normalized * Time.deltaTime * moveSpeed;
    }

    private Vector3 Joystick() {
        Vector3 x = Input.GetAxisRaw(StaticStrings.horizontal) * cameraTransform.right;
        Vector3 y = Input.GetAxisRaw(StaticStrings.vertical) * cameraTransform.forward;
        Vector3 r = x + y;
        r.y = 0;
        return r;
    }
}
