Shader "Mining Simulator/Monster Attack Warning"
{
    Properties { _Color("Color", Color)=(1,.08,.02,.65) _Progress("Contact progress", Range(0,1))=0 }
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
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Progress;
            CBUFFER_END
            Varyings vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; return o; }
            half4 frag(Varyings i):SV_Target
            {
                float radius=length((i.uv-.5)*2);
                clip(1-radius);
                float rim=smoothstep(.94,.97,radius);
                float fill=1-smoothstep(_Progress-.012,_Progress+.012,radius);
                return half4(_Color.rgb, _Color.a*max(rim,max(.12,fill*.8)));
            }
            ENDHLSL
        }
    }
}
