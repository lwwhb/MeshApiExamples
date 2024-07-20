#ifndef FRAMEPREDICTION_COMMON_INCLUDED
#define FRAMEPREDICTION_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#include "FullscreenMeshRenderFeature.cs.hlsl"

#define TILE_SIZE 16    //临时这么写，未来进变量
#define EPSILON 0.00001

RWTexture2D<float4> GradiantTexture;
RWStructuredBuffer<TileInfo> BufTileInfos;

RWByteAddressBuffer VertexPosBuffer;
RWByteAddressBuffer VertexUVBuffer;

RWTexture2D<float4> VertexTexture;

float4 gTilesInfo;  // tileNumX, tileNumY, tileNumX+1, tileNumY+1

uint2 CalcuateVertexIndex2D(uint vertexID)
{
    return uint2(vertexID % gTilesInfo.z, vertexID / gTilesInfo.z);
}
uint CalculateVertexID(uint2 vertexIdx2D)
{
    return vertexIdx2D.y * gTilesInfo.z + vertexIdx2D.x;
}

float4 CalculateVertexClipPosAndUV(uint2 vertexIdx2D)
{
    float2 uv = vertexIdx2D*TILE_SIZE*_ScreenSize.zw;
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