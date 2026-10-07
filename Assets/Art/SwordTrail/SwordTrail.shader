Shader "MiningSimulator/SwordTrail"
{
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
                half4 c = i.color;
                c.a *= smoothstep(0.0, 0.15, i.uv.x) * (1.0 - smoothstep(0.85, 1.0, i.uv.x));
                return c;
            }
            ENDHLSL
        }
    }
}
