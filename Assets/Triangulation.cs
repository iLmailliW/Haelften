using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utils.MathUtils;

public static class Triangulation
{
    public struct LineSegmentNode
    {
        public Vector3 start;
        public Vector3 end;

        public int next;
    }

    // bool Raycast(Vector2 point, Vector2 direction, LineSegmentNode lineSegment)
    // {
    //     Vector2 lineDir = lineSegment.end - lineSegment.start;
    //     // parallel case. return false because either they are parallel, or the endpoints of the parallel line will be counted anyways
    //     if (Mathf.Abs(Cross2x2(lineDir.normalized, direction.normalized)) < EPSILON)
    //     {
    //         return false;
    //     }
    //     Vector2 solution = SolveAugmented2x2(direction, lineDir, point, lineSegment.start);
    //     // exclude 1 so that vertices are only counted once
    //     return solution.x >= 0 && 0 <= solution.y && solution.y < 1;
    // }

    // // returns whether a point is in the interior of a 2d shape, tested using raycasts
    // bool IsInterior(Vector2 point, int startSegment, ReadOnlyCollection<LineSegmentNode> list)
    // {
    //     int cursegment = startSegment;
    //     int intersections = 0;
    //     do
    //     {
    //         if (Raycast(point, Vector2.up, list[cursegment])) intersections++;
    //         cursegment = list[cursegment].next;
    //     }
    //     while (cursegment != startSegment);
    //     int vote1 = intersections % 2;

    //     intersections = 0;
    //     do
    //     {
    //         if (Raycast(point, DIAG1, list[cursegment])) intersections++;
    //         cursegment = list[cursegment].next;
    //     }
    //     while (cursegment != startSegment);
    //     int vote2 = intersections % 2;

    //     intersections = 0;
    //     do
    //     {
    //         if (Raycast(point, DIAG2, list[cursegment])) intersections++;
    //         cursegment = list[cursegment].next;
    //     }
    //     while (cursegment != startSegment);
    //     int vote3 = intersections % 2;

    //     return vote1 + vote2 + vote3 >= 2;
    // }

    // int CountSides(ReadOnlyCollection<LineSegmentNode> list, int start)
    // {
    //     int cur = start;
    //     int count = 0;
    //     do
    //     {
    //         cur = list[cur].next;
    //         count++;
    //     }
    //     while(cur != start);
    //     return count;
    // }

    static float ShoeLaceSignedArea(ReadOnlySpan<LineSegmentNode> list)
    {
        float sum = 0;
        for (int i = 0; i < list.Length; i++)
        {
            sum += 0.5f * Cross2x2(list[i].start, list[i].end);
        }
        return sum;
    }

    static bool PointInTriangle(Vector2 point, Vector2 v1, Vector2 v2, Vector2 v3, in float EPSILON)
    {
        float cross12 = Mathf.Abs(Cross2x2(v2 - v1, point - v1));
        float cross23 = Mathf.Abs(Cross2x2(v3 - v2, point - v2));
        float cross31 = Mathf.Abs(Cross2x2(v1 - v3, point - v3));

        return cross12 + cross23 + cross31 < Mathf.Abs(Cross2x2(v2 - v1, v3 - v2)) + EPSILON;
    }

    // Checks whether connecting the next two line segments starting from segments[first] will intersect any other lines
    static bool CheckIntersections(in LineSegmentNode[] segments, int first, in float EPSILON)
    {
        int second = segments[first].next;
        int cur = segments[second].next; // skip the two line segments we are trying to connect
        do
        {
            if (PointInTriangle(segments[cur].end, segments[first].start, segments[first].end, segments[second].end, EPSILON)) return true;
            cur = segments[cur].next;
        }
        while (segments[cur].next != first);
        return false;
    }

    public static List<Vector3> Triangulate(in LineSegmentNode[] segments, in int MAX_ITERATIONS, in float EPSILON)
    {
        LineSegmentNode[] mutSegments = (LineSegmentNode[])segments.Clone();
        

        float sign;
        if (ShoeLaceSignedArea(mutSegments) > 0) // check the winding of the fill area
        {
            sign = -1.0f; // counter clockwise
        }
        else
        {
            sign = 1.0f; // cw
        }

        List<Vector3> triangles = new List<Vector3>();

        int sides = mutSegments.Length;
        int index = 0;
        int iter = 0;
        while (sides > 3 && iter < MAX_ITERATIONS)
        {
            ref LineSegmentNode cur = ref mutSegments[index];
            LineSegmentNode next = mutSegments[cur.next];

            float angle = Cross2x2(cur.start - cur.end, next.end - cur.end);

            if (Mathf.Abs(angle) < EPSILON) // Degenerate triangle. connect endpoints but don't create a triangle
            {
                cur.next = next.next;
                cur.end = next.end;
                sides--;
                continue;
            }
            if (angle * sign > 0) // Angle is less than 180 degrees, so line segment might be inside shape, check whether this line will intersect another line
            {
                if (CheckIntersections(mutSegments, index, EPSILON))
                {
                    index = cur.next;
                    continue;
                }

                if (sign > 0)
                {
                    triangles.Add(cur.start);
                    triangles.Add(cur.end);
                    triangles.Add(next.end);
                }
                else
                {
                    triangles.Add(cur.start);
                    triangles.Add(next.end);
                    triangles.Add(cur.end);
                }
                cur.next = next.next;
                cur.end = next.end;
                sides--;
            }
            else
            {
                index = cur.next;
            }


            iter++;
        }

        if (sign > 0) // Add remaining triangle
        {
            triangles.Add(mutSegments[index].start);
            triangles.Add(mutSegments[index].end);
            triangles.Add(mutSegments[mutSegments[index].next].end);
        }
        else
        {
            triangles.Add(mutSegments[index].start);
            triangles.Add(mutSegments[mutSegments[index].next].end);
            triangles.Add(mutSegments[index].end);
        }

        return triangles;
    }

}
