Shader "Mining Simulator/Forest Grass Impact"
{
    Properties
    {
        [HDR] _Tint ("Tint", Color) = (1,1,1,1)
        _Intensity ("Glow intensity", Range(0,4)) = 1.4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Intensity;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _Tint;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                return half4(input.color.rgb * _Intensity, input.color.a);
            }
            ENDHLSL
        }
    }
}
