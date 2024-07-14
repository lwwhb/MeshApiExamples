using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class FullscreenMesh : MonoBehaviour
{
    private const int kTileSize = 8;
    private Mesh fullscreenMesh { get; set; }
    private GraphicsBuffer bufferPos;
    private GraphicsBuffer bufferUV;
    
    // Start is called before the first frame update
    void Start()
    {
        if (fullscreenMesh == null)
        {
            float nearClipZ = -1;
            if (SystemInfo.usesReversedZBuffer)
                nearClipZ = 1;
            fullscreenMesh = CreateFullscreenMesh(Camera.main.pixelWidth, Camera.main.pixelHeight,
                nearClipZ);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDestroy()
    {
        if (fullscreenMesh)
        {
            DestroyImmediate(fullscreenMesh);
            fullscreenMesh = null;
        }
        bufferPos?.Dispose();
        bufferPos = null;
        bufferUV?.Dispose();
        bufferUV = null;
    }
    private Mesh CreateFullscreenMesh(int width, int height, float nearZ)
    {
        Debug.Assert((width % kTileSize == 0) && (height % kTileSize == 0));
        int uCount = width / kTileSize;
        int vCount = height / kTileSize;
        int vertexCount = (uCount + 1) * (vCount + 1);
        int indexCount = uCount * vCount * 6;
        Mesh mesh = new Mesh{ name = "Fullscreen Mesh" };
        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            
        mesh.SetVertexBufferParams(vertexCount, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, stream:0), 
                                                                    new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, stream:1));
        var posBuffer = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
        var uvBuffer = new NativeArray<Vector2>(vertexCount, Allocator.Temp);
        for (int j = 0; j < vCount + 1; j++)
        {
            for (int i = 0; i < uCount + 1; i++)
            {
                posBuffer[j * (uCount + 1) + i] = new Vector3((i*kTileSize),  - (j*kTileSize), nearZ);
                uvBuffer[j * (uCount + 1) + i] = new Vector2((i*kTileSize)/(float)width, (j*kTileSize)/(float)height);
            }
        }
            
        mesh.SetVertexBufferData(posBuffer, 0, 0, posBuffer.Length, stream:0);
        mesh.SetVertexBufferData(uvBuffer, 0, 0, uvBuffer.Length, stream:1);
        posBuffer.Dispose();
        uvBuffer.Dispose();
            
        mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
        var indexBuffer = new NativeArray<int>(indexCount, Allocator.Temp);
        int index = 0;
        for (int j = 0; j < vCount; j++)
        {
            for (int i = 0; i < uCount; i++)
            {
                indexBuffer[index++] = j * (uCount + 1) + i;
                indexBuffer[index++] = j * (uCount + 1) + (i + 1);
                indexBuffer[index++] = (j + 1) * (uCount + 1) + i;

                indexBuffer[index++] = (j + 1) * (uCount + 1) + i;
                indexBuffer[index++] = j * (uCount + 1) + (i + 1);
                indexBuffer[index++] = (j + 1) * (uCount + 1) + (i + 1);
            }
        }
        mesh.SetIndexBufferData(indexBuffer, 0, 0, indexBuffer.Length);
        indexBuffer.Dispose();
            
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles));
        mesh.subMeshCount = 1;

        GetComponent<MeshFilter>().sharedMesh = mesh;
        return mesh;
    }
}
