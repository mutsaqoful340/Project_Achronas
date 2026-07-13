Shader "SANKAATI/BloodSplatter"
{
    Properties
    {
        _MainTex        ("Blood Mask (RGB=shape, A=alpha)", 2D) = "white" {}
        _NoiseTex       ("Blood Noise (R=dissolve)", 2D) = "white" {}
        _Color          ("Blood Color", Color) = (0.55, 0.02, 0.02, 1)
        _ColorDark      ("Blood Dark Color", Color) = (0.2, 0.0, 0.0, 1)
        _Dissolve       ("Dissolve Amount", Range(0, 1)) = 0.0
        _DissolveEdge   ("Dissolve Edge Width", Range(0, 0.2)) = 0.05
        _EdgeColor      ("Dissolve Edge Color", Color) = (0.8, 0.05, 0.05, 1)
        _NoiseScale     ("Noise UV Scale", Range(0.5, 4)) = 1.5
        _Wetness        ("Wetness (specularity)", Range(0, 1)) = 0.6
        _Opacity        ("Overall Opacity", Range(0, 1)) = 1.0

        // Particle System built-in
        [Toggle] _UseParticleColor ("Use Particle Vertex Color", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "BloodSplatter"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;       // particle vertex color + alpha
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
                float  fogFactor   : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);  SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                half4  _Color;
                half4  _ColorDark;
                half4  _EdgeColor;
                half   _Dissolve;
                half   _DissolveEdge;
                half   _NoiseScale;
                half   _Wetness;
                half   _Opacity;
                half   _UseParticleColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color;
                OUT.fogFactor   = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // --- Mask texture (Blood_Mask) ---
                half4 mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                // Green channel has the main splatter shape (brightest in your mask)
                half shape = mask.g;

                // --- Noise texture (Blood_Noise) ---
                float2 noiseUV = IN.uv * _NoiseScale;
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                // --- Dissolve ---
                // Combined dissolve field: noise drives organic edges
                half dissolveField = noise * 0.6 + shape * 0.4;
                half dissolveThreshold = _Dissolve;

                // Pixel alive if dissolveField > threshold
                half alive = step(dissolveThreshold, dissolveField);

                // Edge glow band just above the threshold
                half edgeBand = step(dissolveThreshold, dissolveField)
                              * (1.0 - step(dissolveThreshold + _DissolveEdge, dissolveField));

                // --- Color ---
                // Dark pooling at center (low shape value), bright red at splatter edges
                half3 bloodColor = lerp(_ColorDark.rgb, _Color.rgb, shape);

                // Wet specularity – simple fake highlight
                half wetHighlight = pow(saturate(shape), 8.0) * _Wetness * 0.4;
                bloodColor += wetHighlight;

                // Mix in edge dissolve color
                bloodColor = lerp(bloodColor, _EdgeColor.rgb, edgeBand * 0.8);

                // --- Alpha ---
                half alpha = shape * mask.a * alive * _Opacity;

                // Particle vertex color modulation
                half4 vertCol = lerp(half4(1,1,1,1), IN.color, _UseParticleColor);
                bloodColor *= vertCol.rgb;
                alpha      *= vertCol.a;

                // Fog
                half3 finalColor = MixFog(bloodColor, IN.fogFactor);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
