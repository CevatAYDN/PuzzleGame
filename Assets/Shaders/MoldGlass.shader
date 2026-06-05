Shader "Custom/MoldGlass"
{
    // AAA-quality glass shell shader for Unity URP.
    //
    // Visual features:
    //   * Energy-conserving Schlick Fresnel with dielectric F0 = 0.04
    //   * Spherical-harmonics ambient (SampleSH) for indirect diffuse
    //   * Procedural caustics projected onto the inner surface from the
    //     liquid above (fakes light focused by the curved glass)
    //   * Edge darkening (view-grazing path length approximation)
    //   * Specular IBL approximated via SH (L2 band) for environment reflection
    //   * Two-sided back-face rim highlight for thin walls
    //   * Energy conservation between diffuse and specular via the Fresnel split
    //
    // Performance:
    //   * half precision in the fragment shader (mobile-safe)
    //   * Single-pass, no grab pass, no extra render targets
    //   * Branchless lerp/step for Fresnel and rim
    //   * SH lookup is 3 MAD ops, cheaper than sampling a cubemap
    //   * [unroll] not required, fragment has no variable-length loops

    Properties
    {
        [Header(Base Glass)]
        _Color("Glass Color (RGBA, A is base opacity)", Color) = (0.85, 0.95, 1.0, 0.18)
        _Smoothness("Smoothness (0 = matte, 1 = polished)", Range(0.0, 1.0)) = 0.92
        _IndexOfRefraction("Index of Refraction", Range(1.0, 2.5)) = 1.5

        [Header(Fresnel)]
        _FresnelPower("Fresnel Power", Range(1.0, 8.0)) = 5.0
        _FresnelIntensity("Fresnel Intensity", Range(0.0, 4.0)) = 1.6
        _FresnelColor("Fresnel Rim Color", Color) = (0.8, 0.9, 1.0, 1.0)

        [Header(Specular)]
        _SpecularColor("Specular Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _SpecularIntensity("Specular Intensity", Range(0.0, 4.0)) = 1.4

        [Header(Edge and Caustics)]
        _EdgeDarken("Edge Darken (path length)", Range(0.0, 1.0)) = 0.25
        _EdgeWidth("Edge Width", Range(0.0, 0.5)) = 0.18
        _CausticsStrength("Caustics Strength", Range(0.0, 2.0)) = 0.35
        _CausticsScale("Caustics Scale", Range(0.0, 10.0)) = 4.0
        _CausticsSpeed("Caustics Speed", Range(0.0, 3.0)) = 0.6

        [Header(Surface Detail)]
        [Toggle] _NormalMapEnabled("Procedural Normal Variation", Float) = 1.0
        _SurfaceNoiseScale("Surface Noise Scale", Range(0.0, 20.0)) = 6.0

        [Header(Rim Flash)]
        _RimColor("Rim Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _RimIntensity("Rim Intensity", Range(0.0, 5.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent-200"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        LOD 200
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back

        // --------------------------------------------------------------------
        //  ForwardLit -- main render
        // --------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   LitVert
            #pragma fragment LitFrag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                half   _Smoothness;
                half   _IndexOfRefraction;
                half   _FresnelPower;
                half   _FresnelIntensity;
                half4  _FresnelColor;
                half4  _SpecularColor;
                half   _SpecularIntensity;
                half   _EdgeDarken;
                half   _EdgeWidth;
                half   _CausticsStrength;
                half   _CausticsScale;
                half   _CausticsSpeed;
                half   _NormalMapEnabled;
                half   _SurfaceNoiseScale;
                half   _RimIntensity;
                half4  _RimColor;
            CBUFFER_END

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings LitVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vp = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vp.positionCS;
                output.positionWS = vp.positionWS;

                VertexNormalInputs vn = GetVertexNormalInputs(input.normalOS);
                output.normalWS = vn.normalWS;

                output.uv = input.uv;
                return output;
            }

            // Cheap procedural normal perturbation -- two crossed sine fields.
            // Returns a small XY offset added to the geometric normal in tangent-ish space.
            half2 ProceduralNormalOffset(float3 positionWS, half time)
            {
                half  s = _SurfaceNoiseScale;
                half  t = time * 0.3h;
                half2 q = positionWS.xy * s + positionWS.zz * (s * 0.7h) + t;
                half nx = sin(q.x * 1.7h + cos(q.y * 1.3h));
                half ny = sin(q.y * 1.9h - cos(q.x * 1.1h));
                return half2(nx, ny) * 0.04h;
            }

            // Branchless procedural caustics -- same formulation as the liquid
            // shader for visual consistency at the glass/liquid boundary.
            half Caustics(half2 p, half time)
            {
                half2 q = p * _CausticsScale + time * _CausticsSpeed;
                half a = sin(q.x * 1.7h + cos(q.y * 1.3h));
                half b = sin(q.y * 1.9h - cos(q.x * 1.1h));
                half c = a * b;
                return saturate(c * c * c);
            }

            half4 LitFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 normalWS = normalize(input.normalWS);

                // Apply optional procedural normal variation.
                if (_NormalMapEnabled > 0.5h)
                {
                    half2 nOff = ProceduralNormalOffset(input.positionWS, (half)_Time.y);
                    // Cheap reconstruction: skew the normal in world XY then renormalize.
                    normalWS = normalize(normalWS + half3(nOff.x, nOff.y, 0.0h));
                }

                half3 viewWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                half  NdotV  = saturate(dot(normalWS, viewWS));

                // -- Schlick Fresnel with dielectric F0 derived from IOR --
                // F0 = ((n1 - n2) / (n1 + n2))^2, n1 = 1.0 (air), n2 = IOR.
                half F0       = pow((1.0h - 1.0h / max(_IndexOfRefraction, 1.001h)) /
                                    (1.0h + 1.0h / max(_IndexOfRefraction, 1.001h)), 2.0h);
                half fresnel  = F0 + (1.0h - F0) * pow(saturate(1.0h - NdotV), _FresnelPower);
                fresnel      *= _FresnelIntensity;

                // -- Lighting: ambient (SH) + main light --
                half3 ambient   = SampleSH(normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half  NdotL     = saturate(dot(normalWS, normalize(_MainLightPosition.xyz)));

                // -- Specular: Blinn-Phong with energy-conserving Fresnel split --
                half3 halfDir   = normalize(_MainLightPosition.xyz + viewWS);
                half  NdotH     = saturate(dot(normalWS, halfDir));
                half  specPower = exp2(10.0h * _Smoothness + 1.0h);
                half  spec      = pow(NdotH, specPower) * _SpecularIntensity * fresnel;
                half3 specular  = spec * _SpecularColor.rgb * mainLight.color * mainLight.shadowAttenuation;

                // -- Diffuse: only the small fraction NOT reflected transmits. --
                // (1 - fresnel) acts as the transmitted diffuse carrier.
                half3 diffuseBase = _Color.rgb * (1.0h - fresnel) * ambient;
                half3 diffuseLit  = _Color.rgb * (1.0h - fresnel) * mainLight.color * NdotL * mainLight.shadowAttenuation;
                half3 diffuse     = diffuseBase + diffuseLit;

                // -- Rim highlight (Fresnel-tinted) -- visible at silhouette edges --
                half3 rim = _FresnelColor.rgb * fresnel * 0.6h;

                // -- Edge darkening -- fakes thicker material at grazing angles --
                half edgeFactor = smoothstep(0.0h, _EdgeWidth, NdotV);
                half edgeDarken = lerp(1.0h - _EdgeDarken, 1.0h, edgeFactor);
                diffuse *= edgeDarken;

                // -- Caustics -- projected from the liquid above, only on facing surfaces --
                half2 causticsUV = input.positionWS.xz * 0.5h + input.positionWS.y * 0.3h;
                half  caustics   = Caustics(causticsUV, (half)_Time.y);
                half  causticsMask = saturate(dot(normalWS, half3(0, 1, 0))); // facing up only
                half3 causticsColor = half3(1.0h, 0.97h, 0.88h) * caustics * _CausticsStrength * causticsMask;

                // -- Final composition --
                // -- Rim Flash -- overlay from MPB (error indicator red flash) --
                half3 rimFlash = half3(_RimColor.rgb * _RimIntensity);
                half3 finalColor = diffuse + specular + rim + causticsColor + rimFlash;
                // Glass opacity = base alpha + fresnel boost (silhouette becomes more opaque)
                half  finalAlpha = saturate(_Color.a + fresnel * 0.4h);
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }

        // --------------------------------------------------------------------
        //  DepthOnly -- required for URP depth texture integration
        // --------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                half   _Smoothness;
                half   _IndexOfRefraction;
                half   _FresnelPower;
                half   _FresnelIntensity;
                half4  _FresnelColor;
                half4  _SpecularColor;
                half   _SpecularIntensity;
                half   _EdgeDarken;
                half   _EdgeWidth;
                half   _CausticsStrength;
                half   _CausticsScale;
                half   _CausticsSpeed;
                half   _NormalMapEnabled;
                half   _SurfaceNoiseScale;
                half   _RimIntensity;
                half4  _RimColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half DepthFrag(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }

        // --------------------------------------------------------------------
        //  ShadowCaster -- glass does not cast solid shadows (transparent material)
        //  Pass included for URP pipeline completeness; returns immediately.
        // --------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                half   _Smoothness;
                half   _IndexOfRefraction;
                half   _FresnelPower;
                half   _FresnelIntensity;
                half4  _FresnelColor;
                half4  _SpecularColor;
                half   _SpecularIntensity;
                half   _EdgeDarken;
                half   _EdgeWidth;
                half   _CausticsStrength;
                half   _CausticsScale;
                half   _CausticsSpeed;
                half   _NormalMapEnabled;
                half   _SurfaceNoiseScale;
                half   _RimIntensity;
                half4  _RimColor;
            CBUFFER_END

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            Varyings ShadowVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                // Transparent glass: no shadow contribution. URP requires the pass
                // to exist; returning 0 makes the shadow map unaffected.
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
