using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PodracingTracker;

/// <summary>
/// World-space translucent hollow shells for landing requirements when the "Training overlay" config toggle is on.
/// Shell is the volume between inner radius Min and outer radius Max around the requirement anchor.
/// </summary>
public static class TrainingSphereOverlay
{
    private const float Alpha = 0.02f;
    private const int ShellStacks = 16;
    private const int ShellSlices = 24;
    private const float SolidSphereMaxInner = 0.01f;

    private static Transform _root;
    private static Material _sharedMaterial;
    private static readonly List<GameObject> Pool = [];
    private static MaterialPropertyBlock _mpb;

    public static void Clear()
    {
        for (int i = 0; i < Pool.Count; i++)
        {
            if (Pool[i] != null)
                Pool[i].SetActive(false);
        }
    }

    public static void Sync(Location location)
    {
        if (location == null)
        {
            Clear();
            return;
        }

        EnsureRoot();
        int used = 0;

        foreach (Landing landing in location.Landings)
        {
            if (landing.IsLanded)
                continue;

            foreach (Requirement requirement in landing.Requirements)
            {
                bool mazeHidden =
                    LocationManager.mazeLandings.TryGetValue(requirement.Id, out string mazeVol) &&
                    mazeVol != UtilityTools.playerInMaze;
                if (mazeHidden)
                    continue;

                if (!LocationManager.TryGetRequirementTransform(requirement.Id, out Transform anchor))
                    continue;

                float outerR = requirement.Max;
                if (outerR <= 0f)
                    continue;

                float innerR = Mathf.Clamp(requirement.Min, 0f, outerR);
                if (innerR >= outerR)
                    continue;

                GameObject shell = GetPooledShell(used++);
                shell.transform.position = anchor.position;
                shell.transform.rotation = Quaternion.identity;
                shell.transform.localScale = Vector3.one;

                var meshFilter = shell.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null)
                {
                    mesh = new Mesh { name = "TrainingHollowShell" };
                    meshFilter.sharedMesh = mesh;
                }

                BuildHollowSphereMesh(mesh, innerR, outerR, ShellStacks, ShellSlices);

                var renderer = shell.GetComponent<Renderer>();
                _mpb ??= new MaterialPropertyBlock();
                renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor("_Color", RequirementStatusColor(requirement, landing));
                renderer.SetPropertyBlock(_mpb);

                shell.SetActive(true);
            }
        }

        for (int i = used; i < Pool.Count; i++)
            Pool[i].SetActive(false);
    }

    /// <summary>
    /// Green: landing complete (every requirement satisfied).
    /// White: this requirement not satisfied and no other on the landing is.
    /// Yellow: this requirement satisfied, but the landing is not complete.
    /// Red: this requirement not satisfied, but some other on the landing is.
    /// </summary>
    private static Color RequirementStatusColor(Requirement requirement, Landing landing)
    {
        bool thisMet = IsRequirementSatisfied(requirement);

        if (landing.RequirementsMet)
            return Tint(Color.green);

        if (thisMet)
            return Tint(Color.yellow);

        if (AnyOtherRequirementMet(landing, requirement))
            return Tint(Color.red);

        return Tint(Color.white);
    }

    private static bool IsRequirementSatisfied(Requirement r) =>
        r.RequirementsMet.Item1 && r.RequirementsMet.Item2;

    private static bool AnyOtherRequirementMet(Landing landing, Requirement self)
    {
        foreach (Requirement r in landing.Requirements)
        {
            if (ReferenceEquals(r, self))
                continue;
            if (IsRequirementSatisfied(r))
                return true;
        }
        return false;
    }

    private static Color Tint(Color rgb) =>
        new Color(rgb.r, rgb.g, rgb.b, Alpha);

    private static Vector3 DirOnSphere(float phi, float theta)
    {
        float sinPhi = Mathf.Sin(phi);
        return new Vector3(
            sinPhi * Mathf.Cos(theta),
            Mathf.Cos(phi),
            sinPhi * Mathf.Sin(theta));
    }

    private static void AddQuad(List<Vector3> v, List<int> tri, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int i = v.Count;
        v.Add(a);
        v.Add(b);
        v.Add(c);
        v.Add(d);
        tri.Add(i);
        tri.Add(i + 1);
        tri.Add(i + 2);
        tri.Add(i);
        tri.Add(i + 2);
        tri.Add(i + 3);
    }

    private static void AppendReversedWindingDuplicates(List<int> tri)
    {
        int n = tri.Count;
        for (int i = 0; i < n; i += 3)
        {
            tri.Add(tri[i]);
            tri.Add(tri[i + 2]);
            tri.Add(tri[i + 1]);
        }
    }

    /// <summary>
    /// Hollow shell: outer spherical surface at Max plus inner surface at Min (inward normals) so the gap reads as thickness. Double-sided copy for culling.
    /// </summary>
    private static void BuildHollowSphereMesh(Mesh mesh, float innerRadius, float outerRadius, int stacks, int slices)
    {
        outerRadius = Mathf.Max(outerRadius, innerRadius + 1e-4f);

        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        BuildSolidSphereQuadsInto(vertices, triangles, outerRadius, stacks, slices);

        if (innerRadius >= SolidSphereMaxInner)
            BuildSolidSphereQuadsIntoReversed(vertices, triangles, innerRadius, stacks, slices);

        AppendReversedWindingDuplicates(triangles);

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    private static void BuildSolidSphereQuadsInto(List<Vector3> vertices, List<int> triangles, float radius, int stacks, int slices)
    {
        for (int lat = 0; lat < stacks; lat++)
        {
            float phi0 = Mathf.PI * lat / stacks;
            float phi1 = Mathf.PI * (lat + 1) / stacks;

            for (int lon = 0; lon < slices; lon++)
            {
                float theta0 = 2f * Mathf.PI * lon / slices;
                float theta1 = 2f * Mathf.PI * (lon + 1) / slices;

                Vector3 d00 = DirOnSphere(phi0, theta0) * radius;
                Vector3 d01 = DirOnSphere(phi0, theta1) * radius;
                Vector3 d10 = DirOnSphere(phi1, theta0) * radius;
                Vector3 d11 = DirOnSphere(phi1, theta1) * radius;

                AddQuad(vertices, triangles, d00, d10, d11, d01);
            }
        }
    }

    private static void BuildSolidSphereQuadsIntoReversed(List<Vector3> vertices, List<int> triangles, float radius, int stacks, int slices)
    {
        for (int lat = 0; lat < stacks; lat++)
        {
            float phi0 = Mathf.PI * lat / stacks;
            float phi1 = Mathf.PI * (lat + 1) / stacks;

            for (int lon = 0; lon < slices; lon++)
            {
                float theta0 = 2f * Mathf.PI * lon / slices;
                float theta1 = 2f * Mathf.PI * (lon + 1) / slices;

                Vector3 d00 = DirOnSphere(phi0, theta0) * radius;
                Vector3 d01 = DirOnSphere(phi0, theta1) * radius;
                Vector3 d10 = DirOnSphere(phi1, theta0) * radius;
                Vector3 d11 = DirOnSphere(phi1, theta1) * radius;

                AddQuad(vertices, triangles, d00, d01, d11, d10);
            }
        }
    }

    private static void EnsureRoot()
    {
        if (_root != null)
            return;

        Pool.Clear();

        var go = new GameObject("PodracingTracker_TrainingSpheres");
        _root = go.transform;
    }

    private static Material GetSharedMaterial()
    {
        if (_sharedMaterial != null)
            return _sharedMaterial;

        Shader shader =
            Shader.Find("Unlit/Transparent")
            ?? Shader.Find("Sprites/Default")
            ?? Shader.Find("Legacy Shaders/Transparent/VertexLit")
            ?? Shader.Find("Standard");

        _sharedMaterial = new Material(shader);

        if (shader.name.StartsWith("Standard", StringComparison.Ordinal))
        {
            _sharedMaterial.SetFloat("_Mode", 3f);
            _sharedMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
            _sharedMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _sharedMaterial.SetInt("_ZWrite", 0);
            _sharedMaterial.DisableKeyword("_ALPHATEST_ON");
            _sharedMaterial.EnableKeyword("_ALPHABLEND_ON");
            _sharedMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            _sharedMaterial.EnableKeyword("_EMISSION");
            _sharedMaterial.SetColor("_EmissionColor", Color.white);
        }
        else
        {
            if (_sharedMaterial.HasProperty("_MainTex"))
                _sharedMaterial.mainTexture = Create1x1WhiteTexture();

            if (_sharedMaterial.HasProperty("_Cull"))
                _sharedMaterial.SetInt("_Cull", (int)CullMode.Off);
        }

        _sharedMaterial.renderQueue = (int)RenderQueue.Transparent;
        _sharedMaterial.color = Color.white;
        return _sharedMaterial;
    }

    private static Texture2D Create1x1WhiteTexture()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return tex;
    }

    private static GameObject GetPooledShell(int index)
    {
        while (Pool.Count <= index)
        {
            var go = new GameObject("TrainingHollowShell");
            go.transform.SetParent(_root, false);
            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh { name = "TrainingHollowShell" };
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = GetSharedMaterial();
            go.SetActive(false);
            Pool.Add(go);
        }
        return Pool[index];
    }
}
