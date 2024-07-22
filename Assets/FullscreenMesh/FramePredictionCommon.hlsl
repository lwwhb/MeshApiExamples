#ifndef FRAMEPREDICTION_COMMON_INCLUDED
#define FRAMEPREDICTION_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#define TILE_SIZE 16    //临时这么写，未来进变量
#define EPSILON 0.00001

RWTexture2D<float4> GradiantSqSumTexture;
RWTexture2D<float4> VertexTexture;

RWByteAddressBuffer VertexPosBuffer;
RWByteAddressBuffer VertexUVBuffer;

float4 gTilesInfo;  // tileNumX, tileNumY, tileNumX+1, tileNumY+1
matrix gGpuVP;

uint CalculateVertexID(uint2 vertexIdx2D)
{
    return vertexIdx2D.y * gTilesInfo.z + vertexIdx2D.x;
}

float4 CalculateVertexClipPosAndUV(uint2 vertexIdx2D)
{
    float2 uv = vertexIdx2D*TILE_SIZE*_ScreenSize.zw;
    #if UNITY_UV_STARTS_AT_TOP
        uv = uv * float2(1.0, -1.0) + float2(0.0, 1.0);
    #endif
    return float4(uv * 2.0f - 1.0f, uv);
}

float4 CalculatePixelClipPosAndUV(uint2 pixelIdx2D)
{
    float2 uv = pixelIdx2D *_ScreenSize.zw;
    #if UNITY_UV_STARTS_AT_TOP
        uv = uv * float2(1.0, -1.0) + float2(0.0, 1.0);
    #endif
    return float4(uv * 2.0f - 1.0f, uv);
}
void StoreVertexPos(int index, float3 pos)
{
    uint3 data = asuint(pos);
    VertexPosBuffer.Store3((index*3)<<2, data);
}
void StoreVertexUV(int index, float2 uv)
{
    uint2 data = asuint(uv);
    VertexUVBuffer.Store2((index*2)<<2, data);
}

#endif // FRAMEPREDICTION_COMMON_INCLUDED