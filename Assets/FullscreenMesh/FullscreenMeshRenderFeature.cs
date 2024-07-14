using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FullscreenMeshRenderFeature : ScriptableRendererFeature
{
    public Material passMaterial;
    class FullscreenMeshRenderPass : ScriptableRenderPass
    {
        private const int kTileSize = 16;
        private Mesh fullscreenMesh { get; set; }
        private GraphicsBuffer bufferPos;
        
        public Material drawFullscreenMeshMaterial;

        public FullscreenMeshRenderPass( Material passMaterial )
        {
            profilingSampler = new ProfilingSampler(nameof(FullscreenMeshRenderPass));
            drawFullscreenMeshMaterial = passMaterial;
            
        }
        
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (fullscreenMesh == null)
            {
                CameraData cameraData = renderingData.cameraData;
                float nearClipZ = -1;
                if (SystemInfo.usesReversedZBuffer)
                    nearClipZ = 1;
                fullscreenMesh = CreateFullscreenMesh(cameraData.camera.pixelWidth, cameraData.camera.pixelHeight,
                    nearClipZ);
            }
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler))
            {
                CoreUtils.SetRenderTarget(cmd, cameraData.renderer.cameraColorTargetHandle);

                cmd.DrawMesh(fullscreenMesh, Matrix4x4.identity, drawFullscreenMeshMaterial);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
        
        public void Dispose()
        {
            if (fullscreenMesh)
            {
                DestroyImmediate(fullscreenMesh);
                fullscreenMesh = null;
            }
            bufferPos?.Dispose();
            bufferPos = null;
        }
        
        private Mesh CreateFullscreenMesh(int width, int height, float nearZ)
        {
            Debug.Assert((width % kTileSize == 0) && (height % kTileSize == 0));
            int uCount = Mathf.CeilToInt((float)width / kTileSize);
            int vCount = Mathf.CeilToInt((float)height / kTileSize);
            int vertexCount = (uCount + 1) * (vCount + 1);
            int indexCount = uCount * vCount * 6;
            Mesh mesh = new Mesh{ name = "Fullscreen Mesh" };
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                
            mesh.SetVertexBufferParams(vertexCount, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, stream:0));
            var posBuffer = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
            var uvBuffer = new NativeArray<Vector2>(vertexCount, Allocator.Temp);
            for (int j = 0; j < vCount + 1; j++)
            {
                for (int i = 0; i < uCount + 1; i++)
                {
                    posBuffer[j * (uCount + 1) + i] = new Vector3((i*kTileSize)/(float)width - 0.5f, (j*kTileSize)/(float)height - 0.5f , nearZ);
                }
            }
                
            mesh.SetVertexBufferData(posBuffer, 0, 0, posBuffer.Length, stream:0);
            posBuffer.Dispose();
                
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
                
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount, MeshTopology.LineStrip));
            mesh.subMeshCount = 1;
            return mesh;
        }
    }

    FullscreenMeshRenderPass m_ScriptablePass;
    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new FullscreenMeshRenderPass(passMaterial);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (passMaterial == null)
        {
            Debug.LogWarningFormat("The full screen mesh render feature \"{0}\" will not execute - no material is assigned. Please make sure a material is assigned for this feature on the renderer asset.", name);
            return;
        }
        
        renderer.EnqueuePass(m_ScriptablePass);
    }
    
    protected override void Dispose(bool disposing)
    {
        m_ScriptablePass.Dispose();
    }
}


