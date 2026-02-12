Shader "Custom/OutlinePostProcess"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        
        Pass
        {
            Name "OutlinePass"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineThickness;
                float _DepthSensitivity;
                float _NormalSensitivity;
            CBUFFER_END
            
            // Sobel edge detection
            float SampleDepth(float2 uv)
            {
                return SampleSceneDepth(uv);
            }
            
            float3 SampleNormal(float2 uv)
            {
                return SampleSceneNormals(uv);
            }
            
            float DepthEdge(float2 uv, float2 offset)
            {
                float depth = SampleDepth(uv);
                
                // Sobel kernel for depth
                float d1 = SampleDepth(uv + float2(-offset.x, -offset.y));
                float d2 = SampleDepth(uv + float2(0, -offset.y));
                float d3 = SampleDepth(uv + float2(offset.x, -offset.y));
                float d4 = SampleDepth(uv + float2(-offset.x, 0));
                float d6 = SampleDepth(uv + float2(offset.x, 0));
                float d7 = SampleDepth(uv + float2(-offset.x, offset.y));
                float d8 = SampleDepth(uv + float2(0, offset.y));
                float d9 = SampleDepth(uv + float2(offset.x, offset.y));
                
                float sobelX = d3 + 2.0 * d6 + d9 - d1 - 2.0 * d4 - d7;
                float sobelY = d1 + 2.0 * d2 + d3 - d7 - 2.0 * d8 - d9;
                
                float depthEdge = sqrt(sobelX * sobelX + sobelY * sobelY);
                return depthEdge * _DepthSensitivity;
            }
            
            float NormalEdge(float2 uv, float2 offset)
            {
                float3 normal = SampleNormal(uv);
                
                // Sobel kernel for normals
                float3 n1 = SampleNormal(uv + float2(-offset.x, -offset.y));
                float3 n2 = SampleNormal(uv + float2(0, -offset.y));
                float3 n3 = SampleNormal(uv + float2(offset.x, -offset.y));
                float3 n4 = SampleNormal(uv + float2(-offset.x, 0));
                float3 n6 = SampleNormal(uv + float2(offset.x, 0));
                float3 n7 = SampleNormal(uv + float2(-offset.x, offset.y));
                float3 n8 = SampleNormal(uv + float2(0, offset.y));
                float3 n9 = SampleNormal(uv + float2(offset.x, offset.y));
                
                float3 sobelX = n3 + 2.0 * n6 + n9 - n1 - 2.0 * n4 - n7;
                float3 sobelY = n1 + 2.0 * n2 + n3 - n7 - 2.0 * n8 - n9;
                
                float normalEdge = length(sobelX) + length(sobelY);
                return normalEdge * _NormalSensitivity;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                
                // Sample the scene color
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                
                // If no processing needed, return early for debugging
                // return sceneColor;
                
                float2 offset = _OutlineThickness * _ScreenParams.zw - 1.0;
                
                float depthEdge = DepthEdge(uv, offset);
                float normalEdge = NormalEdge(uv, offset);
                
                float edge = saturate(depthEdge + normalEdge);
                
                half4 outlineColor = lerp(sceneColor, _OutlineColor, edge);
                return outlineColor;
            }
            ENDHLSL
        }
    }
}
