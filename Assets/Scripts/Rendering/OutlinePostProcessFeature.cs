using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlinePostProcessFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class OutlineSettings
    {
        public Material outlineMaterial;
        public Color outlineColor = Color.black;
        [Range(0.1f, 5f)] public float outlineThickness = 1f;
        [Range(0f, 100f)] public float depthSensitivity = 10f;
        [Range(0f, 10f)] public float normalSensitivity = 1f;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public OutlineSettings settings = new OutlineSettings();
    private OutlinePass outlinePass;

    public override void Create()
    {
        outlinePass = new OutlinePass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.outlineMaterial == null)
        {
            Debug.LogWarning("Outline material is missing. Please assign a material to the Outline Post Process Feature.");
            return;
        }

        #pragma warning disable CS0618 // Type or member is obsolete
        outlinePass.Setup(renderer.cameraColorTargetHandle);
        #pragma warning restore CS0618
        renderer.EnqueuePass(outlinePass);
    }

    class OutlinePass : ScriptableRenderPass
    {
        private OutlineSettings settings;
        private RTHandle tempRenderTarget;
        private Material material;
        private RTHandle source;

        private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineThicknessProperty = Shader.PropertyToID("_OutlineThickness");
        private static readonly int DepthSensitivityProperty = Shader.PropertyToID("_DepthSensitivity");
        private static readonly int NormalSensitivityProperty = Shader.PropertyToID("_NormalSensitivity");

        public OutlinePass(OutlineSettings settings)
        {
            this.settings = settings;
            this.renderPassEvent = settings.renderPassEvent;
            material = settings.outlineMaterial;
        }

        public void Setup(RTHandle source)
        {
            this.source = source;
        }

        #pragma warning disable CS0672 // Member overrides obsolete member
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;

            RenderingUtils.ReAllocateHandleIfNeeded(ref tempRenderTarget, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempOutlineTexture");

            // Configure depth and normals requirements
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || source == null)
                return;
            
            var cameraData = renderingData.cameraData;
            if (cameraData.camera.cameraType != CameraType.Game && cameraData.camera.cameraType != CameraType.SceneView)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("Outline Post Process");

            // Set material properties
            material.SetColor(OutlineColorProperty, settings.outlineColor);
            material.SetFloat(OutlineThicknessProperty, settings.outlineThickness);
            material.SetFloat(DepthSensitivityProperty, settings.depthSensitivity);
            material.SetFloat(NormalSensitivityProperty, settings.normalSensitivity);

            // Use Blit API
            #pragma warning disable CS0618
            cmd.SetGlobalTexture("_BlitTexture", source);
            Blit(cmd, source, tempRenderTarget, material, 0);
            Blit(cmd, tempRenderTarget, source);
            #pragma warning restore CS0618

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        #pragma warning restore CS0672

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // Cleanup is handled automatically by RTHandle system
        }

        public void Dispose()
        {
            tempRenderTarget?.Release();
        }
    }

    protected override void Dispose(bool disposing)
    {
        outlinePass?.Dispose();
    }
}
