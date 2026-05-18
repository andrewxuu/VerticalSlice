Shader "Custom/SnowURP"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1,1,1,1)

        [Header(Snow)]
        _SnowColor("Snow Color", Color) = (0.95, 0.96, 1.0, 1)
        _MaxSnowThickness("Max Snow Thickness", Range(0, 0.5)) = 0.05
        _SnowNormalCutoff("Normal Cutoff", Range(0, 1)) = 0.8
        _SnowEdgeSharpness("Edge Sharpness", Range(1, 30)) = 20
        _SnowSmoothness("Snow Smoothness", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
        }

        // ====================================================================
        // FORWARD LIT PASS
        // ====================================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half4  _SnowColor;
                float  _MaxSnowThickness;
                float  _SnowNormalCutoff;
                float  _SnowEdgeSharpness;
                half   _SnowSmoothness;
            CBUFFER_END

            // Set globally every frame by SnowManager.cs
            float _SnowDepth;

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float  snowMask   : TEXCOORD3; // per-vertex snow coverage [0,1]
                half   fogCoord   : TEXCOORD4;
                half3  vertexSH   : TEXCOORD5; // spherical harmonics — fixes black in game view
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Returns [0,1]: how much this surface should receive snow.
            // Uses a tight cutoff so only near-flat tops are affected —
            // side faces of hex tiles get 0 and are never displaced.
            float SnowNormalMask(float3 normalWS)
            {
                float upDot = dot(normalize(normalWS), float3(0, 1, 0));
                return saturate((upDot - _SnowNormalCutoff) * _SnowEdgeSharpness);
            }

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float  mask     = SnowNormalMask(normalWS);

                // ── Displacement ──────────────────────────────────────────
                // Push only in world Y (straight up), NOT along the vertex normal.
                // Displacing along normals pushes edge verts outward and opens
                // seams between hex tiles. World-Y keeps side faces stationary.
                float  displacement = _SnowDepth * _MaxSnowThickness * mask;
                float3 worldUp      = TransformWorldToObjectDir(float3(0, 1, 0));
                float3 positionOS   = IN.positionOS.xyz + worldUp * displacement;

                OUT.positionWS = TransformObjectToWorld(positionOS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS   = normalWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);

                // Snow colour coverage ramps up with depth so it spreads
                // visually across the surface as accumulation grows.
                OUT.snowMask = mask * saturate(_SnowDepth * 2.0);

                OUT.fogCoord = ComputeFogFactor(OUT.positionCS.z);

                // Bake spherical harmonics per vertex.
                // Fed into inputData.bakedGI in the fragment — without this
                // UniversalFragmentPBR returns black in game view.
                OUTPUT_SH(normalWS, OUT.vertexSH);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                half3 albedo     = lerp(baseTex.rgb, _SnowColor.rgb, IN.snowMask);
                half  smoothness = lerp(0.1h, _SnowSmoothness, IN.snowMask);

                // ── InputData ─────────────────────────────────────────────
                InputData inputData = (InputData)0;
                inputData.positionWS              = IN.positionWS;
                inputData.normalWS                = normalize(IN.normalWS);
                inputData.viewDirectionWS         = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord             = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord                = IN.fogCoord;
                inputData.vertexLighting          = half3(0, 0, 0);
                inputData.bakedGI                 = SAMPLE_GI(0, IN.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask              = half4(1, 1, 1, 1);

                // ── SurfaceData ───────────────────────────────────────────
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo      = albedo;
                surfaceData.alpha       = 1;
                surfaceData.smoothness  = smoothness;
                surfaceData.metallic    = 0;
                surfaceData.occlusion   = 1;
                surfaceData.normalTS    = half3(0, 0, 1);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb   = MixFog(color.rgb, IN.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // ====================================================================
        // SHADOW CASTER PASS
        // Mirrors the same Y-only displacement so shadows match the geometry.
        // Include order: Core → Lighting → Shadows (Lighting pulls in helpers
        // like LerpWhiteTo that Shadows.hlsl needs — wrong order = compile error).
        // ====================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   shadowVert
            #pragma fragment shadowFrag
            #pragma target   3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half4  _SnowColor;
                float  _MaxSnowThickness;
                float  _SnowNormalCutoff;
                float  _SnowEdgeSharpness;
                half   _SnowSmoothness;
            CBUFFER_END

            float  _SnowDepth;
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attr
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Vary
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 GetShadowPositionHClip(float3 positionWS, float3 normalWS)
            {
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Vary shadowVert(Attr IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Vary OUT;
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float  upDot    = dot(normalize(normalWS), float3(0, 1, 0));
                float  mask     = saturate((upDot - _SnowNormalCutoff) * _SnowEdgeSharpness);

                float  displacement = _SnowDepth * _MaxSnowThickness * mask;
                float3 worldUp      = TransformWorldToObjectDir(float3(0, 1, 0));
                float3 positionOS   = IN.positionOS.xyz + worldUp * displacement;
                float3 positionWS   = TransformObjectToWorld(positionOS);

                OUT.positionCS = GetShadowPositionHClip(positionWS, normalWS);
                return OUT;
            }

            half4 shadowFrag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
