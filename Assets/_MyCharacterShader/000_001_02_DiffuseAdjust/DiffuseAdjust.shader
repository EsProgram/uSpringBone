Shader "DiffuseAdjust"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WorldLightDir ("ワールド座標のライト方向", Vector) = (0, 0, 0 , 0)
        _DiffuseScale ("Diffuseに掛ける値", Float) = 1
        _DiffuseMin ("Diffuseの最小値", Float) = 0
        _DiffuseMax ("Diffuseの最大値", Float) = 1
    }
    SubShader
    {
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
            float3 _WorldLightDir;
            float _DiffuseScale;
            float _DiffuseMin;
            float _DiffuseMax;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // World Space法線
                o.normal = mul((float3x3)unity_ObjectToWorld, v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float diffuse = dot(normalize(i.normal), normalize(_WorldLightDir));
                diffuse = clamp(diffuse * _DiffuseScale, _DiffuseMin, _DiffuseMax);

                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb *= diffuse;
                return col;
            }
            ENDCG
        }
    }
}
