Shader "PuzzleGame/MobileLiquid"
{
    Properties
    {
        [Header(Liquid)]
        _LiquidColor ("Liquid Color", Color) = (0.2, 0.6, 1.0, 1.0)
        _FillLevel ("Fill Level (0-1)", Range(0, 1)) = 0.5

        [Header(Wave)]
        _WaveSpeed ("Wave Speed", Float) = 2.0
        _WaveAmplitude ("Wave Height", Float) = 0.04
        _WaveFrequency ("Wave Frequency", Float) = 2.0

        [Header(Advanced)]
        [Toggle(_USE_GRADIENT)] _UseGradient ("Enable Gradient", Float) = 0
        _ColorTop ("Color Top", Color) = (0.3, 0.7, 1.0, 1.0)
        _ColorBottom ("Color Bottom", Color) = (0.1, 0.3, 0.8, 1.0)

        [Header(Wobble Effect)]
        [HideInInspector] _WobbleX ("Wobble X", Range(-1, 1)) = 0.0
        [HideInInspector] _WobbleZ ("Wobble Z", Range(-1, 1)) = 0.0

        [Header(Rim Flash)]
        _RimColor ("Rim Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _RimIntensity ("Rim Intensity", Range(0.0, 5.0)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "Liquid"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            // Mobile optimization: strip all unnecessary variants
            #pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
            #pragma skip_variants DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
            #pragma skip_variants LIGHTMAP_ON DYNAMICLIGHTMAP_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            float4 _LiquidColor;
            half _FillLevel;
            half _WaveSpeed;
            half _WaveAmplitude;
            half _WaveFrequency;
            float _WobbleX;
            float _WobbleZ;
            float4 _RimColor;
            float _RimIntensity;

            #if _USE_GRADIENT
            float4 _ColorTop;
            float4 _ColorBottom;
            #endif

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 posOS = v.vertex.xyz;
                // Simple wobble offset
                float wobbleY = (posOS.x * _WobbleX + posOS.z * _WobbleZ) * 0.05;
                o.vertex = TransformObjectToHClip(posOS + float3(0, wobbleY, 0));
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // --- Wave surface calculation ---
                half wave1 = sin(i.uv.x * 6.283 * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveAmplitude;
                half wave2 = sin(i.uv.x * 12.566 * _WaveFrequency * 0.7 + _Time.y * _WaveSpeed * 1.3) * _WaveAmplitude * 0.5;
                half wave = wave1 + wave2;

                half surface = _FillLevel + wave;

                // --- Fill mask: below surface = liquid, above = transparent ---
                half fillMask = 1.0 - smoothstep(surface - 0.015, surface + 0.015, i.uv.y);

                // No liquid when completely empty
                fillMask *= step(0.001, _FillLevel);

                // --- Color ---
                #if _USE_GRADIENT
                half gradientT = saturate((surface - i.uv.y) / max(surface, 0.001));
                float4 col = lerp(_ColorBottom, _ColorTop, gradientT);
                #else
                float4 col = _LiquidColor;
                #endif

                col.a *= fillMask;

                // Rim flash overlay
                col.rgb += _RimColor.rgb * _RimIntensity * fillMask;

                return col;
            }
            ENDHLSL
        }
    }

    Fallback "Unlit/Transparent"
}
