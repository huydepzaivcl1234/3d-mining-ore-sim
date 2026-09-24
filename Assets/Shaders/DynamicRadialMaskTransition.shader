Shader "UI/Mining/DynamicRadialMaskTransition"
{
    Properties
    {
        _Center ("Portal viewport center", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Animated radius (0 to 2.8)", Range(0, 2.8)) = 0
        _Phase ("0 cover / 1 reveal", Float) = 0
        _MaskColor ("Mask color", Color) = (0.035, 0.012, 0.055, 1)
        [HDR] _EdgeColor ("Energy color (HDR)", Color) = (1.5, 0.68, 0.08, 1)
        _EdgeWidth ("Edge width", Range(0.002, 0.15)) = 0.025
        _EdgeGlow ("Edge glow", Range(0, 8)) = 3.5
        _NoiseFreq ("Ripple frequency", Range(1, 40)) = 12
        _NoiseAmplitude ("Ripple amplitude", Range(0, 0.08)) = 0.022
        _NoiseSpeed ("Ripple speed", Range(0, 10)) = 3
        _EffectTime ("Time (unscaled)", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Radial UI"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Center;
                float4 _MaskColor;
                float4 _EdgeColor;
                float _Radius;
                float _Phase;
                float _EdgeWidth;
                float _EdgeGlow;
                float _NoiseFreq;
                float _NoiseAmplitude;
                float _NoiseSpeed;
                float _EffectTime;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 center = saturate(_Center.xy);
                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float2 delta = (input.uv - center) * float2(aspect, 1.0);
                float2 farCorner = max(center, 1.0 - center) * float2(aspect, 1.0);
                // 1.68 is the farthest corner: 1.75 leaves clearance for antialiasing/ripples.
                float distanceToCenter = length(delta) / max(length(farCorner), 0.001) * 1.68;

                float reveal = step(0.5, _Phase);
                float front = lerp(_Radius,
                    saturate((_Radius - 1.75) / (2.8 - 1.75)) * 1.75,
                    reveal);

                float angle = atan2(delta.y, delta.x);
                float t = _EffectTime * _NoiseSpeed;
                float ripple = sin(angle * _NoiseFreq + t) * 0.58
                    + sin(angle * _NoiseFreq * 1.73 - t * 1.37) * 0.29
                    + sin(angle * _NoiseFreq * 3.07 + t * 1.91) * 0.13;
                // Fade distortion at the ends so the cover really becomes opaque
                // and the reveal really becomes transparent, even in the corners.
                float rippleFade = saturate(front / 0.10) * saturate((1.75 - front) / 0.12);
                float edge = front + ripple * _NoiseAmplitude * rippleFade;
                float width = max(_EdgeWidth, 0.002);
                float soft = max(fwidth(distanceToCenter) * 1.4, 0.002);
                float inside = 1.0 - smoothstep(edge - soft, edge + soft, distanceToCenter);
                float mask = lerp(inside, 1.0 - inside, reveal);

                float active = saturate(front / 0.04) * saturate((1.75 - front) / 0.055);
                float offset = distanceToCenter - edge;
                float rim = exp2(-abs(offset) / width * 2.3);
                float halo = exp2(-abs(offset) / (width * 3.7) * 1.6);
                float outer = exp2(-abs(offset - width * 3.1) / (width * 0.70) * 1.7);
                float wave = active * (rim + halo * 0.23 + outer * 0.45);

                float glowAlpha = saturate(active * (rim * 0.88 + halo * 0.23 + outer * 0.42));
                float alpha = saturate(max(mask * _MaskColor.a, glowAlpha)) * input.color.a;
                float mixEnergy = saturate(active * (rim + halo * 0.25 + outer * 0.55));
                float3 rgb = lerp(_MaskColor.rgb,
                    _EdgeColor.rgb * _EdgeGlow * max(wave, 0.0), mixEnergy);
                return half4(rgb * input.color.rgb, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
