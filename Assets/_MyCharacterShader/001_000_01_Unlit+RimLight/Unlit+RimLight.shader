Shader "Unlit+RimLight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RimWidth ("リムライトの幅", Float) = 1
    }
    SubShader
    {
        Name "UNLIT+RIMLIGHT"
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _RimWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // View Space法線
                o.normal = mul(UNITY_MATRIX_IT_MV, float4(v.normal, 1));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // ViewSpaceから見た視線方向を一律で(0, 0, 1)として計算
                float rimLight = 1.0 - saturate(pow(dot(normalize(i.normal), float3(0, 0, 1)), _RimWidth));
                fixed4 col = tex2D(_MainTex, i.uv);
                return col + rimLight;
            }
            ENDCG
        }
    }
}
