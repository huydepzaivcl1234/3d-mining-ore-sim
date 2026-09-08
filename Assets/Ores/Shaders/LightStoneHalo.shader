Shader "Mining Simulator/Light Stone Halo"
{
    Properties
    {
        _AuraColor ("Inner Color", Color) = (1, 0.82, 0.25, 0.65)
        _OuterColor ("Outer Color", Color) = (1, 0.52, 0.08, 0.25)
        _Opacity ("Opacity", Range(0, 1)) = 0.42
        _Pulse ("Pulse", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        // Never render the inside of the halo. This prevents a nearby camera
        // from seeing a full-screen transparent sphere.
        Cull Back
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "Halo"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; float3 positionWS : TEXCOORD2; };

            CBUFFER_START(UnityPerMaterial)
                half4 _AuraColor;
                half4 _OuterColor;
                half _Opacity;
                half _Pulse;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(position.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half rim = pow(1.0h - saturate(abs(dot(normalize(input.normalWS), normalize(input.viewWS)))), 2.2h);
                half shimmer = 0.88h + 0.12h * sin(input.positionWS.y * 5.0h + _Time.y * 1.5h);
                half alpha = saturate((rim * 0.82h + 0.12h) * _Opacity * shimmer * lerp(0.88h, 1.12h, _Pulse));
                half3 color = lerp(_AuraColor.rgb, _OuterColor.rgb, rim) * (1.25h + _Pulse * 0.35h);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
