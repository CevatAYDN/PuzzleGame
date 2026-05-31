Shader "Custom/BottleGlass"
{
    Properties
    {
        [Header(Glass Appearance)]
        _Color("Glass Tint Color", Color) = (1,1,1,0)
        _Smoothness("Smoothness", Range(0,1)) = 0.98

        [Header(Fresnel Rim)]
        _FresnelPower("Fresnel Power", Range(0.5,8)) = 5
        _FresnelIntensity("Fresnel Intensity", Range(0,5)) = 2
        _FresnelColor("Fresnel Color", Color) = (1,1,1,0.5)

        [Header(Specular)]
        _SpecularColor("Specular Color", Color) = (1,1,1,1)
        _SpecularIntensity("Specular Intensity", Range(0,5)) = 3
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
        Cull Back

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

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Smoothness;
                float _FresnelPower;
                float _FresnelIntensity;
                float4 _FresnelColor;
                float4 _SpecularColor;
                float _SpecularIntensity;
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

            float CalculateFresnel(float3 normalWS, float3 viewDirWS)
            {
                float NdotV = max(0, dot(normalWS, viewDirWS));
                return pow(abs(1 - NdotV), _FresnelPower);
            }

            float3 CalculateSpecular(float3 normalWS, float3 viewDirWS, float3 lightDirWS, float lightIntensity)
            {
                float3 halfDir = normalize(lightDirWS + viewDirWS);
                float NdotH = max(0, dot(normalWS, halfDir));
                float specPower = exp2(10 * _Smoothness + 1);
                float spec = pow(NdotH, specPower);
                // energy conservation approximation
                return spec * _SpecularColor.rgb * _SpecularIntensity * lightIntensity * (specPower + 2) / 8;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 normalWS = normalize(input.normalWS);

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float lightIntensity = mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                float3 finalColor = _Color.rgb;
                float finalAlpha = _Color.a;

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

                float fresnel = CalculateFresnel(normalWS, viewDirWS) * _FresnelIntensity;
                float3 rimColor = _FresnelColor.rgb * fresnel;

                finalColor += specular + rimColor;
                finalAlpha = saturate(finalAlpha + fresnel + dot(specular, float3(0.2126,0.7152,0.0722)));

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}