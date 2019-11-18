using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ToonLightController : MonoBehaviour
{
    

    // Update is called once per frame
    void Update()
    {
        //Store the rotation of the light in_Light_Direction
        Shader.SetGlobalVector("_Light_Direction", -transform.forward);
    }
}
 