using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Workshop;

public static class ShaderMan
{
    private static readonly Dictionary<string, Shader> shaders;

    static ShaderMan()
    {
        shaders = new Dictionary<string, Shader>();
    }

    public static Shader GetShader(string shaderName, Shader originalShader)
    {
        return !shaders.TryGetValue(shaderName, out Shader shader) ? originalShader : shader;
    }

    public static void OnFejdStartup()
    {
        AssetBundle[] assetBundles = Resources.FindObjectsOfTypeAll<AssetBundle>();
        foreach (AssetBundle bundle in assetBundles)
        {
            IEnumerable<Shader> bundleShaders;
            try
            {
                bundleShaders = bundle.isStreamedSceneAssetBundle && bundle
                    ? bundle.GetAllAssetNames().Select(bundle.LoadAsset<Shader>).Where(shader => shader != null)
                    : bundle.LoadAllAssets<Shader>();
            }
            catch (Exception)
            {
                continue;
            }

            if (bundleShaders == null) continue;
            foreach (Shader shader in bundleShaders)
            {
                if (shaders.ContainsKey(shader.name)) continue;
                shaders[shader.name] = shader;
            }
        }
    }
}

// TextMeshPro/Distance Field
// Custom/LitGui
// Custom/GuiScroll
// UI/Heat Distort
// Lux Lit Particles/ Bumped
// Lux Lit Particles/ Tess Bumped
// Hidden/SimpleClear
// Hidden/SunShaftsComposite
// Hidden/Dof/DX11Dof
// Hidden/Dof/DepthOfFieldHdr
// TextMeshPro/Distance Field (Surface)
// Custom/AlphaParticle
// Unlit/BeaconBeam
// Custom/Blob
// Custom/Bonemass
// Particles/Standard Surface2
// Particles/Standard Unlit2
// Custom/Clouds
// Custom/Creature
// Custom/Decal
// Custom/Distortion
// Custom/Flow
// Custom/FlowOpaque
// Custom/Grass
// Custom/Heightmap
// Unlit/Invis
// Unlit/Lighting
// Custom/LitParticles
// Custom/Mesh Flipbook Particle
// Custom/ParticleDecal
// Custom/Gradient Mapped Particle (Unlit)
// Custom/Particle (Unlit)
// Custom/Piece
// Custom/Player
// Custom/Rug
// Custom/ShadowBlob
// Custom/SkyObject
// Custom/SkyboxProcedural
// Standard TwoSided
// Custom/StaticRock
// Custom/Tar
// Custom/Trilinearmap
// Custom/Vegetation
// Custom/Water
// Custom/WaterMask
// Unlit/WeaponGlow
// Custom/Yggdrasil_root
// Hidden/RadialSegementShader