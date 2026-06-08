Shader "Custom/Wobble" {
    Properties {
        [Toggle] _QualityLevel ("Quality Level", Float) = 1
        _WobbleX ("Wobble X", Float) = 0
        _WobbleZ ("Wobble Z", Float) = 0
        _WobbleStrength ("Wobble Strength", Float) = 0.05
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
        Tags {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _QUALITYLEVEL_ON
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _WobbleX;
            float _WobbleZ;
            float _WobbleStrength;

            v2f vert (appdata v)
            {
                v2f o;
                
                #ifdef _QUALITYLEVEL_ON
                    // High Quality: Apply wobble to vertex position
                    float3 wobble = float3(_WobbleX, 0, _WobbleZ) * _WobbleStrength;
                    o.vertex = UnityObjectToClipPos(v.vertex + wobble);
                #else
                    // Low Quality: Simple vertex squash instead of wobble
                    o.vertex = UnityObjectToClipPos(v.vertex);
                #endif
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
}
