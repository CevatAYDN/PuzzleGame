Shader "Custom/BottleGlass"
{
    Properties
    {
        [Header(Glass Appearance)]
        _Color("Glass Tint Color", Color) = (1, 1, 1, 0.15)
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.95

                [Header(Fresnel Rim)]
        _FresnelPower("Fresnel Power", Range(0.5, 8.0)) = 4.0
        _FresnelIntensity("Fresnel Intensity", Range(0.0, 2.0)) = 0.8
        _FresnelColor("Fresnel Color", Color) = (1, 1, 1, 0.8)

        [Header(Specular)]
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularIntensity("Specular Intensity", Range(0.0, 2.0)) = 1.0

        [Header(Glow Effect)]
        _GlowIntensity("Glow Intensity", Range(0.0, 2.0)) = 0.4
        _GlowColor("Glow Color", Color) = (0.8, 0.9, 1.0, 1.0)
        _GlowPower("Glow Power", Range(1.0, 8.0)) = 3.0

        [Header(Refraction)]
        _RefractionStrength("Refraction Strength", Range(0.0, 0.2)) = 0.07
        _RefractionScale("Refraction Scale", Range(0.5, 3.0)) = 1.2

        [Header(Alpha)]
        _AlphaClip("Alpha Clip Threshold", Range(0.0, 1.0)) = 0.005
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

            // Mobile-optimized: only essential shadow/light variants
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // ═════════════════════════════════════════════════════
            //  CBUFFER — SRP Batcher compatible
            // ════════════════════════════════════════════════════
                        CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Smoothness;
                float _FresnelPower;
                float _FresnelIntensity;
                float4 _FresnelColor;
                float4 _SpecularColor;
                float _SpecularIntensity;
                float _GlowIntensity;
                float4 _GlowColor;
                float _GlowPower;
                float _RefractionStrength;
                float _RefractionScale;
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

            // ════════════════════════════════════════════════════
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

                        // ═══════════════════════════════════════════
            //  Schlick Fresnel approximation
            // ══════════════════════════════════════════════════
            float CalculateFresnel(float3 normalWS, float3 viewDirWS)
            {
                float NdotV = max(0.0, dot(normalWS, viewDirWS));
                return pow(abs(1.0 - NdotV), _FresnelPower) * _FresnelIntensity;
            }

            // ══════════════════════════════════════════════════
            //  Glow effect based on view angle
            // ════════════════════════════════════════════════
            float CalculateGlow(float3 normalWS, float3 viewDirWS)
            {
                float NdotV = max(0.0, dot(normalWS, viewDirWS));
                float glowBase = pow(1.0 - NdotV, _GlowPower);
                return glowBase * _GlowIntensity;
            }

            // ═════════════════════════════════════════════════
            //  Simple refraction via normal distortion
            //  Works without CameraOpaqueTexture for mobile
            // ════════════════════════════════════════════════
            float3 CalculateSimpleRefraction(float3 normalWS)
            {
                // Distort based on normal direction for glass-like refraction
                float3 refractDir = normalWS * _RefractionStrength * _RefractionScale;
                // Add subtle normal perturbation for realism
                float3 perturb = normalize(normalWS + sin(_Time.y + normalWS.x * 10.0 + normalWS.y * 13.0) * 0.003);
                return refractDir + perturb * 0.02;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // View direction (world space)
                float3 viewDirWS = GetWorldSpaceViewDir(input.positionWS);
                float3 viewDir = normalize(viewDirWS);

                // Normal
                float3 normalWS = normalize(input.normalWS);

                // ════════════════════════════════════════════
                //  Fresnel rim
                // ════════════════════════════════════════════
                float fresnel = CalculateFresnel(normalWS, viewDir);
                
                // ════════════════════════════════════════════
                //  Glow effect
                // ══════════════════════════════════════════
                float glow = CalculateGlow(normalWS, viewDir);

                // ══════════════════════════════════════════
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

                // ════════════════════════════════════════════
                //  Additional lights (up to 4, per URP mobile settings)
                // ════════════════════════════════════════════
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

                // ═══════════════════════════════════════════
                //  Refraction (subtle color shift)
                // ═══════════════════════════════════════════
                float3 refraction = CalculateSimpleRefraction(normalWS);

                // ═════════════════════════════════════════
                //  Combine final color
                // ═════════════════════════════════════════
                float3 finalColor = diffuse + specular;

                // Add fresnel rim color on top
                float3 rimColor = _FresnelColor.rgb * fresnel * mainLight.color;
                finalColor += rimColor;
                
                // Add glow effect
                float3 glowColor = _GlowColor.rgb * glow;
                finalColor += glowColor;

                // Subtle refraction tint
                finalColor += refraction * 0.1;

                // ══════════════════════════════════════════
                //  Alpha calculation
                // ═════════════════════════════════════════
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
                float4 _Color;
                float _FresnelPower;
                float _FresnelIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
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

            half4 frag(Varyings input) : SV_Target
            {
                // Only cast shadows where there is glass
                clip(input.positionWS.y - (_Color.a * 2.0)); // rough approximation
                return 0;
            }
            ENDHLSL
        }

        // ═══════════════════════════════════════════════════════
        //  Depth Only Pass
        // ══════════════════════════════════════════════════════
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