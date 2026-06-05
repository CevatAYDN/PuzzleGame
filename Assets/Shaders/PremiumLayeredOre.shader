Shader "Custom/PremiumLayeredOre"
{
    Properties
    {
        [Header(Ore Colors)]
        _Color1("Color 1 Bottom", Color) = (0.15, 0.55, 0.95, 0.98)
        _Color2("Color 2", Color) = (0.08, 0.65, 0.35, 0.98)
        _Color3("Color 3", Color) = (0.95, 0.18, 0.28, 0.98)
        _Color4("Color 4 Top", Color) = (0.98, 0.72, 0.05, 0.98)

        [Header(Fill Levels 0 to 1)]
        _Fill1("Fill Level 1", Range(0.0, 1.0)) = 0.25
        _Fill2("Fill Level 2", Range(0.0, 1.0)) = 0.50
        _Fill3("Fill Level 3", Range(0.0, 1.0)) = 0.75
        _Fill4("Fill Level 4", Range(0.0, 1.0)) = 1.0

        [Header(Magma Emission and Crust)]
        [HDR] _EmissionColorMultiplier("Global Emission Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        _EmissionIntensity("Emission Intensity", Range(0.0, 10.0)) = 2.5
        _CrustColor("Crust Color", Color) = (0.1, 0.05, 0.02, 1.0)
        _CrustScale("Crust Scale", Range(1.0, 50.0)) = 15.0
        _CrustThreshold("Crust Coverage", Range(0.0, 1.0)) = 0.4

        [Header(Heat Distortion)]
        _HeatDistortion("Distortion Amount", Range(0.0, 0.2)) = 0.05
        _HeatSpeed("Distortion Speed", Range(0.0, 5.0)) = 1.5

        [Header(Mold Properties)]
        _BottleHeight("Mold Mesh Height (object space)", Float) = 2.0
        _Radius("Mold Radius (object space)", Float) = 0.4
        _SurfaceHeight("Surface Height", Range(0.0, 1.0)) = 1.0

        [Header(Wobble Effect)]
        [HideInInspector] _WobbleX("Wobble X", Range(-1, 1)) = 0.0
        [HideInInspector] _WobbleZ("Wobble Z", Range(-1, 1)) = 0.0
        _WobbleStrength("Wobble Strength", Range(0.0, 0.3)) = 0.1

        [Header(Ore Surface)]
        _SurfaceSmoothness("Surface Edge Smoothness", Range(0.0, 0.05)) = 0.008
        _SurfaceRippleAmplitude("Ripple Amplitude", Range(0.0, 0.1)) = 0.004
        _SurfaceRippleFrequency("Ripple Frequency", Range(0.0, 50.0)) = 12.0
        _SurfaceRippleSpeed("Ripple Speed", Range(0.0, 5.0)) = 0.8

        [Header(Surface Highlight)]
        _HighlightColor("Highlight Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _HighlightIntensity("Highlight Intensity", Range(0.0, 3.0)) = 1.5
        _HighlightWidth("Highlight Width", Range(0.0, 0.5)) = 0.15

        [Header(Specular and Sparkle)]
        _SpecularColor("Specular Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _SpecularIntensity("Specular Intensity", Range(0.0, 3.0)) = 1.2
        _SpecularSmoothness("Specular Smoothness", Range(0.0, 1.0)) = 0.95
        _SparkleIntensity("Sparkle Intensity", Range(0.0, 2.0)) = 0.1
        _SparkleSize("Sparkle Size", Range(1.0, 32.0)) = 12.0

        [Header(Optical Properties)]
        _Transparency("Transparency", Range(0.0, 1.0)) = 0.02
        _EdgeDarken("Edge Darken", Range(0.0, 1.0)) = 0.1
        _EdgeWidth("Edge Width", Range(0.0, 0.5)) = 0.25

        [Header(Optical Properties)]
        _Transparency("Transparency", Range(0.0, 1.0)) = 0.02
        _EdgeDarken("Edge Darken", Range(0.0, 1.0)) = 0.1
        _EdgeWidth("Edge Width", Range(0.0, 0.5)) = 0.25

        [Header(Layer Boundary)]
        _LayerBoundaryWidth("Layer Boundary Width", Range(0.0, 0.05)) = 0.012
        _LayerBoundaryDarken("Layer Boundary Darken", Range(0.0, 1.0)) = 0.15

        [Header(Rim Flash)]
        _RimColor("Rim Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _RimIntensity("Rim Intensity", Range(0.0, 5.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent-100"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        LOD 100
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
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float4 _Color4;
                float _Fill1;
                float _Fill2;
                float _Fill3;
                float _Fill4;

                float4 _EmissionColorMultiplier;
                float _EmissionIntensity;
                float4 _CrustColor;
                float _CrustScale;
                float _CrustThreshold;
                float _HeatDistortion;
                float _HeatSpeed;

                float _BottleHeight;
                float _SurfaceHeight;
                float _WobbleX;
                float _WobbleZ;
                float _WobbleStrength;
                float _SurfaceSmoothness;
                float _SurfaceRippleAmplitude;
                float _SurfaceRippleFrequency;
                float _SurfaceRippleSpeed;

                float4 _HighlightColor;
                float _HighlightIntensity;
                float _HighlightWidth;
                float4 _SpecularColor;
                float _SpecularIntensity;
                float _SpecularSmoothness;
                float _SparkleIntensity;
                float _SparkleSize;
                float _Transparency;
                float _EdgeDarken;
                float _EdgeWidth;
                float _LayerBoundaryWidth;
                float _LayerBoundaryDarken;
                float _Radius;
                float _RimIntensity;
                float4 _RimColor;
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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                float wobbleY : TEXCOORD4;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;

                output.uv = input.uv;
                output.positionOS = input.positionOS.xyz;

                // Object-space wobble calculation
                output.wobbleY = (input.positionOS.x * _WobbleX + input.positionOS.z * _WobbleZ) * _WobbleStrength;

                return output;
            }

            float CalculateRipple(float3 positionWS, float time)
            {
                float angle = atan2(positionWS.z, positionWS.x);
                float radius = length(positionWS.xz);

                float wavePhase = angle * _SurfaceRippleFrequency + radius * 2.5 - time * _SurfaceRippleSpeed;
                float ripple = sin(wavePhase) * _SurfaceRippleAmplitude;

                float ripple2 = sin(angle * _SurfaceRippleFrequency * 0.5 - time * _SurfaceRippleSpeed * 0.8) * _SurfaceRippleAmplitude * 0.3;

                return ripple + ripple2;
            }

            void GetLayerColor(float localY, float4 colors[4], float fills[4], out float4 layerColor, out float layerAlpha)
            {
                if (localY <= fills[0])
                {
                    layerColor = colors[0];
                    layerAlpha = saturate(localY / max(fills[0] * 0.3, 0.001));
                }
                else if (localY <= fills[1])
                {
                    layerColor = colors[1];
                    layerAlpha = 1.0;
                }
                else if (localY <= fills[2])
                {
                    layerColor = colors[2];
                    layerAlpha = 1.0;
                }
                else if (localY <= fills[3])
                {
                    layerColor = colors[3];
                    layerAlpha = 1.0;
                    float distToSurface = fills[3] - localY;
                    if (distToSurface < 0.03)
                        layerAlpha = saturate(distToSurface / 0.03);
                }
                else
                {
                    layerColor = colors[3];
                    layerAlpha = saturate(1.0 - (localY - fills[3]) / 0.03);
                }
            }

            // Pseudo-random noise for crust
            float hash(float2 p)
            {
                p = 50.0 * frac(p * 0.3183099 + float2(0.71, 0.113));
                return -1.0 + 2.0 * frac(p.x * p.y * (p.x + p.y));
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i + float2(0.0, 0.0)), hash(i + float2(1.0, 0.0)), u.x),
                            lerp(hash(i + float2(0.0, 1.0)), hash(i + float2(1.0, 1.0)), u.x), u.y);
            }

            half4 frag(Varyings input, half facing : VFACE) : SV_Target
            {
                float3x3 worldToObject = (float3x3)GetWorldToObjectMatrix();
                float3 worldUpOS = normalize(mul(worldToObject, float3(0.0, 1.0, 0.0)));
                float3 localUpOS = float3(0.0, 1.0, 0.0);
                float blend = clamp(worldUpOS.y, 0.35, 1.0);
                float3 upOS = normalize(lerp(localUpOS, worldUpOS, blend));

                // Bounding box height calculation along upOS to normalize liquid height
                float horizontalLength = length(upOS.xz);
                float maxHorizontal = _Radius * horizontalLength;
                float minHorizontal = -maxHorizontal;
                float maxVertical = max(0.0, _BottleHeight * upOS.y);
                float minVertical = min(0.0, _BottleHeight * upOS.y);

                float minH = minHorizontal + minVertical;
                float maxH = maxHorizontal + maxVertical;

                float height = dot(input.positionOS, upOS);
                float normalizedY = saturate((height - minH) / max(maxH - minH, 0.0001));

                float time = _Time.y;
                
                // Add heat distortion to position for noise and ripples
                float distortion = noise(input.positionWS.xz * 10.0 + time * _HeatSpeed) * _HeatDistortion;
                float3 distortedPosWS = input.positionWS + float3(distortion, 0, distortion);

                float surfaceRipple = CalculateRipple(distortedPosWS, time);

                float wobbleAdjustment = input.wobbleY;
                float adjustedNormalizedY = normalizedY + wobbleAdjustment;

                float surfaceDist = _SurfaceHeight + surfaceRipple + wobbleAdjustment - normalizedY;
                if (surfaceDist < -0.002) discard;

                float edgeSoftness = 0.002;
                float surfaceAlpha = smoothstep(-edgeSoftness, edgeSoftness, surfaceDist);

                float4 colors[4] = { _Color1, _Color2, _Color3, _Color4 };
                float fills[4] = { _Fill1, _Fill2, _Fill3, _Fill4 };

                float4 layerColor;
                float layerAlpha;
                GetLayerColor(normalizedY, colors, fills, layerColor, layerAlpha);

                float boundaryFactor = 1.0;
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    float distToBoundary = abs(adjustedNormalizedY - fills[i]);
                    if (distToBoundary < _LayerBoundaryWidth)
                    {
                        float t = distToBoundary / _LayerBoundaryWidth;
                        boundaryFactor = min(boundaryFactor, lerp(1.0 - _LayerBoundaryDarken, 1.0, t * t));
                    }
                }

                float3 viewDirWS = GetWorldSpaceViewDir(input.positionWS);
                float3 viewDir = normalize(viewDirWS);
                float3 normalWS = normalize(input.normalWS);

                float NdotV = max(0.0, dot(normalWS, viewDir));

                // Bright surface highlight
                float surfaceProximity = 1.0 - saturate((_SurfaceHeight + surfaceRipple - normalizedY) / 0.03);
                
                // Magma Emission
                float3 emission = layerColor.rgb * _EmissionColorMultiplier.rgb * _EmissionIntensity;

                // Crust Calculation
                float n = noise(distortedPosWS.xz * _CrustScale + time * 0.2) * 0.5 + 0.5;
                float crustFactor = smoothstep(_CrustThreshold - 0.1, _CrustThreshold + 0.1, n) * surfaceProximity;
                
                float3 crustColor = lerp(emission, _CrustColor.rgb, crustFactor);

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotH = max(0.0, dot(normalWS, halfDir));
                float specular = pow(NdotH, _SpecularSmoothness * 256.0) * _SpecularIntensity;
                float3 specularColor = specular * _SpecularColor.rgb * mainLight.color;

                // -- Sparkle Effect ------------------------------------------
                float2 screenUV = input.positionCS.xy * _ScreenParams.zw;
                float sparkleHash = hash(screenUV * _SparkleSize + floor(time * 4.7));
                float sparkleGate = smoothstep(0.92, 1.0, sparkleHash)
                                  * smoothstep(0.0, 0.06, abs(sparkleHash - 0.96));
                float sparkleAngle = pow(NdotH, _SpecularSmoothness * 512.0);
                float sparkle = sparkleGate * sparkleAngle * _SparkleIntensity
                              * saturate(NdotV - 0.3) * 5.0;
                float3 sparkleColor = sparkle * _SpecularColor.rgb * mainLight.color * 3.0;
                specularColor += sparkleColor;

                // Rim Light & Volume Shadow for cylindrical feel
                float rim = 1.0 - NdotV;
                float rimIntensity = smoothstep(0.6, 1.0, rim);
                float3 rimColor = layerColor.rgb * rimIntensity * 2.0;
                
                float volumeShadow = smoothstep(0.3, 1.0, rim);
                crustColor = lerp(crustColor, crustColor * 0.4, volumeShadow * 0.8);

                float3 finalColor = crustColor * boundaryFactor;

                finalColor += specularColor;
                finalColor += rimColor;

                // Rim Flash -- overlay from MPB (error indicator / pour highlight)
                finalColor += _RimColor.rgb * _RimIntensity * 0.5f;

                float finalAlpha = layerColor.a * layerAlpha * (1.0 - _Transparency) * surfaceAlpha;
                finalAlpha = saturate(finalAlpha);

                float4 topColor = _Color4;
                topColor.rgb = lerp(topColor.rgb * _EmissionIntensity, _CrustColor.rgb, crustFactor);
                
                return facing > 0 ? half4(finalColor, finalAlpha) : half4(topColor.rgb, finalAlpha);
            }
            ENDHLSL
        }

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

            CBUFFER_START(UnityPerMaterial)
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float4 _Color4;
                float _Fill1;
                float _Fill2;
                float _Fill3;
                float _Fill4;

                float4 _EmissionColorMultiplier;
                float _EmissionIntensity;
                float4 _CrustColor;
                float _CrustScale;
                float _CrustThreshold;
                float _HeatDistortion;
                float _HeatSpeed;

                float _BottleHeight;
                float _SurfaceHeight;
                float _WobbleX;
                float _WobbleZ;
                float _WobbleStrength;
                float _SurfaceSmoothness;
                float _SurfaceRippleAmplitude;
                float _SurfaceRippleFrequency;
                float _SurfaceRippleSpeed;

                float4 _HighlightColor;
                float _HighlightIntensity;
                float _HighlightWidth;
                float4 _SpecularColor;
                float _SpecularIntensity;
                float _SpecularSmoothness;
                float _SparkleIntensity;
                float _SparkleSize;
                float _Transparency;
                float _EdgeDarken;
                float _EdgeWidth;
                float _LayerBoundaryWidth;
                float _LayerBoundaryDarken;
                float _Radius;
                float _RimIntensity;
                float4 _RimColor;
            CBUFFER_END

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
    Fallback Off
}
