Shader "PuzzleGame/MobileBottle"
{
    Properties
    {
        [Header(Base)]
        _MainTex ("Bottle Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        [Header(Bottle Glass)]
        _GlassColor ("Glass Tint", Color) = (0.9, 0.95, 1.0, 0.3)
        _GlassAlpha ("Glass Opacity", Range(0, 1)) = 0.3

        [Header(Liquid)]
        _LiquidColor ("Liquid Color", Color) = (0.2, 0.6, 1.0, 1.0)
        _FillLevel ("Fill Level (0-1)", Range(0, 1)) = 0.5
        _WaveSpeed ("Wave Speed", Float) = 2.0
        _WaveAmplitude ("Wave Height", Float) = 0.04

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

        // ── Pass 1: Bottle Glass (back) ──
        Pass
        {
            Name "GlassBack"
            CGPROGRAM
            #pragma vertex vertGlass
            #pragma fragment fragGlass
            #pragma target 2.0
            // Mobile: strip fog, lightmap variants
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
            half _GlassAlpha;

            v2f vertGlass (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 fragGlass (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _GlassColor;
                col.a = _GlassAlpha;
                return col;
            }
            ENDCG
        }

        // ── Pass 2: Liquid Fill ──
        Pass
        {
            Name "LiquidFill"
            CGPROGRAM
            #pragma vertex vertLiquid
            #pragma fragment fragLiquid
            #pragma target 2.0
            #pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
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
                float worldY : TEXCOORD1;
            };

            fixed4 _LiquidColor;
            half _FillLevel;
            half _WaveSpeed;
            half _WaveAmplitude;

            v2f vertLiquid (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldY = worldPos.y;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 fragLiquid (v2f i) : SV_Target
            {
                // Fill cutoff: liquid appears from bottom up
                half fillLine = _FillLevel;

                // Wave effect at the fill surface (very cheap: 1 sin call)
                half wave = sin(i.uv.x * 6.283 * 2.0 + _Time.y * _WaveSpeed) * _WaveAmplitude;
                half surface = fillLine + wave;

                // Smooth step at surface (anti-aliased edge)
                half alpha = 1.0 - smoothstep(surface - 0.02, surface + 0.02, i.uv.y);

                // Below fill level: full liquid color
                half belowFill = step(i.uv.y, surface - 0.02);
                alpha = max(alpha, belowFill);

                // No liquid when empty
                alpha *= step(0.001, _FillLevel);

                fixed4 col = _LiquidColor;
                col.a *= alpha;
                return col;
            }
            ENDCG
        }

        // ── Pass 3: Bottle Glass (front) ──
        Pass
        {
            Name "GlassFront"
            CGPROGRAM
            #pragma vertex vertGlassFront
            #pragma fragment fragGlassFront
            #pragma target 2.0
            #pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
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
            half _GlassAlpha;

            v2f vertGlassFront (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 fragGlassFront (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                // Highlight rim effect
                half rim = 1.0 - abs(i.uv.x - 0.5) * 2.0;
                rim = smoothstep(0.7, 1.0, rim);
                col.rgb += _GlassColor.rgb * rim * 0.15;
                col.a = _GlassAlpha;
                return col;
            }
            ENDCG
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
