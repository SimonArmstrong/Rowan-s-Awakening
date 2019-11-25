using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameEngine {
    //
    // Summary:
    //      Returns the normal found 5 meters below the input position
    public static RaycastHit GetGroundHit(this Vector3 pos, float dist = (5f)) {
        RaycastHit hit;
        Physics.Raycast(new Ray(pos, Vector3.down), out hit, dist, 9);

        return hit;
    }

    public static void DrawZPlaneCrossGizmo(Vector3 position, float radius) {
        Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        for (int i = 0; i < 4; i++)
        {
            Debug.DrawRay(position, dirs[i] * radius, Color.blue);
        }
    }
}
