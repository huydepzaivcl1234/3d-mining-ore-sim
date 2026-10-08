Shader "Mining Simulator/Dark Stone Aura"
{
    Properties
    {
        _AuraColor ("Shadow Color", Color) = (0.006, 0.01, 0.016, 0.85)
        _OuterColor ("Mist Color", Color) = (0.035, 0.055, 0.065, 0.55)
        _Opacity ("Opacity", Range(0, 1)) = 0.58
        _Pulse ("Pulse", Range(0, 1)) = 0.5
        _FlowSpeed ("Flow Speed", Float) = 0.32
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+5" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        // Back-face culling keeps the effect local even if the camera enters it.
        Cull Back
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "ShadowMist"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 viewWS : TEXCOORD2; };

            CBUFFER_START(UnityPerMaterial)
                half4 _AuraColor;
                half4 _OuterColor;
                half _Opacity;
                half _Pulse;
                half _FlowSpeed;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 breathe = input.positionOS.xyz * (1.0 + 0.025 * sin(_Time.y * 1.3 + input.positionOS.y * 4.0));
                VertexPositionInputs position = GetVertexPositionInputs(breathe);
                output.positionCS = position.positionCS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(position.positionWS);
                return output;
            }

            half HashNoise(float3 p)
            {
                half a = sin(dot(p, float3(12.9898, 78.233, 37.719)));
                half b = sin(dot(p * 1.73, float3(39.346, 11.135, 83.155)));
                return saturate(0.5h + 0.28h * a + 0.22h * b);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 flowing = input.positionOS * 3.2 + float3(0, _Time.y * _FlowSpeed, _Time.y * 0.09);
                half noise = smoothstep(0.24h, 0.74h, HashNoise(flowing));
                half rim = pow(1.0h - saturate(abs(dot(normalize(input.normalWS), normalize(input.viewWS)))), 1.45h);
                half heightFade = saturate(1.15h - abs(input.positionOS.y) * 0.34h);
                half alpha = saturate((noise * 0.52h + rim * 0.48h) * heightFade * _Opacity * lerp(0.90h, 1.08h, _Pulse));
                half3 color = lerp(_AuraColor.rgb, _OuterColor.rgb, noise * 0.45h + rim * 0.25h);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
