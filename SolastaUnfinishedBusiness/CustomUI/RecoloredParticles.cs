using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Builders;
using TA.AddressableAssets;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace SolastaUnfinishedBusiness.CustomUI;

// Builds recolored copies of vanilla particle prefabs at runtime. The originals are never changed, as other spells
// share them. Vanilla particles load in the background, so each copy is built once its original has loaded, and
// only then handed to the definition. Until that happens the definition keeps showing the original colors.
internal static class RecoloredParticles
{
    private static readonly List<Entry> Pending = [];

    // hue is 0 to 1 on the color wheel; colors less saturated than minSaturation are raised to it, so white and
    // grey particles pick up the new hue too. Brightness and transparency are kept.
    internal static void Recolor(
        [CanBeNull] AssetReference original,
        string name,
        float hue,
        float minSaturation,
        Action<AssetReference> apply)
    {
        if (original == null || !original.RuntimeKeyIsValid())
        {
            return;
        }

        Pending.Add(new Entry(original.RuntimeKey, name, hue, minSaturation, apply));
    }

    internal static void Load()
    {
        if (Pending.Count == 0)
        {
            return;
        }

        var runner = new GameObject("RecoloredParticles").AddComponent<Runner>();

        Object.DontDestroyOnLoad(runner.gameObject);

        // copies live under an inactive holder so they never show or run in the scene themselves
        var holder = new GameObject("Prefabs");

        holder.SetActive(false);
        holder.transform.SetParent(runner.transform, false);
        runner.StartCoroutine(BuildAll(holder.transform));
    }

    private static IEnumerator BuildAll(Transform holder)
    {
        while (!SyncAddressables.Ready)
        {
            yield return null;
        }

        foreach (var entry in Pending)
        {
            // the handle is never released, so the original and its textures stay loaded while the copy uses them
            var handle = Addressables.LoadAssetAsync<GameObject>(entry.Key);

            while (!handle.IsDone)
            {
                yield return null;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded || !handle.Result)
            {
                Main.Error($"Recolored particle {entry.Name}: could not load the original {entry.Key}.");
                continue;
            }

            Build(entry, handle.Result, holder);
        }

        Pending.Clear();
    }

    private static void Build(Entry entry, GameObject original, Transform holder)
    {
        try
        {
            var copy = Object.Instantiate(original, holder);
            var guid = GuidHelper.Create(DefinitionBuilder.CeNamespaceGuid, entry.Name).ToString();

            copy.name = entry.Name;
            Tint(copy, entry.Hue, entry.MinSaturation);
            CustomModels.AddParticlePrefab(guid, copy);
            entry.Apply(new AssetReference(guid));
        }
        catch (Exception ex)
        {
            Main.Error($"Recolored particle {entry.Name}: {ex}");
        }
    }

    private static void Tint(GameObject root, float hue, float minSaturation)
    {
        foreach (var particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particleSystem.main;

            main.startColor = Shift(main.startColor, hue, minSaturation);

            var colorOverLifetime = particleSystem.colorOverLifetime;

            if (colorOverLifetime.enabled)
            {
                colorOverLifetime.color = Shift(colorOverLifetime.color, hue, minSaturation);
            }

            var colorBySpeed = particleSystem.colorBySpeed;

            if (colorBySpeed.enabled)
            {
                colorBySpeed.color = Shift(colorBySpeed.color, hue, minSaturation);
            }

            var trails = particleSystem.trails;

            if (!trails.enabled)
            {
                continue;
            }

            trails.colorOverLifetime = Shift(trails.colorOverLifetime, hue, minSaturation);
            trails.colorOverTrail = Shift(trails.colorOverTrail, hue, minSaturation);
        }

        foreach (var lineRenderer in root.GetComponentsInChildren<LineRenderer>(true))
        {
            lineRenderer.colorGradient = Shift(lineRenderer.colorGradient, hue, minSaturation);
        }

        foreach (var trailRenderer in root.GetComponentsInChildren<TrailRenderer>(true))
        {
            trailRenderer.colorGradient = Shift(trailRenderer.colorGradient, hue, minSaturation);
        }

        foreach (var light in root.GetComponentsInChildren<Light>(true))
        {
            light.color = Shift(light.color, hue, minSaturation);
        }

        // materials are copied, never changed in place, as the original prefab and other effects share them
        var materials = new Dictionary<Material, Material>();

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterials = renderer.sharedMaterials
                .Select(material => Shift(material, materials, hue, minSaturation))
                .ToArray();
        }
    }

    private static Material Shift(
        Material material, Dictionary<Material, Material> materials, float hue, float minSaturation)
    {
        if (!material)
        {
            return material;
        }

        if (materials.TryGetValue(material, out var copy))
        {
            return copy;
        }

        copy = new Material(material) { name = material.name + "Recolored" };

        var shader = copy.shader;

        for (var i = 0; i < shader.GetPropertyCount(); i++)
        {
            if (shader.GetPropertyType(i) != ShaderPropertyType.Color)
            {
                continue;
            }

            var id = shader.GetPropertyNameId(i);

            copy.SetColor(id, Shift(copy.GetColor(id), hue, minSaturation));
        }

        materials.Add(material, copy);

        return copy;
    }

    private static ParticleSystem.MinMaxGradient Shift(
        ParticleSystem.MinMaxGradient gradient, float hue, float minSaturation)
    {
        return gradient.mode switch
        {
            ParticleSystemGradientMode.Color =>
                new ParticleSystem.MinMaxGradient(Shift(gradient.color, hue, minSaturation)),
            ParticleSystemGradientMode.TwoColors =>
                new ParticleSystem.MinMaxGradient(
                    Shift(gradient.colorMin, hue, minSaturation),
                    Shift(gradient.colorMax, hue, minSaturation)),
            ParticleSystemGradientMode.Gradient =>
                new ParticleSystem.MinMaxGradient(Shift(gradient.gradient, hue, minSaturation)),
            ParticleSystemGradientMode.TwoGradients =>
                new ParticleSystem.MinMaxGradient(
                    Shift(gradient.gradientMin, hue, minSaturation),
                    Shift(gradient.gradientMax, hue, minSaturation)),
            ParticleSystemGradientMode.RandomColor =>
                new ParticleSystem.MinMaxGradient(Shift(gradient.gradient, hue, minSaturation))
                {
                    mode = ParticleSystemGradientMode.RandomColor
                },
            _ => gradient
        };
    }

    [CanBeNull]
    private static Gradient Shift([CanBeNull] Gradient gradient, float hue, float minSaturation)
    {
        if (gradient == null)
        {
            return null;
        }

        return new Gradient
        {
            mode = gradient.mode,
            colorKeys = gradient.colorKeys
                .Select(key => new GradientColorKey(Shift(key.color, hue, minSaturation), key.time))
                .ToArray(),
            alphaKeys = gradient.alphaKeys
        };
    }

    private static Color Shift(Color color, float hue, float minSaturation)
    {
        Color.RGBToHSV(color, out _, out var saturation, out var value);

        var shifted = Color.HSVToRGB(hue, Mathf.Max(saturation, minSaturation), value, true);

        shifted.a = color.a;

        return shifted;
    }

    private sealed class Entry(object key, string name, float hue, float minSaturation, Action<AssetReference> apply)
    {
        internal object Key { get; } = key;
        internal string Name { get; } = name;
        internal float Hue { get; } = hue;
        internal float MinSaturation { get; } = minSaturation;
        internal Action<AssetReference> Apply { get; } = apply;
    }

    private sealed class Runner : MonoBehaviour;
}
