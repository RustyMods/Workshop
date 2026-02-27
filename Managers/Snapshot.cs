using System.IO;
using System.Linq;
using UnityEngine;

namespace Workshop;

public static class Snapshot
{
    private const int layer = 3;

    public static void Run(GameObject prefab, string iconName, float lightIntensity = 1f, Quaternion? rotation = null)
    {
        if (TryCreate(prefab, out Sprite icon, lightIntensity, rotation))
        {
            icon.name = iconName;
            byte[] bytes = icon.texture.EncodeToPNG();
            string filePath = Path.Combine(ConfigManager.ConfigFolderPath, icon.name + ".png");
            File.WriteAllBytes(filePath, bytes);
        }
    }

    private static void CleanupVisual(GameObject visual)
    {
        foreach (Transform child in visual.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = layer;
        }

        foreach (ParticleSystem ps in visual.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Stop();
        }
    }
    
    public static bool TryCreate(GameObject prefab, out Sprite icon, float lightIntensity = 1.3f, Quaternion? cameraRotation = null)
    {
        icon = null;
        if (prefab == null) return false;
        if (!prefab.GetComponentsInChildren<Renderer>().Any() && !prefab.GetComponentsInChildren<MeshFilter>().Any())
        {
            return false;
        }

        Camera camera = new GameObject("CameraIcon", typeof(Camera)).GetComponent<Camera>();
        camera.backgroundColor = Color.clear;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(10000f, 10000f, 10000f);
        camera.transform.rotation = cameraRotation ?? Quaternion.Euler(0f, 180f, 0f);
        camera.fieldOfView = 0.5f;
        camera.farClipPlane = 100000;
        camera.cullingMask = 1 << layer;

        Light sideLight = new GameObject("LightIcon", typeof(Light)).GetComponent<Light>();
        sideLight.transform.position = new Vector3(10000f, 10000f, 10000f);
        sideLight.transform.rotation = Quaternion.Euler(5f, 180f, 5f);
        sideLight.type = LightType.Directional;
        sideLight.cullingMask = 1 << layer;
        sideLight.intensity = lightIntensity;

        GameObject visual = Object.Instantiate(prefab);
        CleanupVisual(visual);

        visual.transform.position = Vector3.zero;
        visual.transform.rotation = Quaternion.Euler(23, 51, 25.8f);

        visual.name = prefab.name;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
        
        Vector3 min = renderers.Aggregate(Vector3.positiveInfinity, (cur, renderer) => Vector3.Min(cur, renderer.bounds.min));
        Vector3 max = renderers.Aggregate(Vector3.negativeInfinity, (cur, renderer) => Vector3.Max(cur, renderer.bounds.max));
        // center the prefab
        visual.transform.position = (new Vector3(10000f, 10000f, 10000f)) - (min + max) / 2f;
        Vector3 size = max - min;

        // just in case it doesn't gets deleted properly later
        TimedDestruction timedDestruction = visual.AddComponent<TimedDestruction>();
        timedDestruction.Trigger(1f);
        
        Rect rect = new(0, 0, 256, 256);
        camera.targetTexture = RenderTexture.GetTemporary((int)rect.width, (int)rect.height);
        // camera.orthographic = true;
        // camera.orthographicSize = Mathf.Max(size.x, size.y, size.z) * 0.6f;
        camera.aspect = 1f;
        camera.fieldOfView = 20f;
        // calculate the Z position of the prefab as it needs to be far away from the camera
        float maxMeshSize = Mathf.Max(size.x, size.y) + 0.1f;
        float distance = maxMeshSize / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad) * 1.1f;

        camera.transform.position = new Vector3(10000f, 10000f, 10000f) + new Vector3(0, 0, distance);

        camera.Render();

        RenderTexture currentRenderTexture = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;

        Texture2D previewImage = new((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
        previewImage.ReadPixels(new Rect(0, 0, (int)rect.width, (int)rect.height), 0, 0);
        previewImage.Apply();

        RenderTexture.active = currentRenderTexture;
        
        Color[] pixels = previewImage.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a < 0.01f) continue; // skip transparent pixels

            // boost saturation and brightness
            Color.RGBToHSV(pixels[i], out float h, out float s, out float v);
            pixels[i] = Color.HSVToRGB(h, Mathf.Clamp01(s * 1.3f), Mathf.Clamp01(v * 1.2f));
            pixels[i].a = pixels[i+0].a; // preserve alpha
        }
        previewImage.SetPixels(pixels);
        previewImage.Apply();

        icon = Sprite.Create(previewImage, new Rect(0, 0, (int)rect.width, (int)rect.height), Vector2.one / 2f);
        sideLight.gameObject.SetActive(false);
        camera.targetTexture.Release();
        camera.gameObject.SetActive(false);
        visual.SetActive(false);
        if (ZNetScene.instance) ZNetScene.instance.Destroy(visual);
        else Object.DestroyImmediate(visual);

        Object.Destroy(camera);
        Object.Destroy(sideLight);
        Object.Destroy(camera.gameObject);
        Object.Destroy(sideLight.gameObject);

        return true;
    }
}