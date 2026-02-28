Shader "Custom/Blueprint Ghost"
{
    Properties
    {
        _MainTex        ("Albedo Texture", 2D)            = "white" {}
        _BumpMap        ("Normal Map", 2D)                = "bump" {}
        [HDR] _Color    ("Color", Color)                  = (0.1, 0.45, 1.0, 1.0)
        [HDR] _RimColor ("Rim / Edge Color", Color)       = (0.3, 0.8, 1.0, 1.0)

        _FresnelPower   ("Fresnel Power",   Range(0, 10)) = 3.0
        _NormalStrength ("Normal Strength", Range(0, 2))  = 1.0
        _Power          ("Ghost Power",     Range(0, 1))  = 0.8

        _GridTiling     ("Grid Tiling",     Float)        = 12.0
        _GridThickness  ("Grid Thickness",  Range(0.01, 0.15)) = 0.03
        _GridBrightness ("Grid Brightness", Range(0, 2))  = 0.5

        _ScrollDirection ("Scroll Direction (XY)", Vector) = (0, 0.1, 0, 0)

        _ScanSpeed      ("Scan Speed",      Range(0, 3))  = 0.6
        _ScanSharpness  ("Scan Sharpness",  Range(1, 20)) = 6.0
        _ScanBrightness ("Scan Brightness", Range(0, 1))  = 0.25
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "IgnoreProjector"="True" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100
        Cull Back
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0

            #include "UnityCG.cginc"

            // ── Textures ─────────────────────────────────────────────────
            sampler2D _MainTex;   float4 _MainTex_ST;
            sampler2D _BumpMap;   float4 _BumpMap_ST;

            // ── Uniforms ──────────────────────────────────────────────────
            fixed4  _Color;
            fixed4  _RimColor;
            half    _FresnelPower;
            half    _NormalStrength;
            half    _Power;
            half    _GridTiling;
            half    _GridThickness;
            half    _GridBrightness;
            half2   _ScrollDirection;
            half    _ScanSpeed;
            half    _ScanSharpness;
            half    _ScanBrightness;

            // ── Structs ───────────────────────────────────────────────────
            struct appdata
            {
                float4 vertex  : POSITION;
                float2 uv      : TEXCOORD0;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos         : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float2 uvNormal    : TEXCOORD1;
                float  rim         : TEXCOORD2;
                float3 worldPos    : TEXCOORD3;
                // TBN rows packed into three interpolators
                float3 tbn0        : TEXCOORD4;
                float3 tbn1        : TEXCOORD5;
                float3 tbn2        : TEXCOORD6;
            };

            // ── Helpers ───────────────────────────────────────────────────

            // Procedural grid: 1 on a line, 0 elsewhere
            half GridFactor(float2 uv, half tiling, half thickness)
            {
                float2 g = frac(uv * tiling);
                float2 d = min(g, 1.0 - g);
                return saturate(step(d.x, thickness * 0.5) + step(d.y, thickness * 0.5));
            }

            // World-space horizontal scan band
            half ScanLine(float worldY, half speed, half sharpness)
            {
                float phase = frac(worldY * 0.25 - _Time.y * speed);
                return pow(1.0 - abs(phase - 0.5) * 2.0, sharpness);
            }

            // ── Vertex ────────────────────────────────────────────────────
            v2f vert(appdata v)
            {
                v2f o;

                o.pos      = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                // UVs — albedo scrolls, normal stays put
                o.uv       = TRANSFORM_TEX(v.uv, _MainTex) + _ScrollDirection * _Time.y;
                o.uvNormal = TRANSFORM_TEX(v.uv, _BumpMap);

                // Fresnel (object space, same approach as the Force Field shader)
                float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
                o.rim = 1.0 - saturate(dot(viewDir, v.normal));

                // Build world-space TBN for normal mapping
                float3 wNormal  = UnityObjectToWorldNormal(v.normal);
                float3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
                float  flip     = v.tangent.w * unity_WorldTransformParams.w;
                float3 wBitan   = cross(wNormal, wTangent) * flip;

                o.tbn0 = float3(wTangent.x, wBitan.x, wNormal.x);
                o.tbn1 = float3(wTangent.y, wBitan.y, wNormal.y);
                o.tbn2 = float3(wTangent.z, wBitan.z, wNormal.z);

                return o;
            }

            // ── Fragment ──────────────────────────────────────────────────
            fixed4 frag(v2f i) : SV_Target
            {
                // ── Normal map ────────────────────────────────────────────
                fixed3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uvNormal));
                normalTS.xy    *= _NormalStrength;
                // Transform to world space via packed TBN
                float3 normalWS = normalize(float3(
                    dot(i.tbn0, normalTS),
                    dot(i.tbn1, normalTS),
                    dot(i.tbn2, normalTS)
                ));

                // ── Albedo → luminance tint ───────────────────────────────
                fixed4 albedo    = tex2D(_MainTex, i.uv);
                half   lum       = dot(albedo.rgb, half3(0.299, 0.587, 0.114));

                // ── Fresnel — reuse rim from vertex, refine with normal ────
                //    Blend vertex rim with a normal-perturbed version
                half fresnel = pow(i.rim, _FresnelPower);

                // ── Grid ──────────────────────────────────────────────────
                half grid = GridFactor(i.uv, _GridTiling, _GridThickness);

                // ── Scan line ─────────────────────────────────────────────
                half scan = ScanLine(i.worldPos.y, _ScanSpeed, _ScanSharpness) * _ScanBrightness;

                // ── Compose — mirrors Force Field lerp pattern ────────────
                //    "opaque" version: flat tinted albedo
                fixed4 opaquePixel = albedo * _Color * (0.3 + lum * 0.7);

                //    "ghost" version: fresnel glow + grid + scan
                fixed4 ghostPixel  = _Color * (0.2 + lum * 0.4);
                ghostPixel.rgb    += fresnel       * _RimColor.rgb;
                ghostPixel.rgb    += grid          * _GridBrightness * _RimColor.rgb;
                ghostPixel.rgb    += scan          * _RimColor.rgb;
                ghostPixel        *= pow(_FresnelPower, i.rim);   // Force Field intensity trick
                ghostPixel         = lerp(0, ghostPixel, i.rim);  // hide center like FF shader
                ghostPixel         = clamp(ghostPixel, 0, _RimColor);

                //    Blend between full-opaque and ghost based on _Power
                fixed4 final   = lerp(opaquePixel, ghostPixel, _Power);
                final.a        = saturate(_Color.a * (0.2 + fresnel + grid * 0.3 + scan));

                return final;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}