Shader "Universal Render Pipeline/DrawFullscreenMesh"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        
        ZTest Always
        ZWrite True
        Cull Off
        
        Pass
        {
            Name "DrawFullscreenMesh"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS               : SV_POSITION;
                float2 uv                       : TEXCOORD0;
            };

            float2 TransformTriangleVertexToUV(float2 vertex)
            {
                float2 uv = vertex*2.0f + 1.0f;
                return vertex;
            }
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = float4(input.positionOS.xy, 0.0f, 0.5f);
                output.uv = TransformTriangleVertexToUV(input.positionOS.xy);

                #if UNITY_UV_STARTS_AT_TOP
                    output.uv = output.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
                #endif
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                //half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 col = Gamma22ToLinear(half3(1, 1, 0));
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
