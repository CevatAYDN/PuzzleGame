Shader "Custom/LayeredLiquid"
{
    Properties
    {
        [Header(Liquid Colors)]
        _Color1("Color 1 Bottom", Color) = (0.2, 0.6, 1.0, 0.85)
        _Color2("Color 2", Color) = (0.1, 0.5, 0.3, 0.85)
        _Color3("Color 3", Color) = (0.8, 0.2, 0.3, 0.85)
        _Color4("Color 4 Top", Color) = (0.9, 0.7, 0.1, 0.85)

        [Header(Fill Levels 0 to 1)]
        _Fill1("Fill Level 1", Range(0.0, 1.0)) = 0.25
        _Fill2("Fill Level 2", Range(0.0, 1.0)) = 0.50
        _Fill3("Fill Level 3", Range(0.0, 1.0)) = 0.75
        _Fill4("Fill Level 4", Range(0.0, 1.0)) = 1.0

        [Header(Liquid Surface)]
        _BottleHeight("Bottle Mesh Height (object space)", Float) = 2.0
        _SurfaceHeight("Surface Height", Range(0.0, 1.0)) = 1.0
        _SurfaceSmoothness("Surface Edge Smoothness", Range(0.0, 0.05)) = 0.01
        _SurfaceRippleAmplitude("Ripple Amplitude", Range(0.0, 0.1)) = 0.008
        _SurfaceRippleFrequency("Ripple Frequency", Range(0.0, 50.0)) = 20.0
        _SurfaceRippleSpeed("Ripple Speed", Range(0.0, 5.0)) = 1.5

        [Header(Optical Properties)]
        _Transparency("Transparency", Range(0.0, 1.0)) = 0.08
        _EdgeDarken("Edge Darken", Range(0.0, 1.0)) = 0.25
        _EdgeWidth("Edge Width", Range(0.0, 0.5)) = 0.18
        _SpecularIntensity("Specular Intensity", Range(0.0, 2.0)) = 0.7
        _SpecularSmoothness("Specular Smoothness", Range(0.0, 1.0)) = 0.6

        [Header(Layer Boundary)]
        _LayerBoundaryWidth("Layer Boundary Width", Range(0.0, 0.05)) = 0.025
        _LayerBoundaryDarken("Layer Boundary Darken", Range(0.0, 1.0)) = 0.4

        [Header(Wobble Effect)]
        [HideInInspector] _WobbleX ("Wobble X", Range(-1, 1)) = 0.0
        [HideInInspector] _WobbleZ ("Wobble Z", Range(-1, 1)) = 0.0
        _WobbleStrength ("Wobble Strength", Range(0.0, 0.3)) = 0.15

        [Header(Time)]
        _TimeX("Time X", Float) = 0.0
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

            // Mobile-optimized: only essential shadow/light variants
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float4 _Color4;
                float _Fill1;
                float _Fill2;
                float _Fill3;
                float _Fill4;
                float _BottleHeight;
                float _SurfaceHeight;
                float _WobbleX;
                float _WobbleZ;
                float _WobbleStrength;
                float _SurfaceSmoothness;
                float _SurfaceRippleAmplitude;
                float _SurfaceRippleFrequency;
                float _SurfaceRippleSpeed;
                float _Transparency;
                float _EdgeDarken;
                float _EdgeWidth;
                float _SpecularIntensity;
                float _SpecularSmoothness;
                float _LayerBoundaryWidth;
                float _LayerBoundaryDarken;
                float _TimeX;
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

                // Object-space position used for horizontal liquid projection
                output.positionOS = input.positionOS.xyz;

                // Object-space wobble calculation
                output.wobbleY = (input.positionOS.x * _WobbleX + input.positionOS.z * _WobbleZ) * _WobbleStrength;

                return output;
            }

// ═══════════════════════════════════════════════════
            //  Ripple effect (optimized for mobile)
            //  Uses simplified math to reduce GPU load
            // ═════════════════════════════════════════════
            float CalculateRipple(float3 positionWS, float time)
            {
                float angle = atan2(positionWS.z, positionWS.x);
                float radius = length(positionWS.xz);

                // Optimized: Combine waves into single calculation
                float wavePhase = angle * _SurfaceRippleFrequency + radius * 2.5 - time * _SurfaceRippleSpeed;
                float ripple = sin(wavePhase) * _SurfaceRippleAmplitude;

                // Secondary subtle wave for variation
                float ripple2 = sin(angle * _SurfaceRippleFrequency * 0.5 - time * _SurfaceRippleSpeed * 0.8) * _SurfaceRippleAmplitude * 0.3;

                return ripple + ripple2;
            }

            void GetLayerColor(float localY,
                              float4 colors[4], float fills[4],
                              out float4 layerColor,
                              out float layerAlpha)
            {
                // Sharp color per segment (no gradient lerp between layers)
                if (localY <= fills[0])
                {
                    layerColor = colors[0];
                    // Smooth bottom fade (first ~30% of layer)
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
                    // Smooth top fade near surface
                    float distToSurface = fills[3] - localY;
                    if (distToSurface < 0.03)
                        layerAlpha = saturate(distToSurface / 0.03);
                }
                else
                {
                    // Above all liquid — thin fade-out edge
                    layerColor = colors[3];
                    layerAlpha = saturate(1.0 - (localY - fills[3]) / 0.03);
                }
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Calculate world up in object space to keep liquid horizontal as the bottle tilts
                float3x3 worldToObject = (float3x3)GetWorldToObjectMatrix();
                float3 upOS = normalize(mul(worldToObject, float3(0.0, 1.0, 0.0)));

                float bottleHeight = max(_BottleHeight, 0.001);
                float planeScale = bottleHeight * upOS.y;
                float height = dot(input.positionOS, upOS);
                float normalizedY = saturate(height / max(planeScale, 0.0001));

                float time = _TimeX > 0.0 ? _TimeX : _Time.y;
                float surfaceRipple = CalculateRipple(input.positionWS, time);

                float wobbleAdjustment = input.wobbleY;
                float adjustedNormalizedY = normalizedY + wobbleAdjustment;

                // Fix edge artifacts: use smooth transition instead of hard clip
                float surfaceDist = _SurfaceHeight + surfaceRipple + wobbleAdjustment - normalizedY;
                if (surfaceDist < -0.002) discard;
                
                // Soft edge fade for anti-aliasing
                float edgeSoftness = 0.002;
                float surfaceAlpha = smoothstep(-edgeSoftness, edgeSoftness, surfaceDist);

                float4 colors[4] = { _Color1, _Color2, _Color3, _Color4 };
                float fills[4] = { _Fill1, _Fill2, _Fill3, _Fill4 };

                float4 layerColor;
                float layerAlpha;
                GetLayerColor(normalizedY, colors, fills, layerColor, layerAlpha);

                // Layer boundary lines — thin dark lines between color transitions
                float boundaryFactor = 1.0;
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    float distToBoundary = abs(adjustedNormalizedY - fills[i]);
                    if (distToBoundary < _LayerBoundaryWidth)
                    {
                        float t = distToBoundary / _LayerBoundaryWidth;
                        // Smooth falloff: darkest at exact boundary
                        boundaryFactor = min(boundaryFactor, lerp(1.0 - _LayerBoundaryDarken, 1.0, t * t));
                    }
                }

                float3 viewDirWS = GetWorldSpaceViewDir(input.positionWS);
                float3 viewDir = normalize(viewDirWS);
                float3 normalWS = normalize(input.normalWS);

                float NdotV = max(0.0, dot(normalWS, -viewDir));
                float edgeFactor = smoothstep(0.0, _EdgeWidth, NdotV);
                float edgeDarken = lerp(1.0 - _EdgeDarken, 1.0, edgeFactor);

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotH = max(0.0, dot(normalWS, halfDir));
                float specular = pow(NdotH, _SpecularSmoothness * 128.0) * _SpecularIntensity;

                // Surface highlight — brighter and wider for visual pop
                float surfaceProximity = 1.0 - saturate((_SurfaceHeight + surfaceRipple - normalizedY) / 0.03);
                float surfaceHighlight = pow(surfaceProximity, 3.0) * 0.7;

                float3 finalColor = layerColor.rgb * boundaryFactor * edgeDarken;

                finalColor += specular * mainLight.color;
                finalColor += surfaceHighlight * mainLight.color;

                float finalAlpha = layerColor.a * layerAlpha * (1.0 - _Transparency) * surfaceAlpha;
                finalAlpha = saturate(finalAlpha);

                return half4(finalColor, finalAlpha);
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

        // ═══════════════════════════════════════════════════════
        //  Shadow Caster Pass — Liquid casts shadows
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
                float _Fill1;
                float _Fill2;
                float _Fill3;
                float _Fill4;
                float _BottleHeight;
                float _SurfaceHeight;
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
                float objectY : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.objectY = input.positionOS.y;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Only cast shadows where there is liquid
                float bottleHeight = max(_BottleHeight, 0.001);
                float normalizedY = saturate(input.objectY / bottleHeight);

                // Discard pixels above the liquid surface
                clip(_SurfaceHeight - normalizedY);

                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}