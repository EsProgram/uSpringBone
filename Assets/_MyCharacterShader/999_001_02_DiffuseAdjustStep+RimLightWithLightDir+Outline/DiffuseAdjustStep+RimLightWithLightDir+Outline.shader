Shader "DiffuseAdjustStep+RimLightWithLightDir+Outline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _ViewLightDir ("ビュー座標のライト方向", Vector) = (0, 0, 0, 0)
        _RimWidth ("リムライトの幅", Float) = 1
        _RimSharpnessWithLight ("リムライトが光の方向によって受ける影響の度合い", Float) = 1
        _RimSubtraction ("リムの影響を減算する値", Range(0, 1)) = 0
        _RimHighlightStrength ("リムライトの影響を受ける部分の強さ", Float) = 1
        _DiffuseScale ("Diffuseに掛ける値", Float) = 1
        _DiffuseMin ("Diffuseの最小値", Float) = 0
        _DiffuseMax ("Diffuseの最大値", Float) = 1
        _DiffuseStep ("Diffuseのステップ値", Float) = 0.1
        _OutlineScale ("アウトライン幅", Float) = 0.003
        _OutlineColor ("アウトラインの色(テクスチャ色に乗算)", Color) = (0, 0, 0, 0)
        _EmvironmentLightPower ("環境光の強さ", Float) = 0.05
        _EnvironmentLightColor ("環境光の色", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        UsePass "Outline/OUTLINE"
        UsePass "DiffuseAdjustStep+RimLightWithLightDir/DIFFUSEADJUSTSTEP+RIMLIGHTWITHLIGHTDIR"
    }
}
