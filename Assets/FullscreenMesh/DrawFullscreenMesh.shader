Shader "Universal Render Pipeline/DrawFullscreenMesh"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        
        ZTest Always
        ZWrite On
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
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS               : SV_POSITION;
                float2 uv                       : TEXCOORD0;
                float4 color                    : COLOR;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = float4(input.positionOS.xyz, 1.0f);
                output.uv = input.uv;
                output.color = float4(1,1,0,1);
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                half4 col = Gamma22ToLinear(input.color);
                return col;
            }
            ENDHLSL
        }
    }
}
