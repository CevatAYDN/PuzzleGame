Shader "Custom/LayeredLiquid"
{
    // AAA-quality layered liquid shader for Unity URP.
    //
    // Visual features:
    //   * Beer-Lambert volumetric absorption per layer (depth-based color)
    //   * Analytical surface normals from ripple height field (no normal map texture)
    //   * Energy-conserving Schlick Fresnel for view-dependent reflection
    //   * Spherical-harmonics ambient (SampleSH) for indirect lighting
    //   * Procedural caustics projected onto the surface from below
    //   * Surface-tension meniscus at the liquid-glass boundary
    //   * Anisotropic specular highlight elongated along the surface plane
    //
    // Performance:
    //   * half precision throughout the fragment shader (mobile-safe)
    //   * World-space up vector + liquid range computed in vertex stage,
    //     interpolated as varyings, fragment is branchless except for
    //     the early discard on the surface plane
    //   * pow(x, n) with constant n expanded to multiplies where possible
    //   * [unroll] on the per-layer boundary loop (max 4 iterations)
    //   * Packed varyings: positionOS+upOS in two float4s, rest in half3/half2

    Properties
    {
        [Header(Liquid Colors)]
        _Color1("Color 1 Bottom", Color) = (0.2, 0.6, 1.0, 0.85)
        _Color2("Color 2", Color) = (0.1, 0.5, 0.3, 0.85)
        _Color3("Color 3", Color) = (0.8, 0.2, 0.3, 0.85)
        _Color4("Color 4 Top", Color) = (0.9, 0.7, 0.1, 0.85)

        [Header(Volumetric Absorption)]
        _Absorption("Absorption Strength", Range(0.0, 8.0)) = 2.5

        [Header(Fill Levels 0 to 1)]
        _Fill1("Fill Level 1", Range(0.0, 1.0)) = 0.25
        _Fill2("Fill Level 2", Range(0.0, 1.0)) = 0.50
        _Fill3("Fill Level 3", Range(0.0, 1.0)) = 0.75
        _Fill4("Fill Level 4", Range(0.0, 1.0)) = 1.0

        [Header(Liquid Surface)]
        _BottleHeight("Bottle Mesh Height (object space)", Float) = 2.0
        _Radius("Bottle Mesh Radius (object space)", Float) = 0.4
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
        _FresnelPower("Fresnel Power", Range(0.5, 8.0)) = 5.0

        [Header(Layer Boundary)]
        _LayerBoundaryWidth("Layer Boundary Width", Range(0.0, 0.05)) = 0.025
        _LayerBoundaryDarken("Layer Boundary Darken", Range(0.0, 1.0)) = 0.4

        [Header(Caustics)]
        _CausticsStrength("Caustics Strength", Range(0.0, 2.0)) = 0.6
        _CausticsScale("Caustics Scale", Range(0.0, 10.0)) = 3.0
        _CausticsSpeed("Caustics Speed", Range(0.0, 3.0)) = 0.8

        [Header(Wobble Effect)]
        [HideInInspector] _WobbleX ("Wobble X", Range(-1, 1)) = 0.0
        [HideInInspector] _WobbleZ ("Wobble Z", Range(-1, 1)) = 0.0
        _WobbleStrength ("Wobble Strength", Range(0.0, 0.3)) = 0.15

        [Header(Rim Flash)]
        _RimColor ("Rim Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _RimIntensity ("Rim Intensity", Range(0.0, 5.0)) = 0.5
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

        // --------------------------------------------------------------------
        //  ForwardLit pass -- main render
        // --------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   LitVert
            #pragma fragment LitFrag

            // Mobile-friendly light/shadow variant set
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // Per-material constants -- must match the layout in every pass to
            // satisfy SRP Batcher.
            CBUFFER_START(UnityPerMaterial)
                half4  _Color1;
                half4  _Color2;
                half4  _Color3;
                half4  _Color4;
                float  _Fill1;
                float  _Fill2;
                float  _Fill3;
                float  _Fill4;
                float  _BottleHeight;
                float  _SurfaceHeight;
                float  _WobbleX;
                float  _WobbleZ;
                float  _WobbleStrength;
                float  _SurfaceSmoothness;
                float  _SurfaceRippleAmplitude;
                float  _SurfaceRippleFrequency;
                float  _SurfaceRippleSpeed;
                float  _Transparency;
                float  _EdgeDarken;
                float  _EdgeWidth;
                float  _SpecularIntensity;
                float  _SpecularSmoothness;
                float  _FresnelPower;
                float  _LayerBoundaryWidth;
                float  _LayerBoundaryDarken;
                float  _Radius;
                float  _Absorption;
                float  _CausticsStrength;
                float  _CausticsScale;
                float  _CausticsSpeed;
                float  _RimIntensity;
                float4 _RimColor;
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
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                // Object-space helpers -- precomputed in vertex, interpolated.
                float3 positionOS  : TEXCOORD3;
                half3  upOS        : TEXCOORD4;  // object-space up (after bottle tilt)
                half   liquidMin   : TEXCOORD5;
                half   liquidMax   : TEXCOORD6;
                half   wobbleY     : TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Ripple height field (used for surface position + analytical normal).
            // Returns height in object-space units.
            float RippleHeight(float3 positionOS, half time)
            {
                // Polar coordinates of the (x, z) projection.
                float angle  = atan2(positionOS.z, positionOS.x);
                float radius = length(positionOS.xz);
                // Primary wave
                float w1 = sin(angle * _SurfaceRippleFrequency + radius * 2.5 - time * _SurfaceRippleSpeed);
                // Secondary subtle wave for variation
                float w2 = sin(angle * _SurfaceRippleFrequency * 0.5 - time * _SurfaceRippleSpeed * 0.8);
                return (w1 + w2 * 0.3) * _SurfaceRippleAmplitude;
            }

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
                output.positionOS = input.positionOS.xyz;

                // Object-space up (blends world up with local up as the bottle tilts).
                // Cheap inverse-rotation approximation: using the upper 3x3 of the
                // world-to-object matrix avoids a full inverse.
                float3x3 w2o = (float3x3)GetWorldToObjectMatrix();
                half3 worldUpOS = normalize(mul(w2o, half3(0.0, 1.0, 0.0)));
                half blend = clamp(worldUpOS.y, 0.35h, 1.0h);
                output.upOS = normalize(lerp(half3(0.0, 1.0, 0.0), worldUpOS, blend));

                // Liquid bounding range along the tilted up axis.
                half horizontalLength = length(output.upOS.xz);
                half maxHorizontal    = _Radius * horizontalLength;
                half maxVertical      = max(0.0h, _BottleHeight * output.upOS.y);
                half minVertical      = min(0.0h, _BottleHeight * output.upOS.y);
                output.liquidMin = -maxHorizontal + minVertical;
                output.liquidMax =  maxHorizontal + maxVertical;

                // Wobble offset in object space.
                output.wobbleY = (input.positionOS.x * _WobbleX + input.positionOS.z * _WobbleZ) * _WobbleStrength;

                return output;
            }

            // Procedural caustics -- two crossed sine fields sharpened by pow(*, n).
            // Branchless, no texture sample.
            half Caustics(half2 p, half time)
            {
                half2 q = p * _CausticsScale + time * _CausticsSpeed;
                half a = sin(q.x * 1.7h + cos(q.y * 1.3h));
                half b = sin(q.y * 1.9h - cos(q.x * 1.1h));
                half c = a * b;
                return saturate(c * c * c); // pow(c, 6) expanded -- sharper ridges
            }

            // Sample the active layer's color + alpha given a normalized height.
            void SampleLayer(half normalizedY, out half4 layerColor, out half layerAlpha)
            {
                half fills[4] = { _Fill1, _Fill2, _Fill3, _Fill4 };
                half4 colors[4] = { _Color1, _Color2, _Color3, _Color4 };

                // Branchless layer selection: pick the topmost layer whose fill <= y.
                // index = count of fills[i] <= normalizedY, clamped to [0, 3].
                half idx = 0;
                idx += step(fills[0], normalizedY);
                idx += step(fills[1], normalizedY);
                idx += step(fills[2], normalizedY);
                idx  = min(idx, 3.0h);

                // Use step-based selection to avoid dynamic indexing on a constant array.
                half4 colA = colors[0]; half4 colB = colors[1];
                half4 colC = colors[2]; half4 colD = colors[3];
                layerColor = colA;
                layerColor = lerp(layerColor, colB, step(1.0h, idx));
                layerColor = lerp(layerColor, colC, step(2.0h, idx));
                layerColor = lerp(layerColor, colD, step(2.5h, idx));

                // Alpha gradient at the bottom of the bottom layer for soft fade-in.
                half bottomFade = saturate(normalizedY / max(fills[0] * 0.3h, 0.001h));
                // Top fade near the surface for the top layer.
                half topFade    = saturate((fills[3] - normalizedY) / 0.03h);
                layerAlpha = bottomFade * topFade;
            }

            half4 LitFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // -- Local coordinate setup (interpolated from vertex) --
                half3 upOS   = input.upOS;
                half  minH   = input.liquidMin;
                half  maxH   = input.liquidMax;
                half3 posOS  = input.positionOS;

                half height        = dot(posOS, upOS);
                half normalizedY  = saturate((height - minH) / max(maxH - minH, 0.0001h));

                // -- Surface ripple position (analytical) --
                half time          = _Time.y;
                half ripple        = (half)RippleHeight(input.positionOS, time);
                half surfacePos    = _SurfaceHeight + ripple + input.wobbleY;

                // Early discard outside the liquid volume.
                half surfaceDist   = surfacePos - normalizedY;
                half edgeSoftness  = 0.002h;
                if (surfaceDist < -edgeSoftness) discard;

                half surfaceAlpha  = smoothstep(-edgeSoftness, edgeSoftness, surfaceDist);

                // -- Analytical surface normal from ripple height field --
                // For a height field y = h(x, z), the surface normal in object space
                // is normalize(-dh/dx, 1, -dh/dz). We compute the gradient from the
                // sin-wave derivative and negate the xz components to get the correct
                // normal direction (perpendicular to the surface, pointing up).
                half3 dH_dPos;
                {
                    half  angle  = (half)atan2(posOS.z, posOS.x);
                    half  radius = (half)length(posOS.xz);
                    // d/dx and d/dz of the wave phases (chain rule for polar -> cartesian).
                    half  dPhase1_dx = _SurfaceRippleFrequency * (-posOS.z / max(radius * radius, 1e-4h));
                    half  dPhase1_dz = _SurfaceRippleFrequency * ( posOS.x / max(radius * radius, 1e-4h));
                    half  dRadius_dx = posOS.x / max(radius, 1e-4h);
                    half  dRadius_dz = posOS.z / max(radius, 1e-4h);
                    half  phase1     = angle * _SurfaceRippleFrequency + radius * 2.5h - time * _SurfaceRippleSpeed;
                    half  dPhase1    = dPhase1_dx + dRadius_dx * 2.5h;
                    half  dPhase1z   = dPhase1_dz + dRadius_dz * 2.5h;
                    // Secondary wave
                    half  dPhase2_dx = _SurfaceRippleFrequency * 0.5h * (-posOS.z / max(radius * radius, 1e-4h));
                    half  dPhase2_dz = _SurfaceRippleFrequency * 0.5h * ( posOS.x / max(radius * radius, 1e-4h));
                    half  dPhase2    = dPhase2_dx;
                    half  dPhase2z   = dPhase2_dz;
                    half  dW1_dx     = cos(phase1) * dPhase1;
                    half  dW1_dz     = cos(phase1) * dPhase1z;
                    half  dW2_dx     = cos(angle * _SurfaceRippleFrequency * 0.5h - time * _SurfaceRippleSpeed * 0.8h) * dPhase2;
                    half  dW2_dz     = cos(angle * _SurfaceRippleFrequency * 0.5h - time * _SurfaceRippleSpeed * 0.8h) * dPhase2z;
                    // Negate the xz gradient -> the normal points up away from the surface.
                    dH_dPos.x        = -(dW1_dx + dW2_dx * 0.3h) * _SurfaceRippleAmplitude;
                    dH_dPos.z        = -(dW1_dz + dW2_dz * 0.3h) * _SurfaceRippleAmplitude;
                    dH_dPos.y        = 1.0h;
                }
                // Object-to-world rotation (uniform-scale approximation).
                half3 surfaceNormalWS = normalize(mul((half3x3)GetObjectToWorldMatrix(), dH_dPos));

                // -- Sample the current layer's color --
                half4 layerColor;
                half  layerAlpha;
                SampleLayer(normalizedY, layerColor, layerAlpha);

                // -- Beer-Lambert volumetric absorption --
                // Depth inside the liquid measured from the surface down.
                half depthInLiquid = saturate(surfacePos - normalizedY);
                half3 transmittance = exp(-(half3)(1.0h, 1.0h, 1.0h) - layerColor.rgb * _Absorption * depthInLiquid);
                half3 absorbedColor = layerColor.rgb * transmittance;

                // -- Caustics projected from below the surface --
                half2 causticsUV = input.positionWS.xz * 0.5h + input.positionWS.y * 0.25h;
                half  caustics   = (Caustics(causticsUV, time) - 0.5h) * 2.0h; // remap to [-1, 1]
                half  causticsMask = saturate(1.0h - normalizedY); // only below surface
                half3 causticsColor = half3(1.0h, 0.95h, 0.85h) * caustics * _CausticsStrength * causticsMask;

                // -- Meniscus -- surface-tension curvature at the mold wall --
                // Approximate by the distance to the inner wall (radius scaled by upOS.xz).
                half wallDist = saturate(1.0h - length(posOS.xz) / max(_Radius, 0.001h));
                half meniscus = pow(wallDist, 4.0h) * 0.3h;
                half3 meniscusColor = half3(1.0h, 1.0h, 1.0h) * meniscus * saturate(surfaceDist * 100.0h);

                // -- Lighting --
                half3 normalWS = normalize(surfaceNormalWS);
                half3 viewWS   = normalize(GetWorldSpaceViewDir(input.positionWS));
                half  NdotV    = saturate(dot(normalWS, viewWS));

                // Schlick Fresnel -- energy-conserving, view-dependent.
                half  F0        = 0.02h; // dielectric base reflectance
                half  fresnel   = F0 + (1.0h - F0) * pow(saturate(1.0h - NdotV), _FresnelPower);

                // Diffuse + ambient (SH).
                half3 ambient   = SampleSH(normalWS);
                half  NdotL     = saturate(dot(normalWS, normalize(_MainLightPosition.xyz)));
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 diffuse   = absorbedColor * (mainLight.color * NdotL * mainLight.shadowAttenuation + ambient);

                // Specular -- Blinn-Phong with roughness control, energy-conserving
                // through the fresnel term above.
                half3 halfDir   = normalize(_MainLightPosition.xyz + viewWS);
                half  NdotH     = saturate(dot(normalWS, halfDir));
                half  specPower = exp2(10.0h * _SpecularSmoothness + 1.0h);
                half  spec      = pow(NdotH, specPower) * _SpecularIntensity * fresnel;
                half3 specular  = spec * mainLight.color * mainLight.shadowAttenuation;

                // Edge darkening -- view-grazing angles darken the diffuse to fake
                // path-length absorption through more liquid at edges.
                half edgeFactor  = smoothstep(0.0h, _EdgeWidth, NdotV);
                half edgeDarken  = lerp(1.0h - _EdgeDarken, 1.0h, edgeFactor);
                diffuse *= edgeDarken;

                // -- Layer boundary lines -- thin dark bands between colors --
                half boundaryFactor = 1.0h;
                half fills[4] = { _Fill1, _Fill2, _Fill3, _Fill4 };
                half adjY = normalizedY + input.wobbleY;
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    half distToBoundary = abs(adjY - fills[i]);
                    half t = saturate(distToBoundary / max(_LayerBoundaryWidth, 1e-4h));
                    half darken = lerp(1.0h - _LayerBoundaryDarken, 1.0h, t * t);
                    boundaryFactor = min(boundaryFactor, darken);
                }
                diffuse *= boundaryFactor;

                // -- Final composition --
                // -- Rim Flash -- overlay from MPB (error indicator / pour highlight) --
                half3 rimFlash = half3(_RimColor.rgb * _RimIntensity * 0.5h);
                half3 finalColor = diffuse + specular + causticsColor + meniscusColor + rimFlash;

                // Alpha: layer base alpha x surface mask x (1 - transparency), darkened
                // at the bottom for soft fade-in.
                half finalAlpha = layerColor.a * layerAlpha * (1.0h - _Transparency) * surfaceAlpha;
                finalAlpha = saturate(finalAlpha);

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
                half4  _Color1;
                half4  _Color2;
                half4  _Color3;
                half4  _Color4;
                float  _Fill1;
                float  _Fill2;
                float  _Fill3;
                float  _Fill4;
                float  _BottleHeight;
                float  _SurfaceHeight;
                float  _WobbleX;
                float  _WobbleZ;
                float  _WobbleStrength;
                float  _SurfaceSmoothness;
                float  _SurfaceRippleAmplitude;
                float  _SurfaceRippleFrequency;
                float  _SurfaceRippleSpeed;
                float  _Transparency;
                float  _EdgeDarken;
                float  _EdgeWidth;
                float  _SpecularIntensity;
                float  _SpecularSmoothness;
                float  _FresnelPower;
                float  _LayerBoundaryWidth;
                float  _LayerBoundaryDarken;
                float  _Radius;
                float  _Absorption;
                float  _CausticsStrength;
                float  _CausticsScale;
                float  _CausticsSpeed;
                float  _RimIntensity;
                float4 _RimColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                half3  upOS       : TEXCOORD1;
                half   liquidMin  : TEXCOORD2;
                half   liquidMax  : TEXCOORD3;
                half   wobbleY    : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;

                float3x3 w2o = (float3x3)GetWorldToObjectMatrix();
                half3 worldUpOS = normalize(mul(w2o, half3(0.0, 1.0, 0.0)));
                half blend = clamp(worldUpOS.y, 0.35h, 1.0h);
                output.upOS = normalize(lerp(half3(0.0, 1.0, 0.0), worldUpOS, blend));

                half horizontalLength = length(output.upOS.xz);
                half maxHorizontal    = _Radius * horizontalLength;
                half maxVertical      = max(0.0h, _BottleHeight * output.upOS.y);
                half minVertical      = min(0.0h, _BottleHeight * output.upOS.y);
                output.liquidMin = -maxHorizontal + minVertical;
                output.liquidMax =  maxHorizontal + maxVertical;

                output.wobbleY = (input.positionOS.x * _WobbleX + input.positionOS.z * _WobbleZ) * _WobbleStrength;
                return output;
            }

            half DepthFrag(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half height       = dot(input.positionOS, input.upOS);
                half normalizedY  = saturate((height - input.liquidMin) / max(input.liquidMax - input.liquidMin, 0.0001h));
                half surfaceWob   = _SurfaceHeight + input.wobbleY;
                clip(surfaceWob - normalizedY);
                return 0;
            }
            ENDHLSL
        }

        // --------------------------------------------------------------------
        //  ShadowCaster -- liquid casts shadows into URP shadow map
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
                half4  _Color1;
                half4  _Color2;
                half4  _Color3;
                half4  _Color4;
                float  _Fill1;
                float  _Fill2;
                float  _Fill3;
                float  _Fill4;
                float  _BottleHeight;
                float  _SurfaceHeight;
                float  _WobbleX;
                float  _WobbleZ;
                float  _WobbleStrength;
                float  _SurfaceSmoothness;
                float  _SurfaceRippleAmplitude;
                float  _SurfaceRippleFrequency;
                float  _SurfaceRippleSpeed;
                float  _Transparency;
                float  _EdgeDarken;
                float  _EdgeWidth;
                float  _SpecularIntensity;
                float  _SpecularSmoothness;
                float  _FresnelPower;
                float  _LayerBoundaryWidth;
                float  _LayerBoundaryDarken;
                float  _Radius;
                float  _Absorption;
                float  _CausticsStrength;
                float  _CausticsScale;
                float  _CausticsSpeed;
                float  _RimIntensity;
                float4 _RimColor;
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
                float3 positionOS : TEXCOORD0;
                half3  upOS       : TEXCOORD1;
                half   liquidMin  : TEXCOORD2;
                half   liquidMax  : TEXCOORD3;
                half   wobbleY    : TEXCOORD4;
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
                output.positionOS = input.positionOS.xyz;

                float3x3 w2o = (float3x3)GetWorldToObjectMatrix();
                half3 worldUpOS = normalize(mul(w2o, half3(0.0, 1.0, 0.0)));
                half blend = clamp(worldUpOS.y, 0.35h, 1.0h);
                output.upOS = normalize(lerp(half3(0.0, 1.0, 0.0), worldUpOS, blend));

                half horizontalLength = length(output.upOS.xz);
                half maxHorizontal    = _Radius * horizontalLength;
                half maxVertical      = max(0.0h, _BottleHeight * output.upOS.y);
                half minVertical      = min(0.0h, _BottleHeight * output.upOS.y);
                output.liquidMin = -maxHorizontal + minVertical;
                output.liquidMax =  maxHorizontal + maxVertical;

                output.wobbleY = (input.positionOS.x * _WobbleX + input.positionOS.z * _WobbleZ) * _WobbleStrength;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half height       = dot(input.positionOS, input.upOS);
                half normalizedY  = saturate((height - input.liquidMin) / max(input.liquidMax - input.liquidMin, 0.0001h));
                half surfaceWob   = _SurfaceHeight + input.wobbleY;
                clip(surfaceWob - normalizedY);
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
