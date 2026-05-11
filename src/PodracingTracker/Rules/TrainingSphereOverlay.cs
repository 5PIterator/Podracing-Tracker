using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PodracingTracker;

/// <summary>
/// World-space translucent spheres for landing requirements when Training Overlay is active.
/// </summary>
public static class TrainingSphereOverlay
{
    private const float Alpha = 0.2f;

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

                float radius = requirement.Max;
                if (radius <= 0f)
                    continue;

                GameObject sphere = GetPooledSphere(used++);
                sphere.transform.position = anchor.position;
                sphere.transform.rotation = Quaternion.identity;
                float diameter = 2f * radius;
                sphere.transform.localScale = new Vector3(diameter, diameter, diameter);

                var renderer = sphere.GetComponent<Renderer>();
                _mpb ??= new MaterialPropertyBlock();
                renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor("_Color", RequirementStatusColor(requirement));
                renderer.SetPropertyBlock(_mpb);

                sphere.SetActive(true);
            }
        }

        for (int i = used; i < Pool.Count; i++)
            Pool[i].SetActive(false);
    }

    private static Color RequirementStatusColor(Requirement requirement)
    {
        var met = requirement.RequirementsMet;
        bool minMet = met.Item1;
        bool maxMet = met.Item2;
        Color rgb = (!minMet && !maxMet) ? Color.red : (minMet && maxMet) ? Color.green : Color.yellow;
        return new Color(rgb.r, rgb.g, rgb.b, Alpha);
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

        var shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Transparent/Diffuse");
        _sharedMaterial = new Material(shader);
        _sharedMaterial.SetFloat("_Mode", 3f);
        _sharedMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
        _sharedMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        _sharedMaterial.SetInt("_ZWrite", 0);
        _sharedMaterial.DisableKeyword("_ALPHATEST_ON");
        _sharedMaterial.EnableKeyword("_ALPHABLEND_ON");
        _sharedMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        _sharedMaterial.renderQueue = 3000;
        _sharedMaterial.color = Color.white;
        return _sharedMaterial;
    }

    private static GameObject GetPooledSphere(int index)
    {
        while (Pool.Count <= index)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(_root, false);
            go.GetComponent<Renderer>().sharedMaterial = GetSharedMaterial();
            go.SetActive(false);
            Pool.Add(go);
        }
        return Pool[index];
    }
}
