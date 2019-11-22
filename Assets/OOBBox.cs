using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OOBBox : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerManager.instance.player.transform.position = PlayerManager.instance.entryPoint.transform.position;
    }
}
