Shader "Unlit+RimLightWithLightDir"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RimWidth ("リムライトの幅", Float) = 1
        _ViewLightDir ("ビュー座標のライト方向", Vector) = (0, 0, 0, 0)
        _RimSharpnessWithLight ("リムライトが光の方向によって受ける影響の度合い", Float) = 1
        _RimSubtraction ("リムの影響を減算する値", Range(0, 1)) = 0
        _RimHighlightStrength ("リムライトの影響を受ける部分の強さ", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "UNLIT+RIMLIGHTWITHLIGHTDIR"
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
            float3 _ViewLightDir;
            float _RimSharpnessWithLight;
            float _RimSubtraction;
            float _RimHighlightStrength;

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
                // 法線とライトの方向から光が強く当たるほど1に近くなるよう値を算出(光が当たりにくい箇所は0に近付く)
                float lightAttendance = clamp(pow(dot(normalize(i.normal), normalize(_ViewLightDir)), _RimSharpnessWithLight) - _RimSubtraction, 0, 1);

                fixed4 col = tex2D(_MainTex, i.uv);
                return col + lightAttendance * _RimHighlightStrength;
            }
            ENDCG
        }
    }
}
