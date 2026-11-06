using Unity.Mathematics;
using UnityEngine;

namespace Utils
{
    public static class MathUtils
    {
        public static float Cross2x2(Vector2 v1, Vector2 v2)
        {
            return v1.x * v2.y - v1.y * v2.x;
        }

        /// <summary>
        /// Determine whether a point is on the normal-facing side of a plane
        /// </summary>
        /// <param name="point"> </param>
        /// <param name="origin"> An arbitrary point on the plane </param>
        /// <param name="normal"> The normal of the plane </param>
        /// <returns> Returns true when the point is on the normal-facing side of a plane or is on the plane, 0 otherwise. </returns>
        public static bool PointOnPlanePosSide(Vector3 point, Vector3 origin, Vector3 normal)
        {
            Vector3 diff = point - origin;
            return Vector3.Dot(diff, normal) > 0;
        }

        

        /// <summary>
        /// Determine the intersection of a line segment and a plane, assuming one exists.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="origin"></param>
        /// <param name="normal"></param>
        /// <returns> Returns the point of intersection. UB if the line segment is parallel or non intersecting </returns>
        public static Vector3 GetLinePlaneIntersection(Vector3 v1, Vector3 v2, Vector3 origin, Vector3 normal, out float t)
        {
            t = Vector3.Dot(origin - v1, normal) / Vector3.Dot(v2 - v1, normal);
            return Vector3.Lerp(v1, v2, t);
        }

        // solves the linear equation v1 * x + c1 = v2 * y + c2, assuming there is a solution. UB if a solution doesn't exist
        public static Vector2 SolveAugmented2x2(Vector2 v1, Vector2 v2, Vector2 c1, Vector2 c2)
        {
            Vector2 deltac = c2 - c1;
            float vectorCross = Cross2x2(v1, v2);
            return new Vector2(Cross2x2(deltac, v2) / vectorCross, Cross2x2(deltac, v1) / vectorCross);
        }
    }
}

