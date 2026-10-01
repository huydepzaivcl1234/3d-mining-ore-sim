Shader "Mining Simulator/Monster Dissolve"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _DissolveAmount ("Dissolve", Range(0,1)) = 0
        [HDR] _EdgeColor ("Edge", Color) = (1,.6,.1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST, _BaseColor, _EdgeColor;
                float _DissolveAmount;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 cell = floor(input.positionWS * 25);
                float noise = frac(sin(dot(cell, float3(12.9898,78.233,37.719))) * 43758.5453);
                clip(noise - _DissolveAmount);
                half4 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(base.a - .05);
                Light light = GetMainLight();
                half3 normal = normalize(input.normalWS);
                half3 lit = base.rgb * (SampleSH(normal) + light.color * saturate(dot(normal, light.direction)));
                float edge = _DissolveAmount > .001 ? 1 - smoothstep(0, .07, noise - _DissolveAmount) : 0;
                return half4(lerp(lit, _EdgeColor.rgb * 2, edge), 1);
            }
            ENDHLSL
        }
    }
}
