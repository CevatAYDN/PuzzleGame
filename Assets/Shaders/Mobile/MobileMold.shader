Shader "PuzzleGame/MobileMold"
{
    Properties
    {
        [Header(Base)]
        _MainTex ("Bottle Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        [Header(Bottle Glass)]
        _GlassColor ("Glass Tint", Color) = (0.9, 0.95, 1.0, 0.3)
        _GlassAlpha ("Glass Opacity", Range(0, 1)) = 0.3

        [Header(Liquid - Multi Layer)]
        _Color1("Color 1 (Bottom)", Color) = (0.15, 0.55, 0.95, 0.98)
        _Color2("Color 2", Color) = (0.08, 0.65, 0.35, 0.98)
        _Color3("Color 3", Color) = (0.95, 0.18, 0.28, 0.98)
        _Color4("Color 4 (Top)", Color) = (0.98, 0.72, 0.05, 0.98)

        [Header(Fill Levels)]
        _Fill1("Fill Level 1", Range(0, 1)) = 0.25
        _Fill2("Fill Level 2", Range(0, 1)) = 0.50
        _Fill3("Fill Level 3", Range(0, 1)) = 0.75
        _Fill4("Fill Level 4", Range(0, 1)) = 1.0

        [Header(Surface & Sparkle)]
        _SurfaceHeight ("Surface Height", Range(0, 1)) = 1.0
        _WaveSpeed ("Wave Speed", Float) = 2.0
        _WaveAmplitude ("Wave Height", Float) = 0.04
        _SparkleIntensity("Sparkle Intensity", Range(0, 2)) = 0.08
        _SparkleSize("Sparkle Size", Range(1, 32)) = 12.0

        [Header(Layer Boundary)]
        _LayerBoundaryWidth("Layer Boundary Width", Range(0, 0.05)) = 0.012
        _LayerBoundaryDarken("Layer Boundary Darken", Range(0, 1)) = 0.15

        [Header(Performance)]
        [Toggle(_USE_NORMAL_MAP)] _UseNormalMap ("Enable Normal Map", Float) = 0
        [NoScaleOffset] _NormalMap ("Normal Map (Optional)", 2D) = "bump" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        // ── Pass 1: Bottle Glass (back faces) ──
        Pass
        {
            Name "GlassBack"
            CGPROGRAM
            #pragma vertex vertGlass
            #pragma fragment fragGlass
            #pragma target 2.0
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _GlassColor;
            fixed _GlassAlpha;

            v2f vertGlass(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 fragGlass(v2f i) : SV_Target
            {
                fixed4 glass = _GlassColor;
                glass.a = _GlassAlpha;
                return glass;
            }
            ENDCG
        }

        // ── Pass 2: Liquid + Glass (front faces) ──
        Pass
        {
            Name "LiquidFront"
            CGPROGRAM
            #pragma vertex vertLiquid
            #pragma fragment fragLiquid
            #pragma target 2.0
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local _USE_NORMAL_MAP

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _GlassColor;
            fixed _GlassAlpha;

            fixed4 _Color1, _Color2, _Color3, _Color4;
            float _Fill1, _Fill2, _Fill3, _Fill4;
            float _SurfaceHeight;
            float _WaveSpeed;
            float _WaveAmplitude;
            float _SparkleIntensity;
            float _SparkleSize;
            float _LayerBoundaryWidth;
            float _LayerBoundaryDarken;

        #if _USE_NORMAL_MAP
            sampler2D _NormalMap;
        #endif

            // Simple hash for sparkle
            float hash2(float2 p)
            {
                p = 50.0 * frac(p * 0.3183099 + float2(0.71, 0.113));
                return frac(p.x * p.y * (p.x + p.y));
            }

            void getLayerColor(float localY, fixed4 colors[4], float fills[4], out fixed4 layerColor, out float layerAlpha)
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

            v2f vertLiquid(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 fragLiquid(v2f i) : SV_Target
            {
                // Determine local Y from vertex position (object space)
                // Since we don't have object-space position in v2f, approximate via world-space
                // For a upright cylinder, worldPos.y relative to object pivot approximates height
                float3 worldPos = i.worldPos;
                float time = _Time.y;

                // Simple wave on surface
                float wave = sin(worldPos.x * 4.0 + time * _WaveSpeed)
                           * cos(worldPos.z * 3.5 - time * _WaveSpeed * 0.7)
                           * _WaveAmplitude;

                // Normalized height: approximate from world Y relative to pivot
                // Assume mold is ~2 units tall, pivot at base
                float localY = saturate((worldPos.y - unity_ObjectToWorld._m13) / 2.0);
                float surfaceY = _SurfaceHeight + wave;

                if (localY > surfaceY + 0.002) discard;

                fixed4 colors[4] = { _Color1, _Color2, _Color3, _Color4 };
                float fills[4] = { _Fill1, _Fill2, _Fill3, _Fill4 };

                fixed4 layerColor;
                float layerAlpha;
                getLayerColor(localY, colors, fills, layerColor, layerAlpha);

                // Layer boundary darkening
                float boundaryFactor = 1.0;
                for (int k = 0; k < 3; k++)
                {
                    float distToBoundary = abs(localY - fills[k]);
                    if (distToBoundary < _LayerBoundaryWidth)
                    {
                        float t = distToBoundary / _LayerBoundaryWidth;
                        boundaryFactor = min(boundaryFactor, lerp(1.0 - _LayerBoundaryDarken, 1.0, t * t));
                    }
                }

                // Simple directional light
                float3 lightDir = normalize(float3(0.5, 1.0, 0.3));
                float NdotL = max(0.2, dot(normalize(i.normalWS), lightDir));
                float3 diffuse = layerColor.rgb * NdotL;

                // Sparkle (lightweight, screen-space hash)
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float sparkleHash = hash2(screenUV * _SparkleSize + floor(time * 4.7));
                float sparkleGate = smoothstep(0.92, 1.0, sparkleHash)
                                  * smoothstep(0.0, 0.06, abs(sparkleHash - 0.96));
                float3 sparkleColor = sparkleGate * _SparkleIntensity * 3.0 * fixed3(1, 1, 1);

                float3 finalColor = diffuse * boundaryFactor + sparkleColor;

                // Surface fade
                float surfaceAlpha = smoothstep(-0.002, 0.002, surfaceY - localY);

                float finalAlpha = layerColor.a * layerAlpha * surfaceAlpha;
                finalAlpha = saturate(finalAlpha);

                return fixed4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }

    Fallback "Unlit/Transparent"
}
