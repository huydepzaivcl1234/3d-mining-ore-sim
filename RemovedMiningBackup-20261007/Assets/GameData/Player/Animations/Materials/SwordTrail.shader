Shader "MiningSimulator/SwordTrail"
{
    Properties
    {
        _CoreColor ("Bright Core Color", Color) = (0.88, 0.97, 1, 1)
        _CoreStrength ("Core Highlight", Range(0, 1)) = 0.55
        _Opacity ("Trail Visibility", Range(0, 3)) = 1.8
        _EdgeSoftness ("Soft Edges", Range(0.01, 0.45)) = 0.22
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half _CoreStrength;
                half _Opacity;
                half _EdgeSoftness;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color; o.uv = v.uv; return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                half softness = max(0.01, _EdgeSoftness);
                // Fade the old tail; keep the newest blade edge bright.
                half tail = smoothstep(0.0, softness * 1.5, i.uv.x);
                half blade = smoothstep(0.0, softness, i.uv.y) *
                             (1.0 - smoothstep(1.0 - softness, 1.0, i.uv.y));
                half core = pow(saturate(1.0 - abs(i.uv.y - 0.58) / 0.58), 2.0);
                half4 c;
                c.rgb = lerp(i.color.rgb, _CoreColor.rgb, saturate(_CoreStrength * core));
                c.a = saturate(i.color.a * _Opacity * tail * blade * lerp(0.55, 1.0, core));
                return c;
            }
            ENDHLSL
        }
    }
}
