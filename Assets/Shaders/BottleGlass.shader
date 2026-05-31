Shader "Custom/BottleGlass"
{
    Properties
    {
        [Header(Glass Appearance)]
        _Color("Glass Tint Color", Color) = (1, 1, 1, 0.1)
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.9
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0

        [Header(Fresnel Rim)]
        _FresnelPower("Fresnel Power", Range(0.5, 8.0)) = 3.0
        _FresnelIntensity("Fresnel Intensity", Range(0.0, 2.0)) = 0.6
        _FresnelColor("Fresnel Color", Color) = (1, 1, 1, 0.8)

        [Header(Specular)]
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularIntensity("Specular Intensity", Range(0.0, 2.0)) = 0.8

        [Header(Refraction)]
        _RefractionStrength("Refraction Strength", Range(0.0, 0.2)) = 0.05
        _RefractionScale("Refraction Scale", Range(0.5, 3.0)) = 1.0

        [Header(Alpha)]
        _AlphaClip("Alpha Clip Threshold", Range(0.0, 1.0)) = 0.01
        _AlphaToCoverage("Alpha to Coverage", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        LOD 200
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        AlphaToMask [_AlphaToCoverage]

        // ═══════════════════════════════════════════════════════
        //  Forward Lit Pass — Main rendering
        // ═══════════════════════════════════════════════════════
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Multi-compile for mobile feature levels
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // ═══════════════════════════════════════════════════
            //  CBUFFER — SRP Batcher compatible
            // ═══════════════════════════════════════════════════
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Smoothness;
                float _Metallic;
                float _FresnelPower;
                float _FresnelIntensity;
                float4 _FresnelColor;
                float4 _SpecularColor;
                float _SpecularIntensity;
                float _RefractionStrength;
                float _RefractionScale;
                float _AlphaClip;
                float _AlphaToCoverage;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;

                return output;
            }

            // ═══════════════════════════════════════════════════
            //  Blinn-Phong specular for mobile efficiency
            // ═══════════════════════════════════════════════════
            float3 CalculateSpecular(
                float3 normalWS,
                float3 viewDirWS,
                float3 lightDirWS,
                float lightIntensity)
            {
                float3 halfDir = normalize(lightDirWS + viewDirWS);
                float NdotH = max(0.0, dot(normalWS, halfDir));
                float smoothnessFactor = _Smoothness * 100.0 + 1.0;
                float spec = pow(NdotH, smoothnessFactor);
                return spec * _SpecularColor.rgb * _SpecularIntensity * lightIntensity;
            }

            // ═══════════════════════════════════════════════════
            //  Schlick Fresnel approximation
            // ═══════════════════════════════════════════════════
            float CalculateFresnel(float3 normalWS, float3 viewDirWS)
            {
                float NdotV = max(0.0, dot(normalWS, viewDirWS));
                return pow(abs(1.0 - NdotV), _FresnelPower) * _FresnelIntensity;
            }

            // ═══════════════════════════════════════════════════
            //  Simple refraction via normal distortion
            //  Works without CameraOpaqueTexture for mobile
            // ══════════════════════════════════════════════════
            float3 CalculateSimpleRefraction(float3 normalWS)
            {
                // Distort based on normal direction for glass-like refraction
                float3 refractDir = normalWS * _RefractionStrength * _RefractionScale;
                return refractDir;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // View direction (world space)
                float3 viewDirWS = GetWorldSpaceViewDir(input.positionWS);
                float3 viewDir = normalize(viewDirWS);

                // Normal
                float3 normalWS = normalize(input.normalWS);

                // ═══════════════════════════════════════════
                //  Fresnel rim
                // ═══════════════════════════════════════════
                float fresnel = CalculateFresnel(normalWS, viewDir);

                // ═══════════════════════════════════════════
                //  Main light contribution
                // ═══════════════════════════════════════════
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float lightIntensity = mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                // Diffuse
                float NdotL = max(0.0, dot(normalWS, lightDir));
                float3 diffuse = _Color.rgb * NdotL * mainLight.color * lightIntensity;

                // Specular
                float3 specular = CalculateSpecular(normalWS, viewDir, lightDir, lightIntensity);

                // ═══════════════════════════════════════════
                //  Additional lights (up to 4, per URP mobile settings)
                // ═══════════════════════════════════════════
                #if defined(_ADDITIONAL_LIGHTS)
                    uint additionalLightsCount = GetAdditionalLightsCount();
                    for (uint i = 0u; i < additionalLightsCount; i++)
                    {
                        Light light = GetAdditionalLight(i, input.positionWS);
                        float3 addLightDir = normalize(light.direction);
                        float addIntensity = light.distanceAttenuation * light.shadowAttenuation;
                        float addNdotL = max(0.0, dot(normalWS, addLightDir));
                        diffuse += _Color.rgb * addNdotL * light.color * addIntensity * 0.5;
                        specular += CalculateSpecular(normalWS, viewDir, addLightDir, addIntensity) * 0.5;
                    }
                #endif

                // ══════════════════════════════════════════
                //  Refraction (subtle color shift)
                // ═══════════════════════════════════════════
                float3 refraction = CalculateSimpleRefraction(normalWS);

                // ═══════════════════════════════════════════
                //  Combine final color
                // ═══════════════════════════════════════════
                float3 finalColor = diffuse + specular;

                // Add fresnel rim color on top
                float3 rimColor = _FresnelColor.rgb * fresnel * mainLight.color;
                finalColor += rimColor;

                // Subtle refraction tint
                finalColor += refraction * 0.1;

                // ═══════════════════════════════════════════
                //  Alpha calculation
                // ═══════════════════════════════════════════
                float alpha = _Color.a;
                // Increase alpha at edges for glass-like look
                alpha = lerp(alpha, 1.0, fresnel * 0.5);
                alpha = saturate(alpha);

                // Clip near-transparent pixels
                clip(alpha - _AlphaClip);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }

        // ═══════════════════════════════════════════════════════
        //  Shadow Caster Pass — Bottles cast proper shadows
        // ═══════════════════════════════════════════════════════
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _AlphaClip;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ═══════════════════════════════════════════════════════
        //  Depth Only Pass — For proper transparency sorting
        // ═══════════════════════════════════════════════════════
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                return output;
            }

            half frag(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}