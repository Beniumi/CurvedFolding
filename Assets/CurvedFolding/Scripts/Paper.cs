using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CurvedFoldingSystem
{
    public class Paper
    {
        List<Vector3> vertices = new List<Vector3>();
        float max;

        public List<Vector3> Vertices
        {
            get { return vertices; }
            set
            {
                vertices = new List<Vector3>(value);
                SetMax(vertices);
                if(vertices.Count > 0)
                    vertices.Add(vertices[0]);
            }
        }

        void SetMax(List<Vector3> vertices)
        {
            Bounds bounds = new Bounds();
            foreach (Vector3 v in vertices)
                bounds.Encapsulate(v);
            max = Vector3.Magnitude(bounds.max - bounds.min);
        }

        public Paper()
        {
        }

        public Paper(List<Vector3> vertices)
        {
            Vertices = vertices;
        }

        public bool IsCrossing(Vector3 pts, Vector3 vec)
        {
            for (int i = 1; i < vertices.Count; i++)
                if (IsCrossing(pts, pts + vec * max, vertices[i - 1], vertices[i]))
                    return true;
            return false;
        }

        public List<Vector3> GetIntersections(Vector3 pts, Vector3 vec)
        {
            List<Vector3> Intersections = new List<Vector3>();
            for (int i = 1; i < vertices.Count; i++)
                if (IsCrossing(pts, pts + vec * max, vertices[i - 1], vertices[i]))
                    Intersections.Add(GetIntersection(pts, pts + vec * max, vertices[i - 1], vertices[i]));
            //Intersections.Sort((a, b) => (b - pts).sqrMagnitude.CompareTo((a - pts).sqrMagnitude));
            return Intersections;
        }

        #region static method

        static public bool IsCrossing(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
        {
            double s1 = Vector3.Cross(B - A, C - A).y;
            double t1 = Vector3.Cross(B - A, D - A).y;
            double s2 = Vector3.Cross(D - C, A - C).y;
            double t2 = Vector3.Cross(D - C, B - C).y;
            if (s1 * t1 < 0 && s2 * t2 < 0)
                return true;
            return false;
        }

        static public Vector3 GetIntersection(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
        {
            Vector3 n1 = (B - A).normalized;
            Vector3 n2 = (D - C).normalized;
            float dotn1n2 = Vector3.Dot(n1, n2);
            float d1 = Vector3.Dot(n1 - dotn1n2 * n2, C - A) / (1 - Mathf.Pow(dotn1n2, 2));
            //float d2 = Vector3.Dot(-n2 + dotn1n2 * n1, C - A) / (1 - Mathf.Pow(dotn1n2, 2));
            return A + d1 * n1;
            //return C + d2 * n2;
        }

        #endregion
    }
}
