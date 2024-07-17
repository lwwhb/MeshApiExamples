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
        private Mesh fullscreenMesh { get; set; }
        private GraphicsBuffer bufferVertexPos;
        
        private Material drawFullscreenMeshMaterial;
        private ComputeShader computeShader;

        private int tileNumX;
        private int tileNumY;

        private RenderTexture debugComputeOutput = null;
        

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
                //UpdateMeshWithGPU(cmd, cameraData.renderer.cameraDepthTargetHandle);
                //cmd.Blit(debugComputeOutput, cameraData.renderer.cameraColorTargetHandle);
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
            bufferVertexPos?.Dispose();
            bufferVertexPos = null;
        }
        
        private Mesh CreateFullscreenMesh(int width, int height)
        {
            Debug.Assert((width % kTileSize == 0) && (height % kTileSize == 0));
            tileNumX = Mathf.CeilToInt((float)width / kTileSize);
            tileNumY = Mathf.CeilToInt((float)height / kTileSize);
            int vertexCount = (tileNumX + 1) * (tileNumY + 1);
            int indexCount = tileNumX * tileNumY * 6;
            Mesh mesh = new Mesh{ name = "Fullscreen Mesh" };
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                
            mesh.SetVertexBufferParams(vertexCount, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, stream:0));
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

        private void UpdateMeshWithGPU(CommandBuffer cmd, RTHandle depthRT)
        {
            if (debugComputeOutput && computeShader)
            {
                int kernelHandle = computeShader.FindKernel("FramePrediction");
                cmd.SetComputeTextureParam(computeShader, kernelHandle, "_CameraDepthTexture", depthRT);
                cmd.SetComputeTextureParam(computeShader, kernelHandle, "GradiantTexture", debugComputeOutput);
                cmd.DispatchCompute(computeShader, kernelHandle, debugComputeOutput.width / kTileSize,
                    debugComputeOutput.height / kTileSize, 1);
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


