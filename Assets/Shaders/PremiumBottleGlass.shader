Shader "Custom/PremiumBottleGlass"
{
    Properties
    {
        [Header(Glass Outline Style)]
        _OutlineColor("Outline Color", Color) = (0.0, 0.4, 1.0, 1.0)
        _OutlineWidth("Outline Width", Range(0.001, 0.02)) = 0.008
        _InnerLineColor("Inner Line Color", Color) = (0.9, 0.95, 1.0, 1.0)
        _InnerLineWidth("Inner Line Width", Range(0.0005, 0.01)) = 0.002

        [Header(Glass Appearance)]
        _Color("Glass Tint Color", Color) = (0.9, 0.95, 1.0, 0.2)
        _GlassAlpha("Glass Alpha", Range(0.0, 1.0)) = 0.15

        [Header(Refraction)]
        _RefractionIntensity("Refraction Intensity", Range(0, 0.2)) = 0.08
        _IndexOfRefraction("IOR", Range(1.0, 2.5)) = 1.5

        [Header(Highlights)]
        _SpecularColor("Specular Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _SpecularIntensity("Specular Intensity", Range(0, 5)) = 2.0
        _SpecularSmoothness("Specular Smoothness", Range(0, 1)) = 0.9
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 200
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        // Main Glass Pass
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float4 _Color;
                float _GlassAlpha;
                float _RefractionIntensity;
                float _IndexOfRefraction;
                float4 _SpecularColor;
                float _SpecularIntensity;
                float _SpecularSmoothness;
                float4 _InnerLineColor;
                float _InnerLineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPosition : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float3 positionOS : TEXCOORD5;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;

                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.screenPosition = ComputeScreenPos(output.positionCS);
                output.uv = input.uv;
                output.positionOS = input.positionOS.xyz;

                return output;
            }

            float3 CalculateSpecular(float3 normalWS, float3 viewDirWS, float3 lightDirWS, float lightIntensity)
            {
                float3 halfDir = normalize(lightDirWS + viewDirWS);
                float NdotH = max(0.0, dot(normalWS, halfDir));

                float specPower = exp2(10.0 * _SpecularSmoothness + 1.0);
                float spec = pow(NdotH, specPower);
                float3 specular = spec * _SpecularColor.rgb * _SpecularIntensity * lightIntensity * (specPower + 2.0) / 8.0;

                return specular;
            }

            half4 frag(Varyings input, half facing : VFACE) : SV_Target
            {
                float3 viewDirWS = normalize(input.viewDirWS);
                float3 normalWS = normalize(input.normalWS);

                if (facing < 0)
                {
                    normalWS = -normalWS;
                }

                float2 screenUV = input.screenPosition.xy / input.screenPosition.w;

                float3 refractedDir = refract(-viewDirWS, normalWS, 1.0 / _IndexOfRefraction);
                float2 refractedUV = screenUV + refractedDir.xy * _RefractionIntensity;

                float4 backgroundColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, refractedUV);

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float lightIntensity = mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                float3 glassColor = _Color.rgb;
                float glassAlpha = _GlassAlpha;

                // Inner white highlight line
                float NdotV = dot(normalWS, viewDirWS);
                float innerLine = smoothstep(_InnerLineWidth, 0.0, abs(NdotV - 0.7));
                innerLine *= smoothstep(0.65, 0.75, NdotV);
                float3 innerLineColor = _InnerLineColor.rgb * innerLine * 0.8;

                // Blue outline on edges
                float outline = smoothstep(0.3 + _OutlineWidth, 0.3, NdotV);
                float3 outlineColor = _OutlineColor.rgb * outline;

                float3 specular = CalculateSpecular(normalWS, viewDirWS, lightDir, lightIntensity);

#if defined(_ADDITIONAL_LIGHTS)
                uint additionalLightsCount = GetAdditionalLightsCount();
                for (uint i = 0; i < additionalLightsCount; ++i)
                {
                    Light light = GetAdditionalLight(i, input.positionWS);
                    float3 addLightDir = normalize(light.direction);
                    float addIntensity = light.distanceAttenuation * light.shadowAttenuation;
                    specular += CalculateSpecular(normalWS, viewDirWS, addLightDir, addIntensity);
                }
#endif

                float3 finalColor = lerp(backgroundColor.rgb, glassColor, glassAlpha);
                finalColor += specular;
                finalColor += innerLineColor;
                finalColor += outlineColor;

                float finalAlpha = saturate(glassAlpha + outline);

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
