Shader "Custom/StylizedLavaFloor_URP"
{
    Properties
    {
        [Header(Obsidian_Crust)] 
        _CrustColor ("Crust Rock Tint (Obsidian / Basalt)", Color) = (0.08, 0.07, 0.09, 1.0)
        _CrustSmoothness ("Crust Smoothness", Range(0.0, 1.0)) = 0.2
        _CrustTiling ("Crust Tiling Scale", Float) = 0.35

        [Header(Molten_Magma_Veins)] 
        [HDR] _LavaHotColor ("Magma Hot Core Color", Color) = (2.5, 1.6, 0.1, 1.0) // Bright Electric Yellow-Orange HDR
        [HDR] _LavaMidColor ("Magma Flow Color", Color) = (2.0, 0.35, 0.02, 1.0) // Fiery Orange-Red HDR
        _LavaCoolColor ("Magma Crust Edge Color", Color) = (0.4, 0.05, 0.02, 1.0) // Dark Charred Crimson

        [Header(Magma Flow and Pulse)]
        _FlowSpeed ("Lava Flow Speed", Float) = 0.4
        _FlowTiling ("Lava Texture Tiling", Float) = 0.5
        _LavaPulseSpeed ("Heat Breathing Pulse Speed", Float) = 2.0
        _LavaPulseIntensity ("Heat Breathing Pulse Strength", Range(0.0, 1.0)) = 0.25

        [Header(Cracked Vein Mask)]
        _CrackDensity ("Magma Crack Density", Range(0.1, 5.0)) = 1.8
        _CrackSharpness ("Magma Crack Sharpness", Range(1.0, 20.0)) = 8.0
        _LavaLevel ("Lava vs Crust Coverage Ratio", Range(0.0, 1.0)) = 0.42

        [Header(Emission and Bloom)]
        _EmissionIntensity ("HDR Bloom Multiplier", Range(1.0, 10.0)) = 4.0
        _LavaGlowThreshold ("Glow Threshold Offset", Range(0.0, 1.0)) = 0.15

        [Header(Heat Shimmer Undulation)]
        _WaveStrength ("Surface Heat Wave Undulation", Range(0.0, 0.1)) = 0.02
        _WaveSpeed ("Heat Wave Speed", Float) = 3.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _CrustColor;
                float _CrustSmoothness;
                float _CrustTiling;

                float4 _LavaHotColor;
                float4 _LavaMidColor;
                float4 _LavaCoolColor;

                float _FlowSpeed;
                float _FlowTiling;
                float _LavaPulseSpeed;
                float _LavaPulseIntensity;

                float _CrackDensity;
                float _CrackSharpness;
                float _LavaLevel;

                float _EmissionIntensity;
                float _LavaGlowThreshold;

                float _WaveStrength;
                float _WaveSpeed;
            CBUFFER_END

            // Procedural pseudo-random hash
            float2 Hash2D(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            // Voronoi Cellular Noise for volcanic rock cracks
            float VoronoiCracks(float2 uv)
            {
                float2 g = floor(uv);
                float2 f = frac(uv);
                float d1 = 1.0;
                float d2 = 1.0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 lattice = float2(x, y);
                        float2 offset = Hash2D(g + lattice);
                        float2 delta = lattice + offset - f;
                        float d = length(delta);

                        if (d < d1)
                        {
                            d2 = d1;
                            d1 = d;
                        }
                        else if (d < d2)
                        {
                            d2 = d;
                        }
                    }
                }
                // Border distance (creates rock crust cracks)
                return d2 - d1;
            }

            // Dual harmonic turbulence for flowing molten liquid
            float LavaTurbulence(float2 uv, float timeVal)
            {
                float2 flow1 = uv + float2(timeVal * 0.5, timeVal * 0.3);
                float2 flow2 = uv * 1.5 - float2(timeVal * 0.4, -timeVal * 0.6);

                float wave1 = sin(flow1.x * 6.0 + sin(flow1.y * 5.0));
                float wave2 = cos(flow2.x * 7.0 - cos(flow2.y * 6.0));
                return (wave1 + wave2) * 0.25 + 0.5;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                float3 posWS = vertexInputs.positionWS;

                // Subtle heat shimmer wave displacement on molten areas
                float wave = sin(posWS.x * 3.0 + _Time.y * _WaveSpeed) * cos(posWS.z * 3.0 + _Time.y * _WaveSpeed);
                posWS.y += wave * _WaveStrength;

                output.positionCS = TransformWorldToHClip(posWS);
                output.positionWS = posWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // World-space UV mapping for perfectly seamless tiling across all floor blocks
                float2 worldUV = input.positionWS.xz;

                // 1. Calculate Volcanic Rock Cracked Crust Mask
                float crackDistance = VoronoiCracks(worldUV * _CrackDensity);
                float crackMask = smoothstep(_LavaLevel - 0.1, _LavaLevel + 0.1, crackDistance);

                // 2. Churning Molten Lava Flow
                float timeFlow = _Time.y * _FlowSpeed;
                float turbulence = LavaTurbulence(worldUV * _FlowTiling, timeFlow);

                // Breathing heat pulse
                float pulse = 1.0 + sin(_Time.y * _LavaPulseSpeed) * _LavaPulseIntensity;

                // 3. Magma Color Gradient (Core -> Mid -> Edge)
                float heatIntensity = saturate(turbulence * (1.0 - crackMask * 0.8));
                half3 lavaColor = lerp(_LavaCoolColor.rgb, _LavaMidColor.rgb, smoothstep(0.1, 0.6, heatIntensity));
                lavaColor = lerp(lavaColor, _LavaHotColor.rgb * pulse, smoothstep(0.6, 0.95, heatIntensity));

                // 4. Blend Obsidian Rock Crust with Glowing Magma Cracks
                half3 finalAlbedo = lerp(lavaColor, _CrustColor.rgb, crackMask);

                // 5. Lighting and Shadow Calculations (URP Main Light)
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = saturate(dot(normalize(input.normalWS), lightDir));
                half3 diffuseLighting = mainLight.color * (NdotL * mainLight.shadowAttenuation + 0.25);

                // 6. HDR Emission (Magma illuminates itself regardless of shadows)
                float isLava = 1.0 - crackMask;
                half3 emission = lavaColor * isLava * _EmissionIntensity;

                // Combine diffuse lighting on rocks + self-illuminated glowing lava
                half3 finalColor = (finalAlbedo * diffuseLighting) + emission;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
