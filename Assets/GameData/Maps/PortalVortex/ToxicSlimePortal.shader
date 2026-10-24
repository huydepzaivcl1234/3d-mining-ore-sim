Shader "Mining Simulator/Toxic Slime Portal"
{
    Properties
    {
        _Open ("Open amount", Range(0, 1)) = 1
        _SwirlSpeed ("Swirl speed", Range(0.5, 6)) = 2.5
        _CellScale ("Bubble density", Range(4, 16)) = 8
        _RimSplash ("Rim splash", Range(0.01, 0.12)) = 0.05
        _Aspect ("Portal width / height", Range(0.25, 4)) = 1
        [HDR] _CoreColor ("Dark emerald core", Color) = (0.016, 0.09, 0.03, 1)
        [HDR] _SlimeColor ("Slime green", Color) = (0.17, 0.88, 0.07, 1)
        [HDR] _RimColor ("Radioactive rim", Color) = (1.0, 1.8, 0.01, 1)
        _Glow ("Glow strength", Range(0, 6)) = 2
        _TimeOffset ("Animation time offset", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "Toxic Slime Vortex"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float _Open;
                float _SwirlSpeed;
                float _CellScale;
                float _RimSplash;
                float _Aspect;
                float4 _CoreColor;
                float4 _SlimeColor;
                float4 _RimColor;
                float _Glow;
                float _TimeOffset;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // Moving cellular spots; fixed 3x3 search for predictable fragment cost.
            float VoronoiBubbles(float2 p, float t)
            {
                float2 cell = floor(p);
                float nearest = 10.0;
                [unroll] for (int y = -1; y <= 1; y++)
                {
                    [unroll] for (int x = -1; x <= 1; x++)
                    {
                        float2 id = cell + float2(x, y);
                        float2 seed = float2(Hash21(id), Hash21(id + 37.9));
                        float2 location = id + 0.5 + 0.32 * sin(t * float2(0.75, 1.14) + seed * 6.28318);
                        nearest = min(nearest, length(p - location));
                    }
                }
                return 1.0 - smoothstep(0.13, 0.52, nearest);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // On a square Quad, XY in UVs produces the same round portal as the canvas demo.
                // Match Aspect to the Quad's authored width/height for intentionally non-square Quads.
                float2 p = (input.uv - 0.5) * float2(max(_Aspect, 0.01), 1.0);
                float t = _Time.y * _SwirlSpeed + _TimeOffset;
                float radius = length(p);
                float angle = atan2(p.y, p.x);
                float open = saturate(_Open);
                float wave = sin(angle * 9.0 + t * 3.0) * 0.5
                           + sin(angle * 15.3 - t * 4.0) * 0.3
                           + cos(angle * 5.0 + t * 1.5) * 0.2;
                float border = 0.44 * open * (0.92 + wave * (_RimSplash * 2.0));
                float signedDistance = radius - border;
                float antialias = max(fwidth(signedDistance) * 1.5, 0.001);
                float inside = 1.0 - smoothstep(-antialias, antialias, signedDistance);

                float normRadius = radius / max(0.44 * open, 0.001);
                float3 interior = lerp(_CoreColor.rgb, _SlimeColor.rgb,
                    smoothstep(0.08, 0.72, normRadius));
                interior = lerp(interior, _RimColor.rgb * 0.72,
                    smoothstep(0.76, 1.0, normRadius));

                // Four rotating Archimedean spiral arms: angle = offset + normalizedRadius * 6.
                float spiral = angle - normRadius * 6.0 + t * 1.2;
                float arm = pow(saturate(0.5 + 0.5 * cos(spiral * 4.0)), 7.0);
                interior += _RimColor.rgb * arm * 0.57 * saturate(normRadius * 2.5);
                float shade = pow(saturate(0.5 + 0.5 * cos(spiral * 4.0 + 3.14159)), 9.0);
                interior *= 1.0 - shade * 0.43;

                float2 cellsUv = p * (_CellScale * 2.2);
                float cells = VoronoiBubbles(cellsUv, t * 0.42);
                float bubbleZone = smoothstep(0.17, 0.33, normRadius) *
                                   (1.0 - smoothstep(0.83, 0.98, normRadius));
                interior += _RimColor.rgb * cells * bubbleZone * 0.60;

                float rim = exp2(-abs(signedDistance) / max(antialias * 1.5, 0.006));
                float halo = exp2(-abs(signedDistance) / 0.036);
                float3 color = interior * inside + _RimColor.rgb * (rim * _Glow + halo * _Glow * 0.14);
                float alpha = max(inside, saturate(rim * 0.95 + halo * 0.30));

                // Orbiting droplets, independent of mesh/particle setup.
                [unroll] for (int i = 0; i < 12; i++)
                {
                    float seed = Hash21(float2(i, 9.3));
                    float theta = 6.2831853 * (i / 12.0) + t * (0.42 + seed * 0.28);
                    float dist = border * (1.02 + 0.16 * Hash21(float2(i, 2.7)));
                    float2 droplet = dist * float2(cos(theta), sin(theta));
                    float size = (0.003 + 0.004 * Hash21(float2(i, 4.1))) * open;
                    float d = length(p - droplet);
                    float core = 1.0 - smoothstep(size * 0.5, size + antialias, d);
                    float softGlow = exp2(-d / max(size * 3.5, 0.001));
                    color += _RimColor.rgb * (core * 1.3 + softGlow * 0.17) * _Glow;
                    alpha = max(alpha, saturate(core + softGlow * 0.18));
                }

                return float4(color, alpha * smoothstep(0.0, 0.02, open));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
