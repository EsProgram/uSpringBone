Shader "Matcap"
{
    Properties
    {
        _Matcap ("Matcap Texture", 2D) = "white" {}
        _MatcapRange ("Matcapの範囲", Range(0, 1.5)) = 1
    }
    SubShader
    {
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
                float4 normal : TEXCOORD1;
            };

            sampler2D _Matcap;
            float _MatcapRange;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                //o.normal = float4(v.normal, 1);//LocalSpace Normal
                //o.normal = float4(mul((float3x3)_Object2World, v.normal), 1);//WorldSpace Normal
                o.normal = mul(UNITY_MATRIX_IT_MV, float4(v.normal, 1));//ProjectionSpace Normal
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 normalProj = normalize(i.normal.xyz) * 0.5 + 0.5;
                fixed4 col = tex2D(_Matcap, normalProj * _MatcapRange);
                return col;
            }
            ENDCG
        }
    }
}