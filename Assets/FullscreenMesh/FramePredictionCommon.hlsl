#ifndef FRAMEPREDICTION_COMMON_INCLUDED
#define FRAMEPREDICTION_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#define TILE_SIZE 16    //临时这么写，未来进变量

struct Pixel
{
    uint2 pos;
    float2 uv;
    float gradiant;
};
struct Tile
{
    uint2 maxGradiantPixel;
};

RWStructuredBuffer<uint2> CacheUAV;

uint2 CalcuateVertexIndex2D(uint vertexID)
{
    uint2 tilesDim = (uint2)ceil(_ScreenSize.xy / TILE_SIZE);
    uint2 vertexDim = tilesDim + 1;
    return uint2(vertexID % vertexDim.x, vertexID / vertexDim.x);
}
uint CalculateVertexID(uint2 vertexIdx2D)
{
    return vertexIdx2D.y * TILE_SIZE + vertexIdx2D.x;
}

float4 CalculateVertexClipPos(uint2 vertexIdx2D)
{
    float4 clipPos = float4(vertexIdx2D*TILE_SIZE*_ScreenSize.zw * 2.0f - 1.0f, UNITY_NEAR_CLIP_VALUE, 1.0f);
    #ifdef UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION
        clipPos = ApplyPretransformRotation(pos);
    #endif
    return clipPos;
}
float2 CalculateVertexUV(float2 clipPos)
{
    return clipPos * 0.5f + 0.5f;
}

#endif // FRAMEPREDICTION_COMMON_INCLUDED