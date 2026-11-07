using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static Utils.MathUtils;

public class Cuttable : MonoBehaviour
{
    private MeshFilter meshFilter;

    public Transform plane;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        Cut(plane.position, -plane.forward, plane.right);
    }

    private void RotateTriangle(ref int v1, ref int v2, ref int v3)
    {
        int temp = v2;
        v2 = v3;
        v3 = v1;
        v1 = temp;
    }

    private void AddTriangle(List<Vector3> list, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        list.Add(v1);
        list.Add(v2);
        list.Add(v3);
    }

    
    private void AddNormals(List<Vector3> list, Vector3 n1, Vector3 n2, Vector3 n3)
    {
        list.Add(n1.normalized);
        list.Add(n2.normalized);
        list.Add(n3.normalized);
    }

    private void InstantiateSubmesh(IReadOnlyList<Vector3> triangles, IReadOnlyList<Vector3> normals, string name, Color color)
    {
        Mesh mesh = new Mesh()
        {
            vertices = triangles.ToArray(),
            triangles = Enumerable.Range(0, triangles.Count).ToArray(),
            normals = normals.ToArray(),
        };
        // mesh.vertices = triangles.ToArray();
        // mesh.triangles = Enumerable.Range(0, triangles.Count).ToArray();
        // mesh.normals = normals.ToArray();
        GameObject obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.position = transform.position;
        obj.transform.rotation = transform.rotation;
        obj.transform.localScale = transform.localScale;
        obj.GetComponent<MeshFilter>().mesh = mesh;
        obj.GetComponent<MeshRenderer>().material = GetComponent<MeshRenderer>().material;
        obj.GetComponent<MeshRenderer>().material.color = color;
    }

    private void AddUnorderedLineSegment(in Dictionary<(int, int), (int, int)[]> dict, (int, int) id1, (int, int) id2)
    {
        if (dict.ContainsKey(id1)) {
            dict[id1][1] = id2;
        }
        else
        {
            dict[id1] = new (int, int)[]{id2, (-1, -1)};
        }
        if (dict.ContainsKey(id2)) {
            dict[id2][1] = id1;
        }
        else
        {
            dict[id2] = new (int, int)[]{id1, (-1, -1)};
        }
    }

    private (int, int) CreateIDPair(int n, int m)
    {
        if (n < m) return (m, n);
        return (n, m);
    }

    private void RegisterIntersection(Dictionary<(int, int), Vector3> registry, (int, int) idpair, Vector3 point)
    {
        if (!registry.ContainsKey(idpair)) registry[idpair] = point;
    }

    private int GetCanonicalID(Dictionary<Vector3, int> canonicalIDs, Vector3 point, int noncanonID)
    {
        if (canonicalIDs.ContainsKey(point)) return canonicalIDs[point];
        canonicalIDs.Add(point, noncanonID);
        return noncanonID;
    }

    private Vector3 ProjectDown(Vector3 v, Vector3 origin, Vector3 e1, Vector3 e2)
    {
        v = v - origin;
        return new Vector3(Vector3.Dot(v, e1), Vector3.Dot(v, e2), 0);
    } 

    private Vector3 ProjectUp(Vector3 v, Vector3 origin, Vector3 e1, Vector3 e2)
    {
        return e1 * v.x + e2 * v.y + origin;
    }

    // Given a dictionary and registry of int pairs that represent line segments, return a single loop contained in the dictionary as a looped linked list in the plane's subspace
    private Triangulation.LineSegmentNode[] parseLoop(in Dictionary<(int, int), (int, int)[]> dict, in Dictionary<(int, int), Vector3> registry, in HashSet<(int, int)> visited, Quaternion basis, Vector3 origin, (int, int) start, in int MAX_ITERATIONS)
    {
        Vector3 e1 = basis * Vector3.right;
        Vector3 e2 = basis * Vector3.up;

        List<Triangulation.LineSegmentNode> loop = new();

        int index = 0;
        int iter = 0;
        (int, int) last = start;
        (int, int) cur = dict[last][0];
        while (cur != start && iter < MAX_ITERATIONS)
        {
            visited.Add(cur);
            if (dict[cur][0] == last)
            {
                Vector3 projectedStart = ProjectDown(registry[cur], origin, e1, e2);
                Vector3 projectedEnd = ProjectDown(registry[dict[cur][1]], origin, e1, e2);
                loop.Add(new Triangulation.LineSegmentNode(){start = projectedStart, end = projectedEnd, next = index + 1});
                last = cur;
                cur = dict[cur][1];
            }
            else
            {
                Vector3 projectedStart = ProjectDown(registry[cur], origin, e1, e2);
                Vector3 projectedEnd = ProjectDown(registry[dict[cur][0]], origin, e1, e2);
                loop.Add(new Triangulation.LineSegmentNode(){start = projectedStart, end = projectedEnd, next = index + 1});
                last = cur;
                cur = dict[cur][0];
            }
            index++;
            iter++;
        }
        if (iter == MAX_ITERATIONS) Debug.LogWarning("Max iterations reached on line segment generation");
        // Close the loop
        visited.Add(cur);
        if (dict[cur][0] == last)
        {
            Vector3 projectedStart = ProjectDown(registry[cur], origin, e1, e2);
            Vector3 projectedEnd = ProjectDown(registry[dict[cur][1]], origin, e1, e2);
            loop.Add(new Triangulation.LineSegmentNode(){start = projectedStart, end = projectedEnd, next = 0});
        }
        else
        {
            Vector3 projectedStart = ProjectDown(registry[cur], origin, e1, e2);
            Vector3 projectedEnd = ProjectDown(registry[dict[cur][0]], origin, e1, e2);
            loop.Add(new Triangulation.LineSegmentNode(){start = projectedStart, end = projectedEnd, next = 0});
        }

        print(loop.Count);
        return loop.ToArray();
    }

    private List<Triangulation.LineSegmentNode[]> parseLineSegments(in Dictionary<(int, int), (int, int)[]> dict, in Dictionary<(int, int), Vector3> registry, Quaternion basis, Vector3 origin)
    {

        List<Triangulation.LineSegmentNode[]> loops = new();
        HashSet<(int, int)> visited = new();

        // List<Triangulation.LineSegmentNode> segments = new List<Triangulation.LineSegmentNode>();

        const int MAX_ITERATIONS = 2000;

        var keys = dict.Keys.ToArray();

        (int, int) first = dict.Keys.First();
        

        loops.Add(parseLoop(dict, registry, visited, basis, origin, first, MAX_ITERATIONS));

        return loops;
    }

    public void Cut(Vector3 origin, Vector3 normal, Vector3 e1)
    {
        origin = transform.InverseTransformPoint(origin);
        normal = transform.localToWorldMatrix.transpose.MultiplyVector(normal);

        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3[] normals = mesh.normals;

        List<Vector3> posTriangles = new();
        List<Vector3> negTriangles = new();
        List<Vector3> posNormals = new();
        List<Vector3> negNormals = new();
        
        Dictionary<(int, int), (int, int)[]> unsortedLineSegmentPairs = new();
        Dictionary<(int, int), Vector3> intersectionPoints = new();
        Dictionary<Vector3, int> canonicalVertexIDs = new();

        if (mesh.triangles.Length % 3 != 0)
        {
            Debug.LogError("Triangle array count not divisible by three");
            return;
        }

        for (int i = 0; i < triangles.Length / 3; i++)
        {
            int v1 = triangles[3 * i];
            int v2 = triangles[3 * i + 1];
            int v3 = triangles[3 * i + 2];

            // Becuase vertex IDs are not unique, we need to make them unique if we want to index our intersection vertices by which vertices they are between
            int canonv1 = GetCanonicalID(canonicalVertexIDs, vertices[v1], v1);
            int canonv2 = GetCanonicalID(canonicalVertexIDs, vertices[v2], v2);
            int canonv3 = GetCanonicalID(canonicalVertexIDs, vertices[v3], v3);

            bool positive1 = PointOnPlanePosSide(vertices[v1], origin, normal);
            bool positive2 = PointOnPlanePosSide(vertices[v2], origin, normal);
            bool positive3 = PointOnPlanePosSide(vertices[v3], origin, normal);

            if (positive1 && positive2 && positive3)
            {
                AddTriangle(posTriangles, vertices[v1], vertices[v2], vertices[v3]);
                AddNormals(posNormals, normals[v1], normals[v2], normals[v3]);
                
            }
            else if (!positive1 && !positive2 && !positive3)
            {
                AddTriangle(negTriangles, vertices[v1], vertices[v2], vertices[v3]);
                AddNormals(negNormals, normals[v1], normals[v2], normals[v3]);
            }
            else {
                // rotate so that v1 is always on the positive side, and v3 is always on the negative side
                while (!PointOnPlanePosSide(vertices[v1], origin, normal) || PointOnPlanePosSide(vertices[v3], origin, normal)) 
                {
                    RotateTriangle(ref v1, ref v2, ref v3);
                    RotateTriangle(ref canonv1, ref canonv2, ref canonv3);
                }
                if (PointOnPlanePosSide(vertices[v2], origin, normal))
                {
                    Vector3 intersection13 = GetLinePlaneIntersection(vertices[v1], vertices[v3], origin, normal, out float t13);
                    Vector3 normal13 = Vector3.Lerp(normals[v1], normals[v3], t13);

                    Vector3 intersection23 = GetLinePlaneIntersection(vertices[v2], vertices[v3], origin, normal, out float t23);
                    Vector3 normal23 = Vector3.Lerp(normals[v2], normals[v3], t23);
                    
                    (int, int) idpair13 = CreateIDPair(canonv1, canonv3);
                    RegisterIntersection(intersectionPoints, idpair13, intersection13);
                    (int, int) idpair23 = CreateIDPair(canonv2, canonv3);
                    RegisterIntersection(intersectionPoints, idpair23, intersection23);
                    
                    AddUnorderedLineSegment(unsortedLineSegmentPairs, idpair13, idpair23);

                    AddTriangle(posTriangles, vertices[v1], vertices[v2], intersection23);
                    AddNormals(posNormals, normals[v1], normals[v2], normal23);

                    AddTriangle(posTriangles, vertices[v1], intersection23, intersection13);
                    AddNormals(posNormals, normals[v1], normal23, normal13);

                    AddTriangle(negTriangles, intersection13, intersection23, vertices[v3]);
                    AddNormals(negNormals, normal13, normal23, normals[v3]);

                }
                else
                {
                    Vector3 intersection12 = GetLinePlaneIntersection(vertices[v1], vertices[v2], origin, normal, out float t12);
                    Vector3 normal12 = Vector3.Lerp(normals[v1], normals[v2], t12);

                    Vector3 intersection13 = GetLinePlaneIntersection(vertices[v1], vertices[v3], origin, normal, out float t13);
                    Vector3 normal13 = Vector3.Lerp(normals[v1], normals[v3], t13);

                    (int, int) idpair12 = CreateIDPair(canonv1, canonv2);
                    RegisterIntersection(intersectionPoints, idpair12, intersection12);
                    (int, int) idpair13 = CreateIDPair(canonv1, canonv3);
                    RegisterIntersection(intersectionPoints, idpair13, intersection13);
                    AddUnorderedLineSegment(unsortedLineSegmentPairs, idpair12, idpair13);

                    AddTriangle(posTriangles, vertices[v1], intersection12, intersection13);
                    AddNormals(posNormals, normals[v1], normal12, normal13);

                    AddTriangle(negTriangles, intersection12, vertices[v2], intersection13);
                    AddNormals(negNormals, normal12, normals[v2], normal13);

                    AddTriangle(negTriangles, intersection13, vertices[v2], vertices[v3]);
                    AddNormals(negNormals, normal13, normals[v2], normals[v3]);


                }
            }
            
        }

        // Did we generate any new vertices?
        if (unsortedLineSegmentPairs.Count != 0)
        {
            Quaternion basis = Quaternion.LookRotation(normal, Vector3.Cross(normal, e1));
            Triangulation.LineSegmentNode[] lineSegments = parseLineSegments(unsortedLineSegmentPairs, intersectionPoints, basis, origin)[0];
            List<Vector3> cap = Triangulation.Triangulate(lineSegments, 100, 0.001f);

            cap = cap.Select(
                v => ProjectUp(v, origin, basis * Vector3.right, basis * Vector3.up)
            ).ToList();

            posTriangles.AddRange(cap);
            posNormals.AddRange(Enumerable.Repeat(-normal, cap.Count));

            for (int i = 0; i < cap.Count / 3; i++) // Reverse the winding for the other cap
            {
                Vector3 temp = cap[3 * i];
                cap[3 * i] = cap[3 * i + 1];
                cap[3 * i + 1] = temp;
            }
            negTriangles.AddRange(cap);
            negNormals.AddRange(Enumerable.Repeat(normal, cap.Count));
        }

        InstantiateSubmesh(posTriangles, posNormals, "Pos", Color.green);
        InstantiateSubmesh(negTriangles, negNormals, "Neg", Color.red);

        Destroy(gameObject);
    }
}
