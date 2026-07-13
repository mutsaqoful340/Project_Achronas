Shader "Particles/Blood Effect"
{
    Properties
    {
        [Header(Color Controls)]
        [HDR] _BaseColor ("Base Color Mult", Color) = (1,1,1,1)
        _AlphaMin ("Alpha Clip Min", Range(-0.01,1.01)) = 0.1
        _AlphaSoft ("Alpha Clip Softness", Range(0,1)) = 0.022
        _EdgeDarken ("Edge Darkening", Float) = 1.0
        _ProcMask ("Procedural Mask Strength", Float) = 1.0

        [Header(Mask Controls)]
        _MainTex ("Mask Texture", 2D) = "white" {}
        _MaskStr ("Mask Strength", Float) = 0.7
        _Columns ("Flipbook Columns", Int) = 1
        _Rows ("Flipbook Rows", Int) = 1
        _ChannelMask ("Channel Mask", Vector) = (1,0,0,0)

        [Header(Noise Controls)]
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseAlphaStr ("Noise Strength", Float) = 0.8
        _ChannelMask2 ("Channel Mask", Vector) = (1,0,0,0)

        [Header(Vertex Physics)]
        _FallOffset ("Gravity Offset", Range(-1,0)) = -1
        _FallRandomness ("Gravity Randomness", Float) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            float4 _BaseColor;
            float _AlphaMin;
            float _AlphaSoft;
            float _EdgeDarken;
            float _ProcMask;

            float _MaskStr;
            float _Columns;
            float _Rows;
            float4 _ChannelMask;

            float _NoiseAlphaStr;
            float4 _ChannelMask2;

            float _FallOffset;
            float _FallRandomness;

            float4 _MainTex_ST;
            float4 _NoiseTex_ST;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 noiseUV : TEXCOORD1;
                float4 color : COLOR;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;

                float lifetime = v.uv.w;
                lifetime = lifetime * lifetime + (_FallOffset + ((v.uv.z - 0.5) * _FallRandomness)) * lifetime;

                float3 pos = v.positionOS.xyz;
                pos.y += lifetime;

                o.positionHCS = TransformObjectToHClip(pos);
                o.uv = TRANSFORM_TEX(v.uv.xy, _MainTex);
                o.noiseUV = TRANSFORM_TEX(v.uv.xy, _NoiseTex);

                o.color = v.color;
                o.color.a *= o.color.a;
                o.color.a += _AlphaMin;

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                mask = saturate(lerp(1, mask, _MaskStr));

                half alphaMask = saturate(dot(mask, _ChannelMask));

                half4 noiseTex = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.noiseUV);
                half noise = saturate(dot(noiseTex, _ChannelMask2));
                noise = saturate(lerp(1, noise, _NoiseAlphaStr));

                half alpha = alphaMask * noise * i.color.a;

                half clippedAlpha = saturate((alpha - _AlphaMin) / max(_AlphaSoft, 0.001));

                half edge = 1 - saturate(alpha * clippedAlpha);
                edge *= edge;
                edge = 1 - edge;
                edge = saturate(lerp(0.71, edge * edge, _EdgeDarken));

                half4 col = _BaseColor * i.color;
                col.a = clippedAlpha;
                col.rgb *= edge;

                return col;
            }

            ENDHLSL
        }
    }
}