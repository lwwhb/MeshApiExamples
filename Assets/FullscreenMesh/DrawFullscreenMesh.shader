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
                #if UNITY_UV_STARTS_AT_TOP
                    output.uv = output.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
                #endif
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
