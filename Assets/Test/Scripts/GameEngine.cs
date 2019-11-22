using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameEngine {
    //
    // Summary:
    //      Returns the normal found 5 meters below the input position
    public static RaycastHit GetGroundHit(this Vector3 pos) {
        RaycastHit hit;
        Physics.Raycast(new Ray(pos, Vector3.down), out hit, 5f, 9);

        return hit;
    }
}
