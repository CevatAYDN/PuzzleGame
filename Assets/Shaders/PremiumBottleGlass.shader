Shader "Custom/PremiumBottleGlass"
{
    Properties
    {
        [Header(Glass Appearance)]
        _Color("Glass Tint Color", Color) = (0.95, 0.97, 1.0, 0.15)
        _Smoothness("Smoothness", Range(0, 1)) = 0.98
        _Thickness("Glass Thickness", Range(0.001, 0.1)) = 0.03

        [Header(Refraction)]
        _RefractionIntensity("Refraction Intensity", Range(0, 0.2)) = 0.08
        _IndexOfRefraction("IOR", Range(1.0, 2.5)) = 1.52

        [Header(Fresnel Rim)]
        _FresnelPower("Fresnel Power", Range(0.5, 10)) = 4.0
        _FresnelIntensity("Fresnel Intensity", Range(0, 5)) = 1.5
        _FresnelColor("Fresnel Color", Color) = (1.0, 1.0, 1.0, 1.0)

        [Header(Specular)]
        _SpecularColor("Specular Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _SpecularIntensity("Specular Intensity", Range(0, 5)) = 2.0
        _SpecularSecondary("Secondary Specular", Range(0, 2)) = 0.5

        [Header(Thickness Color)]
        _ThicknessColor("Thickness Tint", Color) = (0.5, 0.7, 1.0, 1.0)
        _ThicknessPower("Thickness Power", Range(0.5, 5)) = 2.0
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
                float4 _Color;
                float _Smoothness;
                float _Thickness;
                float _RefractionIntensity;
                float _IndexOfRefraction;
                float _FresnelPower;
                float _FresnelIntensity;
                float4 _FresnelColor;
                float4 _SpecularColor;
                float _SpecularIntensity;
                float _SpecularSecondary;
                float4 _ThicknessColor;
                float _ThicknessPower;
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

                return output;
            }

            float CalculateFresnel(float3 normalWS, float3 viewDirWS)
            {
                float NdotV = max(0.0, dot(normalWS, viewDirWS));
                return pow(1.0 - NdotV, _FresnelPower);
            }

            float3 CalculateSpecular(float3 normalWS, float3 viewDirWS, float3 lightDirWS, float lightIntensity)
            {
                float3 halfDir = normalize(lightDirWS + viewDirWS);
                float NdotH = max(0.0, dot(normalWS, halfDir));
                
                float specPower = exp2(10.0 * _Smoothness + 1.0);
                float spec = pow(NdotH, specPower);
                float3 specular = spec * _SpecularColor.rgb * _SpecularIntensity * lightIntensity * (specPower + 2.0) / 8.0;
                
                float spec2 = pow(NdotH, specPower * 0.1);
                float3 specular2 = spec2 * _SpecularColor.rgb * _SpecularSecondary * lightIntensity * 0.3;
                
                return specular + specular2;
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

                float3 finalColor = _Color.rgb;
                float finalAlpha = _Color.a;

                float fresnel = CalculateFresnel(normalWS, viewDirWS) * _FresnelIntensity;
                float3 rimColor = _FresnelColor.rgb * fresnel;

                float thickness = pow(1.0 - dot(normalWS, viewDirWS), _ThicknessPower);
                float3 thicknessTint = _ThicknessColor.rgb * thickness * _ThicknessColor.a;

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

                float3 glassColor = lerp(backgroundColor.rgb, finalColor, finalAlpha);
                glassColor += thicknessTint;
                glassColor += rimColor;
                glassColor += specular;

                finalAlpha = saturate(finalAlpha + fresnel * 0.3);

                return half4(glassColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
