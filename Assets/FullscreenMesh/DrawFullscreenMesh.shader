Shader "Universal Render Pipeline/DrawFullscreenMesh"
{
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

            #include "FramePredictionCommon.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS               : SV_POSITION;
                float2 uv                       : TEXCOORD0;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                uint2 vertexIdx = CalcuateVertexIndex2D(input.vertexID);
                output.positionCS = CalculateVertexClipPos(vertexIdx);
                output.uv = CalculateVertexUV(output.positionCS.xy);

                #if UNITY_UV_STARTS_AT_TOP
                    output.uv = output.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
                #endif

                if(vertexIdx.x == 0)
                {
                    output.positionCS.x = -1;
                    output.uv.x = 0;
                }
                if(vertexIdx.y == 0)
                {
                    output.positionCS.y = -1;
                    output.uv.y = 0;
                }
                if(vertexIdx.x == _ScreenSize.x)
                {
                    output.positionCS.x = 1;
                    output.uv.x = 1;
                }
                if(vertexIdx.y == _ScreenSize.y)
                {
                    output.positionCS.y = 1;
                    output.uv.y = 1;
                }

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
