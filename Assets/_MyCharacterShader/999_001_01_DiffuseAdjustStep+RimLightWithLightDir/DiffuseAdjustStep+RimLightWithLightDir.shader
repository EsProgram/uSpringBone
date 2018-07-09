Shader "DiffuseAdjustStep+RimLightWithLightDir"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ViewLightDir ("ビュー座標のライト方向", Vector) = (0, 0, 0, 0)
        _RimWidth ("リムライトの幅", Float) = 1
        _RimSharpnessWithLight ("リムライトが光の方向によって受ける影響の度合い", Range(0, 1)) = 1
        _RimSubtraction ("リムの影響を減算する値", Range(0, 1)) = 0
        _RimHighlightStrength ("リムライトの影響を受ける部分の強さ", Float) = 1
        _DiffuseScale ("Diffuseに掛ける値", Float) = 1
        _DiffuseMin ("Diffuseの最小値", Float) = 0
        _DiffuseMax ("Diffuseの最大値", Float) = 1
        _DiffuseStep ("Diffuseのステップ値", Float) = 0.1
        _EmvironmentLightPower ("環境光の強さ", Float) = 0.05
        _EnvironmentLightColor ("環境光の色", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "DIFFUSEADJUSTSTEP+RIMLIGHTWITHLIGHTDIR"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                fixed4 vertex : POSITION;
                fixed2 uv : TEXCOORD0;
                fixed3 normal : NORMAL;
            };

            struct v2f
            {
                fixed2 uv : TEXCOORD0;
                fixed4 vertex : SV_POSITION;
                fixed3 normal : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _MainTex_ST;
            fixed _RimWidth;
            fixed3 _ViewLightDir;
            fixed _RimSharpnessWithLight;
            fixed _RimSubtraction;
            fixed _RimHighlightStrength;
            fixed _DiffuseScale;
            fixed _DiffuseMin;
            fixed _DiffuseMax;
            fixed _DiffuseStep;
            fixed _EmvironmentLightPower;
            fixed3 _EnvironmentLightColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // View Space法線
                o.normal = mul(UNITY_MATRIX_IT_MV, fixed4(v.normal, 1.0));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Diffuse計算
                fixed diffuse = dot(normalize(i.normal), normalize(_ViewLightDir));
                fixed diffuseStep = diffuse * _DiffuseScale - fmod(diffuse * _DiffuseScale, _DiffuseStep);
                diffuse = clamp(diffuseStep, _DiffuseMin, _DiffuseMax);

                // 法線とライトの方向から光が強く当たるほど1に近くなるよう値を算出(光が当たりにくい箇所は0に近付く)
                // FIXME: 何故かios実機で動かすと陰が出てくる。意味不明。 -> pow無くしたら直った何故。
                // fixed lightAttendance = clamp(pow(dot(normalize(i.normal.xyz), normalize(_ViewLightDir.xyz)), _RimSharpnessWithLight) - _RimSubtraction, 0.0, 1.0);
                fixed lightAttendance = clamp(dot(normalize(i.normal.xyz), normalize(_ViewLightDir.xyz)) * _RimSharpnessWithLight - _RimSubtraction, 0.0, 1.0);
                // fixed lightAttendance = clamp(dot(normalize(i.normal.xyz), normalize(_ViewLightDir.xyz)) - _RimSubtraction, 0.0, 1.0);

                fixed4 col = tex2D(_MainTex, i.uv);
                // fixed3 ret = col.rgb * diffuse + lightAttendance * _RimHighlightStrength + _EnvironmentLightColor * _EmvironmentLightPower;
                fixed3 ret = col.rgb + abs(lightAttendance * _RimHighlightStrength);
                return float4(ret.r, ret.g, ret.b, col.a);
            }
            ENDCG
        }
    }
}
