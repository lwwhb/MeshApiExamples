using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FullscreenMeshRenderFeature : ScriptableRendererFeature
{
    public Material passMaterial;
    public ComputeShader computeShader;
    class FullscreenMeshRenderPass : ScriptableRenderPass
    {
        private const int kTileSize = 16;       //如果修改，还需同时修改对应Shader中的预定义，就不做变量传了
        
        [GenerateHLSL(needAccessors = false)]
        struct TileInfo
        {
            public float maxGradiantSqSum;
            public Vector2Int maxGradiantSqSumPixelIdx;
        }
        
        private Mesh fullscreenMesh { get; set; }
        
        private GraphicsBuffer vertexPosBuffer;
        private GraphicsBuffer vertexUVBuffer;
        
        
        private Material drawFullscreenMeshMaterial;
        private ComputeShader computeShader;

        private int tileNumX;
        private int tileNumY;

        private RenderTexture debugComputeOutput = null;
        private RenderTexture debugVertexOutput = null;
        

        public FullscreenMeshRenderPass( Material passMaterial, ComputeShader shader )
        {
            profilingSampler = new ProfilingSampler(nameof(FullscreenMeshRenderPass));
            drawFullscreenMeshMaterial = passMaterial;
            computeShader = shader;
        }
        
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            CameraData cameraData = renderingData.cameraData;
            if (fullscreenMesh == null)
            {
                fullscreenMesh = CreateFullscreenMesh(cameraData.camera.pixelWidth, cameraData.camera.pixelHeight);
            }

            if (debugComputeOutput == null)
            {
                debugComputeOutput = new RenderTexture(cameraData.camera.pixelWidth, cameraData.camera.pixelHeight, 24);
                debugComputeOutput.enableRandomWrite = true;
                debugComputeOutput.Create();
            }

            if (debugVertexOutput == null)
            {
                debugVertexOutput = new RenderTexture(cameraData.camera.pixelWidth/kTileSize + 1, cameraData.camera.pixelHeight/kTileSize + 1, 24);
                debugVertexOutput.enableRandomWrite = true;
                debugVertexOutput.filterMode = FilterMode.Point;
                debugVertexOutput.Create();
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
                UpdateMeshWithGPU(cmd, cameraData);
                cmd.Blit(debugVertexOutput, cameraData.renderer.cameraColorTargetHandle);
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
            if (debugComputeOutput)
            {
                DestroyImmediate(debugComputeOutput);
                debugComputeOutput = null;
            }

            if (debugVertexOutput)
            {
                DestroyImmediate(debugVertexOutput);
                debugVertexOutput = null;
            }
            
            vertexPosBuffer?.Dispose();
            vertexPosBuffer = null;
            vertexUVBuffer?.Dispose();
            vertexUVBuffer = null;
        }
        
        private Mesh CreateFullscreenMesh(int width, int height)
        {
            Debug.Assert((width % kTileSize == 0) && (height % kTileSize == 0));
            tileNumX = width / kTileSize;
            tileNumY = height / kTileSize;
            int vertexCount = (tileNumX + 1) * (tileNumY + 1);
            int indexCount = tileNumX * tileNumY * 6;
            Mesh mesh = new Mesh{ name = "Fullscreen Mesh" };
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            mesh.SetVertexBufferParams(vertexCount, 
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, stream:0),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, stream:1));
            
            mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
            
            var indexBuffer = new NativeArray<int>(indexCount, Allocator.Temp);
            int index = 0;
            for (int j = 0; j < tileNumY; j++)
            {
                for (int i = 0; i < tileNumX; i++)
                {
                    indexBuffer[index++] = j * (tileNumX + 1) + i;
                    indexBuffer[index++] = j * (tileNumX + 1) + (i + 1);
                    indexBuffer[index++] = (j + 1) * (tileNumX + 1) + i;

                    indexBuffer[index++] = (j + 1) * (tileNumX + 1) + i;
                    indexBuffer[index++] = j * (tileNumX + 1) + (i + 1);
                    indexBuffer[index++] = (j + 1) * (tileNumX + 1) + (i + 1);
                }
            }
            mesh.SetIndexBufferData(indexBuffer, 0, 0, indexBuffer.Length);
            indexBuffer.Dispose();
                
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount, MeshTopology.LineStrip));
            mesh.subMeshCount = 1;
            return mesh;
        }

        private void UpdateMeshWithGPU(CommandBuffer cmd, CameraData cameraData)
        {
            if (debugComputeOutput && computeShader)
            {
                RTHandle depthRT = cameraData.renderer.cameraDepthTargetHandle;
                var gpuP = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(), true);
                var gpuV = cameraData.GetViewMatrix();
                var gpuVP = gpuP * gpuV;
                
                cmd.SetComputeMatrixParam(computeShader, "gGpuVP", gpuVP);
                cmd.SetComputeVectorParam(computeShader,"gTilesInfo", new Vector4(tileNumX, tileNumY, tileNumX+1, tileNumY+1));
                
                int framePredictionKernelHandle = computeShader.FindKernel("FramePrediction");
                vertexPosBuffer ??= fullscreenMesh.GetVertexBuffer(0);
                vertexUVBuffer ??= fullscreenMesh.GetVertexBuffer(1);
                cmd.SetComputeTextureParam(computeShader, framePredictionKernelHandle, "_CameraDepthTexture", depthRT);
                cmd.SetComputeTextureParam(computeShader, framePredictionKernelHandle, "GradiantTexture", debugComputeOutput);
                cmd.SetComputeTextureParam(computeShader, framePredictionKernelHandle, "VertexTexture", debugVertexOutput);
                cmd.SetComputeBufferParam(computeShader, framePredictionKernelHandle, "VertexPosBuffer", vertexPosBuffer);
                cmd.SetComputeBufferParam(computeShader, framePredictionKernelHandle, "VertexUVBuffer", vertexUVBuffer);
                cmd.DispatchCompute(computeShader, framePredictionKernelHandle, (tileNumX+1),
                    (tileNumY+1), 1);
            }
        }
    }

    FullscreenMeshRenderPass m_ScriptablePass;
    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new FullscreenMeshRenderPass(passMaterial, computeShader);

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


