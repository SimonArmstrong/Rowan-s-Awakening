using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Model : MonoBehaviour
{
    public Animator anim;

    public Transform headTransform;
    public Transform leftFootTransform;
    public Transform rightFootTransform;
    public Transform leftHandTransform;
    public Transform rightHandTransform;

    public float stepHeight;
    public float feetOffset;

    public bool useIK = false;

    public void Init() {
        leftFootTransform = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFootTransform = anim.GetBoneTransform(HumanBodyBones.RightFoot);
    }

    private void OnAnimatorIK(int layerIndex) {
        RaycastHit left, right;
        Physics.Raycast(new Ray(leftFootTransform.position + (Vector3.up * stepHeight), Vector3.down), out left, stepHeight * 2);
        Physics.Raycast(new Ray(rightFootTransform.position + (Vector3.up * stepHeight), Vector3.down), out right, stepHeight * 2);

        anim.SetIKPosition(AvatarIKGoal.LeftFoot, left.point + (Vector3.up * feetOffset));
        anim.SetIKPosition(AvatarIKGoal.RightFoot, right.point + (Vector3.up * feetOffset));
        anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, useIK ? 1 : 0);
        anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, useIK ? 1 : 0);

        //headTransform.LookAt(new Vector3(lookPos.x, lookPos.y + headHeightTransform.localPosition.y, lookPos.z));
    }
}
