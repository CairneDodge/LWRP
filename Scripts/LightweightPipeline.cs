//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Experimental.Rendering;
//using UnityEngine.Rendering;
//using UnityEngine.XR;
//using UnityEngine.Experimental.Rendering.LightweightPipeline;

//public class LightweightPipeline : RenderPipeline
//{
//    /// <summary>
//    /// 渲染管线资源
//    /// </summary>
//    public LightweightPipelineAsset pipelineAsset { get; private set; }
//    /// <summary>
//    /// 摄像机按深度排序
//    /// </summary>
//    CameraComparer m_CameraComparer = new CameraComparer();
//    /// <summary>
//    /// 前向渲染类
//    /// </summary>
//    LightweightForwardRenderer m_Renderer;
//    /// <summary>
//    /// 剔除结果
//    /// </summary>
//    CullResults m_CullResults;
//    /// <summary>
//    /// 非平行光源编号索引，用于阴影生成
//    /// </summary>
//    List<int> m_LocalLightIndices = new List<int>();
//    /// <summary>
//    /// 标记逐相机渲染进行过程
//    /// </summary>
//    bool m_IsCameraRendering;

//    public LightweightPipeline(LightweightPipelineAsset asset)
//    {
//        pipelineAsset = asset;

//        SetSupportedRenderingFeatures();
//        SetPipelineCapabilities(asset);

//        PerFrameBuffer._GlossyEnvironmentColor = Shader.PropertyToID("_GlossyEnvironmentColor");
//        PerFrameBuffer._SubtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");

//        PerCameraBuffer._ScaledScreenParams = Shader.PropertyToID("_ScaledScreenParams");
//        m_Renderer = new LightweightForwardRenderer(asset);

//        // Let engine know we have MSAA on for cases where we support MSAA backbuffer
//        if (QualitySettings.antiAliasing != pipelineAsset.msaaSampleCount)
//            QualitySettings.antiAliasing = pipelineAsset.msaaSampleCount;

//        Shader.globalRenderPipeline = "LightweightPipeline";
//        m_IsCameraRendering = false;
//    }

//    public override void Dispose()
//    {
//        base.Dispose();
//        Shader.globalRenderPipeline = "";
//        SupportedRenderingFeatures.active = new SupportedRenderingFeatures();

//#if UNITY_EDITOR
//        SceneViewDrawMode.ResetDrawMode();
//#endif

//        m_Renderer.Dispose();
//    }

//    public override void Render(ScriptableRenderContext context, Camera[] cameras)
//    {
//        if (m_IsCameraRendering)
//        {
//            Debug.LogWarning("Nested camera rendering is forbidden. If you are calling camera.Render inside OnWillRenderObject callback, use BeginCameraRender callback instead.");
//            return;
//        }

//        //初始化每帧数据
//        base.Render(context, cameras);
//        BeginFrameRendering(cameras);

//        GraphicsSettings.lightsUseLinearIntensity = true;
//        SetupPerFrameShaderConstants();

//        // Sort cameras array by camera depth
//        Array.Sort(cameras, m_CameraComparer);

//        foreach (Camera camera in cameras)
//        {
//            BeginCameraRendering(camera);
//            string renderCameraTag = "Render " + camera.name;
//            CommandBuffer cmd = CommandBufferPool.Get(renderCameraTag);
//            //using 结束或中途中断会调用()中类的Dispose()方法
//            using (new ProfilingSample(cmd, renderCameraTag))
//            {
//                //初始化逐相机的数据
//                CameraData cameraData;
//                InitializeCameraData(camera, out cameraData);
//                SetupPerCameraShaderConstants(cameraData);
//                //剔除
//                ScriptableCullingParameters cullingParameters;
//                if (!CullResults.GetCullingParameters(camera, cameraData.isStereoEnabled, out cullingParameters))
//                {
//                    CommandBufferPool.Release(cmd);
//                    continue;
//                }

//                cullingParameters.shadowDistance = Mathf.Min(cameraData.maxShadowDistance, camera.farClipPlane);
//                //这里执行的new ProfilingSample(cmd, renderCameraTag)中设置的cmd.BeginSample(name);命令
//                context.ExecuteCommandBuffer(cmd);
//                cmd.Clear();

//#if UNITY_EDITOR
//                try
//#endif
//                {
//                    m_IsCameraRendering = true;
//#if UNITY_EDITOR
//                    // Emit scene view UI
//                    if (cameraData.isSceneViewCamera)
//                        ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
//#endif
//                    //剔除结果
//                    CullResults.Cull(ref cullingParameters, context, ref m_CullResults);
//                    List<VisibleLight> visibleLights = m_CullResults.visibleLights;

//                    RenderingData renderingData;
//                    InitializeRenderingData(ref cameraData, visibleLights,
//                        m_Renderer.maxSupportedLocalLightsPerPass, m_Renderer.maxSupportedVertexLights,
//                        out renderingData);
//                    m_Renderer.Setup(ref context, ref m_CullResults, ref renderingData);
//                    m_Renderer.Execute(ref context, ref m_CullResults, ref renderingData);
//                }
//#if UNITY_EDITOR
//                catch (Exception)
//                {
//                    CommandBufferPool.Release(cmd);
//                    throw;
//                }
//                finally
//#endif
//                {
//                    m_IsCameraRendering = false;
//                }
//            }
//            //这里执行的ProfilingSample.Dispose()中设置的m_Cmd.EndSample(m_Name);命令
//            context.ExecuteCommandBuffer(cmd);
//            CommandBufferPool.Release(cmd);
//            context.Submit();
//        }
//    }

//    public static void RenderPostProcess(CommandBuffer cmd, PostProcessRenderContext context, ref CameraData cameraData, RenderTextureFormat colorFormat, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool opaqueOnly)
//    {
//        context.Reset();
//        context.camera = cameraData.camera;
//        context.source = source;
//        context.sourceFormat = colorFormat;
//        context.destination = dest;
//        context.command = cmd;
//        context.flip = cameraData.camera.targetTexture == null;

//        if (opaqueOnly)
//            cameraData.postProcessLayer.RenderOpaqueOnly(context);
//        else
//            cameraData.postProcessLayer.Render(context);
//    }
//    /// <summary>
//    /// 设置支持的渲染特征,active是静态的
//    /// </summary>
//    void SetSupportedRenderingFeatures()
//    {
//#if UNITY_EDITOR
//        SupportedRenderingFeatures.active = new SupportedRenderingFeatures()
//        {
//            //反射探针
//            reflectionProbeSupportFlags = SupportedRenderingFeatures.ReflectionProbeSupportFlags.None,
//            //默认的光照贴图的混合烘焙模式
//            defaultMixedLightingMode = SupportedRenderingFeatures.LightmapMixedBakeMode.Subtractive,
//            //支持的光照贴图的混合烘焙模式
//            supportedMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeMode.Subtractive,
//            //支持的光照贴图烘焙模式
//            supportedLightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed,
//            //支持的光照贴图类型
//            supportedLightmapsModes = LightmapsMode.CombinedDirectional | LightmapsMode.NonDirectional,
//            //支持光照探针代理
//            rendererSupportsLightProbeProxyVolumes = false,
//            //支持运动矢量
//            rendererSupportsMotionVectors = false,
//            //接受阴影
//            rendererSupportsReceiveShadows = true,
//            //反射探针
//            rendererSupportsReflectionProbes = true
//        };
//        SceneViewDrawMode.SetupDrawMode();
//#endif
//    }
//    /// <summary>
//    /// 初始化摄像机数据
//    /// </summary>
//    /// <param name="camera"></param>
//    /// <param name="cameraData"></param>
//    void InitializeCameraData(Camera camera, out CameraData cameraData)
//    {
//        //接近1的pipelineAsset.renderScale(或者XRSettings.eyeTextureResolutionScale)，置为1
//        const float kRenderScaleThreshold = 0.05f;
//        cameraData.camera = camera;

//        bool msaaEnabled = camera.allowMSAA && pipelineAsset.msaaSampleCount > 1;
//        if (msaaEnabled)
//            cameraData.msaaSamples = (camera.targetTexture != null) ? camera.targetTexture.antiAliasing : pipelineAsset.msaaSampleCount;
//        else
//            cameraData.msaaSamples = 1;
//        //场景相机
//        cameraData.isSceneViewCamera = camera.cameraType == CameraType.SceneView;
//        //存在RT且不是场景相机
//        cameraData.isOffscreenRender = camera.targetTexture != null && !cameraData.isSceneViewCamera;
//        cameraData.isStereoEnabled = IsStereoEnabled(camera);
//        cameraData.isHdrEnabled = camera.allowHDR && pipelineAsset.supportsHDR;

//        cameraData.postProcessLayer = camera.GetComponent<PostProcessLayer>();
//        cameraData.postProcessEnabled = cameraData.postProcessLayer != null && cameraData.postProcessLayer.isActiveAndEnabled;

//        // PostProcess for VR is not working atm. Disable it for now.
//        cameraData.postProcessEnabled &= !cameraData.isStereoEnabled;

//        Rect cameraRect = camera.rect;
//        cameraData.isDefaultViewport = (!(Math.Abs(cameraRect.x) > 0.0f || Math.Abs(cameraRect.y) > 0.0f ||
//                                          Math.Abs(cameraRect.width) < 1.0f || Math.Abs(cameraRect.height) < 1.0f));

//        // Discard variations lesser than kRenderScaleThreshold.
//        // Scale is only enabled for gameview.
//        // In XR mode, grab renderScale from XRSettings instead of SRP asset for now.
//        // This is just a temporary change pending full integration of XR with SRP

//        if (camera.cameraType == CameraType.Game)
//        {
//#if !UNITY_SWITCH
//            if (cameraData.isStereoEnabled)
//            {
//                cameraData.renderScale = XRSettings.eyeTextureResolutionScale;
//            }
//            else
//#endif
//            {
//                cameraData.renderScale = pipelineAsset.renderScale;
//            }
//        }
//        else
//        {
//            cameraData.renderScale = 1.0f;
//        }

//        cameraData.renderScale = (Mathf.Abs(1.0f - cameraData.renderScale) < kRenderScaleThreshold) ? 1.0f : cameraData.renderScale;

//        cameraData.requiresDepthTexture = pipelineAsset.supportsCameraDepthTexture || cameraData.isSceneViewCamera;
//        cameraData.requiresSoftParticles = pipelineAsset.supportsSoftParticles;
//        cameraData.requiresOpaqueTexture = pipelineAsset.supportsCameraOpaqueTexture;
//        cameraData.opaqueTextureDownsampling = pipelineAsset.opaqueDownsampling;

//        bool anyShadowsEnabled = pipelineAsset.supportsDirectionalShadows || pipelineAsset.supportsLocalShadows;
//        cameraData.maxShadowDistance = (anyShadowsEnabled) ? pipelineAsset.shadowDistance : 0.0f;
//        //这是一个额外添加的脚本
//        LightweightAdditionalCameraData additionalCameraData = camera.gameObject.GetComponent<LightweightAdditionalCameraData>();
//        if (additionalCameraData != null)
//        {
//            cameraData.maxShadowDistance = (additionalCameraData.renderShadows) ? cameraData.maxShadowDistance : 0.0f;
//            cameraData.requiresDepthTexture &= additionalCameraData.requiresDepthTexture;
//            cameraData.requiresOpaqueTexture &= additionalCameraData.requiresColorTexture;
//        }
//        else if (!cameraData.isSceneViewCamera && camera.cameraType != CameraType.Reflection && camera.cameraType != CameraType.Preview)
//        {
//            cameraData.requiresDepthTexture = false;
//            cameraData.requiresOpaqueTexture = false;
//        }

//        cameraData.requiresDepthTexture |= cameraData.postProcessEnabled;
//    }
//    /// <summary>
//    /// 初始化渲染数据
//    /// </summary>
//    /// <param name="cameraData"></param>
//    /// <param name="visibleLights"></param>
//    /// <param name="maxSupportedLocalLightsPerPass"></param>
//    /// <param name="maxSupportedVertexLights"></param>
//    /// <param name="renderingData"></param>
//    void InitializeRenderingData(ref CameraData cameraData, List<VisibleLight> visibleLights, int maxSupportedLocalLightsPerPass, int maxSupportedVertexLights, out RenderingData renderingData)
//    {
//        //用于生成阴影的非平行光源
//        m_LocalLightIndices.Clear();
//        //有阴影投射平行光源
//        bool hasDirectionalShadowCastingLight = false;
//        //有阴影投射非平行光源
//        bool hasLocalShadowCastingLight = false;
//        //初始化阴影的相关变量
//        if (cameraData.maxShadowDistance > 0.0f)
//        {
//            for (int i = 0; i < visibleLights.Count; ++i)
//            {
//                Light light = visibleLights[i].light;
//                bool castShadows = light != null && light.shadows != LightShadows.None;
//                if (visibleLights[i].lightType == LightType.Directional)
//                {
//                    hasDirectionalShadowCastingLight |= castShadows;
//                }
//                else
//                {
//                    hasLocalShadowCastingLight |= castShadows;
//                    m_LocalLightIndices.Add(i);
//                }
//            }
//        }

//        renderingData.cameraData = cameraData;
//        InitializeLightData(visibleLights, maxSupportedLocalLightsPerPass, maxSupportedVertexLights, out renderingData.lightData);
//        InitializeShadowData(hasDirectionalShadowCastingLight, hasLocalShadowCastingLight, out renderingData.shadowData);
//        renderingData.supportsDynamicBatching = pipelineAsset.supportsDynamicBatching;
//    }
//    /// <summary>
//    /// 初始化阴影数据
//    /// </summary>
//    /// <param name="hasDirectionalShadowCastingLight"></param>
//    /// <param name="hasLocalShadowCastingLight"></param>
//    /// <param name="shadowData"></param>
//    void InitializeShadowData(bool hasDirectionalShadowCastingLight, bool hasLocalShadowCastingLight, out ShadowData shadowData)
//    {
//        // Until we can have keyword stripping forcing single cascade hard shadows on gles2
//        bool supportsScreenSpaceShadows = SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;
//        //渲染平行光阴影
//        shadowData.renderDirectionalShadows = pipelineAsset.supportsDirectionalShadows && hasDirectionalShadowCastingLight;

//        // we resolve shadows in screenspace when cascades are enabled to save ALU as computing cascade index + shadowCoord on fragment is expensive
//        shadowData.requiresScreenSpaceShadowResolve = shadowData.renderDirectionalShadows && supportsScreenSpaceShadows && pipelineAsset.cascadeCount > 1;
//        //阴影级联
//        shadowData.directionalLightCascadeCount = (shadowData.requiresScreenSpaceShadowResolve) ? pipelineAsset.cascadeCount : 1;
//        shadowData.directionalShadowAtlasWidth = pipelineAsset.directionalShadowAtlasResolution;
//        shadowData.directionalShadowAtlasHeight = pipelineAsset.directionalShadowAtlasResolution;
//        //阴影级联统一为Vector3表达
//        switch (shadowData.directionalLightCascadeCount)
//        {
//            case 1:
//                shadowData.directionalLightCascades = new Vector3(1.0f, 0.0f, 0.0f);
//                break;

//            case 2:
//                shadowData.directionalLightCascades = new Vector3(pipelineAsset.cascade2Split, 1.0f, 0.0f);
//                break;

//            default:
//                shadowData.directionalLightCascades = pipelineAsset.cascade4Split;
//                break;
//        }
//        //渲染非平行光阴影
//        shadowData.renderLocalShadows = pipelineAsset.supportsLocalShadows && hasLocalShadowCastingLight;
//        shadowData.localShadowAtlasWidth = shadowData.localShadowAtlasHeight = pipelineAsset.localShadowAtlasResolution;
//        shadowData.supportsSoftShadows = pipelineAsset.supportsSoftShadows;
//        shadowData.bufferBitCount = 16;

//        shadowData.renderedDirectionalShadowQuality = LightShadows.None;
//        shadowData.renderedLocalShadowQuality = LightShadows.None;
//    }
//    /// <summary>
//    /// 初始化光源数据
//    /// </summary>
//    /// <param name="visibleLights"></param>
//    /// <param name="maxSupportedLocalLightsPerPass"></param>
//    /// <param name="maxSupportedVertexLights"></param>
//    /// <param name="lightData"></param>
//    void InitializeLightData(List<VisibleLight> visibleLights, int maxSupportedLocalLightsPerPass, int maxSupportedVertexLights, out LightData lightData)
//    {
//        //控制最大可见光源数量<=pipelineAsset.maxPixelLights
//        int visibleLightsCount = Math.Min(visibleLights.Count, pipelineAsset.maxPixelLights);
//        lightData.mainLightIndex = GetMainLight(visibleLights);

//        // If we have a main light we don't shade it in the per-object light loop. We also remove it from the per-object cull list
//        int mainLightPresent = (lightData.mainLightIndex >= 0) ? 1 : 0;
//        //计算附加光数量，maxSupportedLocalLightsPerPasss是4或16
//        int additionalPixelLightsCount = Math.Min(visibleLightsCount - mainLightPresent, maxSupportedLocalLightsPerPass);
//        int vertexLightCount = (pipelineAsset.supportsVertexLight) ? Math.Min(visibleLights.Count, maxSupportedLocalLightsPerPass) - additionalPixelLightsCount : 0;
//        //计算逐顶点光数量，maxSupportedVertexLights是4
//        vertexLightCount = Math.Min(vertexLightCount, maxSupportedVertexLights);

//        //附加光数量
//        lightData.pixelAdditionalLightsCount = additionalPixelLightsCount;
//        //不支持顶点光时vertexLightCount是0，最后是additionalPixelLightsCount；
//        //支持时最后是 Math.Min(visibleLights.Count, maxSupportedLocalLightsPerPass)
//        lightData.totalAdditionalLightsCount = additionalPixelLightsCount + vertexLightCount;
//        lightData.visibleLights = visibleLights;
//        lightData.visibleLocalLightIndices = m_LocalLightIndices;
//    }
//    /// <summary>
//    /// 查找第一个Direction光源，返回数组中编号，若无返回-1
//    /// </summary>
//    /// <param name="visibleLights"></param>
//    /// <returns></returns>
//    // Main Light is always a directional light
//    int GetMainLight(List<VisibleLight> visibleLights)
//    {
//        int totalVisibleLights = visibleLights.Count;

//        if (totalVisibleLights == 0 || pipelineAsset.maxPixelLights == 0)
//            return -1;

//        for (int i = 0; i < totalVisibleLights; ++i)
//        {
//            VisibleLight currLight = visibleLights[i];

//            // Particle system lights have the light property as null. We sort lights so all particles lights
//            // come last. Therefore, if first light is particle light then all lights are particle lights.
//            // In this case we either have no main light or already found it.
//            if (currLight.light == null)
//                break;

//            // In case no shadow light is present we will return the brightest directional light
//            if (currLight.lightType == LightType.Directional)
//                return i;
//        }

//        return -1;
//    }
//    /// <summary>
//    /// 设置每帧的Shader常量_GlossyEnvironmentColor、_SubtractiveShadowColor
//    /// </summary>
//    void SetupPerFrameShaderConstants()
//    {
//        // When glossy reflections are OFF in the shader we set a constant color to use as indirect specular
//        SphericalHarmonicsL2 ambientSH = RenderSettings.ambientProbe;
//        Color linearGlossyEnvColor = new Color(ambientSH[0, 0], ambientSH[1, 0], ambientSH[2, 0]) * RenderSettings.reflectionIntensity;
//        Color glossyEnvColor = CoreUtils.ConvertLinearToActiveColorSpace(linearGlossyEnvColor);
//        Shader.SetGlobalVector(PerFrameBuffer._GlossyEnvironmentColor, glossyEnvColor);

//        // Used when subtractive mode is selected
//        Shader.SetGlobalVector(PerFrameBuffer._SubtractiveShadowColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.subtractiveShadowColor));
//    }
//    /// <summary>
//    /// 设置每个相机的Shader常量_ScaledScreenParams
//    /// </summary>
//    /// <param name="cameraData"></param>
//    void SetupPerCameraShaderConstants(CameraData cameraData)
//    {
//        float cameraWidth = (float)cameraData.camera.pixelWidth * cameraData.renderScale;
//        float cameraHeight = (float)cameraData.camera.pixelWidth * cameraData.renderScale;
//        Shader.SetGlobalVector(PerCameraBuffer._ScaledScreenParams, new Vector4(cameraWidth, cameraHeight, 1.0f + 1.0f / cameraWidth, 1.0f + 1.0f / cameraHeight));
//    }

//    bool IsStereoEnabled(Camera camera)
//    {
//#if !UNITY_SWITCH
//        bool isSceneViewCamera = camera.cameraType == CameraType.SceneView;
//        return XRSettings.isDeviceActive && !isSceneViewCamera && (camera.stereoTargetEye == StereoTargetEyeMask.Both);
//#else
//            return false;
//#endif
//    }

//}
