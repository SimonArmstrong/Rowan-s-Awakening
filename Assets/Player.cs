using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private float moveSpeed = 5;
    [SerializeField] private float rotateSpeed = 30;

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement() {
        Vector3 movement = LeftJoystick();

        anim.SetFloat(StaticStrings.anim_moveSpeed, movement.magnitude);
        anim.SetFloat(StaticStrings.anim_horizontal, movement.x);
        anim.SetFloat(StaticStrings.anim_vertical, movement.y);

        Vector3 lookPos = transform.position + movement - transform.position;

        Quaternion tr = Quaternion.LookRotation(lookPos);
        Quaternion targetRot = Quaternion.Slerp(transform.rotation, tr, Time.deltaTime * movement.magnitude * rotateSpeed);
        transform.rotation = targetRot;
        
        transform.position += movement.normalized * movement.magnitude * Time.deltaTime * moveSpeed;
    }

    private Vector3 LeftJoystick() {
        Vector3 x = Input.GetAxisRaw(StaticStrings.horizontal) * Camera.main.transform.right;
        Vector3 y = Input.GetAxisRaw(StaticStrings.vertical) * Camera.main.transform.forward;
        Vector3 r = x + y;
        r.y = 0;
        return r;
    }

    private Vector3 RightJoystick() {
        float x = Input.GetAxisRaw(StaticStrings.r_horizontal);
        float y = Input.GetAxisRaw(StaticStrings.r_vertical);
        Vector3 r = new Vector3(x, y, 0);
        return r;
    }
}
