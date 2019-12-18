//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Experimental.Rendering;
//using UnityEngine.Experimental.Rendering.LightweightPipeline;
//using UnityEngine.Rendering;

//public class DefaultRendererSetup : IRendererSetup
//{
//    private DepthOnlyPass m_DepthOnlyPass;
//    private MainLightShadowCasterPass m_MainLightShadowCasterPass;
//    private AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
//    private SetupForwardRenderingPass m_SetupForwardRenderingPass;
//    private ScreenSpaceShadowResolvePass m_ScreenSpaceShadowResolvePass;
//    private CreateLightweightRenderTexturesPass m_CreateLightweightRenderTexturesPass;
//    private BeginXRRenderingPass m_BeginXrRenderingPass;
//    private SetupLightweightConstanstPass m_SetupLightweightConstants;
//    private RenderOpaqueForwardPass m_RenderOpaqueForwardPass;
//    private OpaquePostProcessPass m_OpaquePostProcessPass;
//    private DrawSkyboxPass m_DrawSkyboxPass;
//    private CopyDepthPass m_CopyDepthPass;
//    private CopyColorPass m_CopyColorPass;
//    private RenderTransparentForwardPass m_RenderTransparentForwardPass;
//    private TransparentPostProcessPass m_TransparentPostProcessPass;
//    private FinalBlitPass m_FinalBlitPass;
//    private EndXRRenderingPass m_EndXrRenderingPass;
//    private CustomRenderPass m_CustomPass;

//#if UNITY_EDITOR
//    private SceneViewDepthCopyPass m_SceneViewDepthCopyPass;
//#endif


//    private RenderTargetHandle ColorAttachment;
//    private RenderTargetHandle DepthAttachment;
//    private RenderTargetHandle DepthTexture;
//    private RenderTargetHandle OpaqueColor;
//    private RenderTargetHandle MainLightShadowmap;
//    private RenderTargetHandle AdditionalLightsShadowmap;
//    private RenderTargetHandle ScreenSpaceShadowmap;

//    private List<IBeforeRender> m_BeforeRenderPasses = new List<IBeforeRender>(10);

//    [NonSerialized]
//    private bool m_Initialized = false;

//    private void Init()
//    {
//        if (m_Initialized)
//            return;

//        m_DepthOnlyPass = new DepthOnlyPass();
//        m_MainLightShadowCasterPass = new MainLightShadowCasterPass();
//        m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass();
//        m_SetupForwardRenderingPass = new SetupForwardRenderingPass();
//        m_ScreenSpaceShadowResolvePass = new ScreenSpaceShadowResolvePass();
//        m_CreateLightweightRenderTexturesPass = new CreateLightweightRenderTexturesPass();
//        m_BeginXrRenderingPass = new BeginXRRenderingPass();
//        m_SetupLightweightConstants = new SetupLightweightConstanstPass();
//        m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass();
//        m_OpaquePostProcessPass = new OpaquePostProcessPass();
//        m_DrawSkyboxPass = new DrawSkyboxPass();
//        m_CopyDepthPass = new CopyDepthPass();
//        m_CopyColorPass = new CopyColorPass();
//        m_RenderTransparentForwardPass = new RenderTransparentForwardPass();
//        m_TransparentPostProcessPass = new TransparentPostProcessPass();
//        m_FinalBlitPass = new FinalBlitPass();
//        m_EndXrRenderingPass = new EndXRRenderingPass();

//        m_CustomPass = new CustomRenderPass();

//#if UNITY_EDITOR
//        m_SceneViewDepthCopyPass = new SceneViewDepthCopyPass();
//#endif

//        // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
//        // Samples (MSAA) depend on camera and pipeline
//        ColorAttachment.Init("_CameraColorTexture");
//        DepthAttachment.Init("_CameraDepthAttachment");
//        DepthTexture.Init("_CameraDepthTexture");
//        OpaqueColor.Init("_CameraOpaqueTexture");
//        MainLightShadowmap.Init("_MainLightShadowmapTexture");
//        AdditionalLightsShadowmap.Init("_AdditionalLightsShadowmapTexture");
//        ScreenSpaceShadowmap.Init("_ScreenSpaceShadowmapTexture");

//        m_Initialized = true;
//    }

//    public void Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
//    {
//        Init();

//        Camera camera = renderingData.cameraData.camera;
//        camera.GetComponents(m_BeforeRenderPasses);
//        //设置每个物体的灯光信息
//        renderer.SetupPerObjectLightIndices(ref renderingData.cullResults, ref renderingData.lightData);
//        RenderTextureDescriptor baseDescriptor = ScriptableRenderer.CreateRenderTextureDescriptor(ref renderingData.cameraData);
//        RenderTextureDescriptor shadowDescriptor = baseDescriptor;
//        ClearFlag clearFlag = ScriptableRenderer.GetCameraClearFlag(renderingData.cameraData.camera);
//        shadowDescriptor.dimension = TextureDimension.Tex2D;

//        bool requiresRenderToTexture = ScriptableRenderer.RequiresIntermediateColorTexture(ref renderingData.cameraData, baseDescriptor)
//                                       || m_BeforeRenderPasses.Count != 0;

//        RenderTargetHandle colorHandle = RenderTargetHandle.CameraTarget;
//        RenderTargetHandle depthHandle = RenderTargetHandle.CameraTarget;

//        if (requiresRenderToTexture)
//        {
//            colorHandle = ColorAttachment;
//            depthHandle = DepthAttachment;

//            var sampleCount = (SampleCount)renderingData.cameraData.msaaSamples;
//            m_CreateLightweightRenderTexturesPass.Setup(baseDescriptor, colorHandle, depthHandle, sampleCount);
//            //创建管线中即将要用到的两个主要renderTexture，color texture（保存物体渲染的颜色信息），depth  texture（保存物体渲染的深度信息）
//            renderer.EnqueuePass(m_CreateLightweightRenderTexturesPass);
//        }

//        //如果相机上有IBeforeRender相关的组件话，将会执行这些组件所定义的行为。这一般是开放给开发者，自己定义的某些渲染行为的接口
//        foreach (var pass in m_BeforeRenderPasses)
//        {
//            renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle, clearFlag));
//        }

//        bool mainLightShadows = false;
//        //如果场景里需要主光源渲染阴影，那么开始给所有物体绘制阴影。在绘制阴影时，会使用所需要绘制物体的“ShadowCaster” pass进行阴影的渲染。
//        //并且，在这一阶段，cpu会准备各种渲染阴影所需要的数据，设置各种参数和状态，供shader使用。
//        if (renderingData.shadowData.supportsMainLightShadows)
//        {
//            mainLightShadows = m_MainLightShadowCasterPass.Setup(MainLightShadowmap, ref renderingData);
//            if (mainLightShadows)
//                renderer.EnqueuePass(m_MainLightShadowCasterPass);
//        }
//        //如果场景需要渲染AdditionalLightsShadow，那么就渲染这些阴影。这一过程与上一阶段类似。
//        if (renderingData.shadowData.supportsAdditionalLightShadows)
//        {
//            bool additionalLightShadows = m_AdditionalLightsShadowCasterPass.Setup(AdditionalLightsShadowmap, ref renderingData, renderer.maxVisibleAdditionalLights);
//            if (additionalLightShadows)
//                renderer.EnqueuePass(m_AdditionalLightsShadowCasterPass);
//        }

//        bool resolveShadowsInScreenSpace = mainLightShadows && renderingData.shadowData.requiresScreenSpaceShadowResolve;
//        bool requiresDepthPrepass = resolveShadowsInScreenSpace || renderingData.cameraData.isSceneViewCamera ||
//                                    (renderingData.cameraData.requiresDepthTexture && (!CanCopyDepth(ref renderingData.cameraData) || renderingData.cameraData.isOffscreenRender));

//        // For now VR requires a depth prepass until we figure out how to properly resolve texture2DMS in stereo
//        requiresDepthPrepass |= renderingData.cameraData.isStereoEnabled;

//        //为LWRP设置摄像机参数。包括摄像机的视口，各种矩阵参数，位置，屏幕信息，以及全局时间属性等，供后续Shader的使用
//        renderer.EnqueuePass(m_SetupForwardRenderingPass);

//        //.如果需要处理深度相关的信息，就会开始进入到DepthOnlyPass阶段。这一阶段主要处理的是RenderQueue为opaque的物体，然后通过物体材质上的"DepthOnly"的shader，将处理结果保存在名为“_CameraDepthTexture”的贴图上。
//        //这样，在后续的阶段，我们就可以在shader中直接使用“_CameraDepthTexture”的property来获取场景中物体的深度信息了
//        if (requiresDepthPrepass)
//        {
//            m_DepthOnlyPass.Setup(baseDescriptor, DepthTexture, SampleCount.One);
//            renderer.EnqueuePass(m_DepthOnlyPass);
//            //如果相机上有IAfterDepthPrePass组件，还会进行这些组件所定义的pass行为。这些都是开放给开发者去自定义一些深度处理行为的接口
//            foreach (var pass in camera.GetComponents<IAfterDepthPrePass>())
//                renderer.EnqueuePass(pass.GetPassToEnqueue(m_DepthOnlyPass.descriptor, DepthTexture));
//        }
//        //.如果处理屏幕空间的阴影，则会进入ScreenSpaceShadowResolvePass阶段。
//        //这一阶段好像就是用一个内置的材质处理了一下得到的屏幕空间遮挡的贴图。这一阶段的作用作者也不是很清楚=。=
//        if (resolveShadowsInScreenSpace)
//        {
//            m_ScreenSpaceShadowResolvePass.Setup(baseDescriptor, ScreenSpaceShadowmap);
//            renderer.EnqueuePass(m_ScreenSpaceShadowResolvePass);
//        }

//        //XR相关设置
//        if (renderingData.cameraData.isStereoEnabled)
//            renderer.EnqueuePass(m_BeginXrRenderingPass);

//        RendererConfiguration rendererConfiguration = ScriptableRenderer.GetRendererConfiguration(renderingData.lightData.additionalLightsCount);

//        m_SetupLightweightConstants.Setup(renderer.maxVisibleAdditionalLights, renderer.perObjectLightIndices);
//        //设置LightWeightConstansPass。顾名思义，就是设置渲染管线里面的shader所需要的常数。
//        //比如我们可以在渲染不透明物体之前执行这个阶段来增加许多我们想要在渲染时获取的常数。
//        renderer.EnqueuePass(m_SetupLightweightConstants);

//        // If a before all render pass executed we expect it to clear the color render target
//        if (m_BeforeRenderPasses.Count != 0)
//            clearFlag = ClearFlag.None;

//        m_RenderOpaqueForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, clearFlag, camera.backgroundColor, rendererConfiguration);
//        //渲染不透明物体。这一阶段就是真正的将场景里renderQueue为opaque的物体渲染到之前创建的color texture上，
//        //这一阶段使用的Pass是shader中LightMode为LightweightForward和SRPDefaultUnlit的pass。
//        renderer.EnqueuePass(m_RenderOpaqueForwardPass);

//        //如果摄像机上有IAfterOpaquePass的组件，则会进行这些组件所定义的pass行为。这些是开放给开发者自定义行为的阶段
//        foreach (var pass in camera.GetComponents<IAfterOpaquePass>())
//            renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

//        //如果开启了不透明物体的后处理相关的行为（postProcess）,会在这一阶段处理后处理的pass。后处理效果是unity官方推出的一个提高画面质量的插件，这里不做过多描述如果开启了不透明物体的后处理相关的行为（postProcess）,
//        //会在这一阶段处理后处理的pass。后处理效果是unity官方推出的一个提高画面质量的插件，这里不做过多描述
//        if (renderingData.cameraData.postProcessEnabled &&
//            renderingData.cameraData.postProcessLayer.HasOpaqueOnlyEffects(renderer.postProcessingContext))
//        {
//            m_OpaquePostProcessPass.Setup(baseDescriptor, colorHandle);
//            renderer.EnqueuePass(m_OpaquePostProcessPass);
//            //如果摄像机上有IAfterOpaquePostProcess组件，执行开发者自定义的Pass行为。
//            foreach (var pass in camera.GetComponents<IAfterOpaquePostProcess>())
//                renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));
//        }

//        //如果摄像机渲染天空盒，就进行天空盒的渲染。颜色和深度信息会分别渲染到之前我们所创建的color Texture 和depth Texture上
//        if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
//        {
//            m_DrawSkyboxPass.Setup(colorHandle, depthHandle);
//            renderer.EnqueuePass(m_DrawSkyboxPass);
//        }

//        //如果摄像机上有IAfterSkyboxPass组件，执行开发者自定义的这个Pass行为
//        foreach (var pass in camera.GetComponents<IAfterSkyboxPass>())
//            renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

//        //如果需要使用DepthTexture，则把之前渲染得到的深度信息拷贝到深度贴图上，并设置全局的深度贴图“_CameraDepthAttachment”属性供shader使用，
//        //此外还会设置一些关键词的状态。
//        if (renderingData.cameraData.requiresDepthTexture && !requiresDepthPrepass)
//        {
//            m_CopyDepthPass.Setup(depthHandle, DepthTexture);
//            renderer.EnqueuePass(m_CopyDepthPass);
//        }

//        //如果需要获取到OpaqueTexture的贴图，还会将之前渲染不透明物体得到的color Texture的颜色信息拷贝到另外一个render texture上，
//        //这个texture可以用“_CameraOpaqueTexture”属性获得到。
//        if (renderingData.cameraData.requiresOpaqueTexture)
//        {
//            m_CopyColorPass.Setup(colorHandle, OpaqueColor);
//            renderer.EnqueuePass(m_CopyColorPass);
//        }
//        //渲染透明物体。和不透明物体一样，都使用LightMode为LightweightForward和SRPDefaultUnlit的pass。
//        //但是这个阶段只渲染透明物体。最终它也会把渲染结果保存在之前创建的color Texture上。
//        m_RenderTransparentForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, rendererConfiguration);
//        renderer.EnqueuePass(m_RenderTransparentForwardPass);

//        //如果摄像机上有IAfterTransparentPass组件，执行开发者自定义的这个Pass行为
//        foreach (var pass in camera.GetComponents<IAfterTransparentPass>())
//            renderer.EnqueuePass(pass.GetPassToEnqueue(baseDescriptor, colorHandle, depthHandle));

//        //如果开启了透明物体的postProcess相关行为，在这一阶段处理后处理的pass，类似之前不透明的物体。
//        if (renderingData.cameraData.postProcessEnabled)
//        {
//            m_TransparentPostProcessPass.Setup(baseDescriptor, colorHandle, BuiltinRenderTextureType.CameraTarget);
//            renderer.EnqueuePass(m_TransparentPostProcessPass);
//        }
//        else if (!renderingData.cameraData.isOffscreenRender && colorHandle != RenderTargetHandle.CameraTarget)
//        {
//            m_FinalBlitPass.Setup(baseDescriptor, colorHandle);
//            //如果没有开启透明物体的postProcess行为，且不是offScreenRender，那么会进行混合。
//            //把 之前我们创建的color Texture的像素信息拷贝到屏幕上，显示最终这一帧的屏幕图像。
//            renderer.EnqueuePass(m_FinalBlitPass);
//        }

//        //如果摄像机上有IAfterRender的组件，执行开发者自定义的这个Pass行为。
//        foreach (var pass in camera.GetComponents<IAfterRender>())
//            renderer.EnqueuePass(pass.GetPassToEnqueue());

//        if (renderingData.cameraData.isStereoEnabled)
//        {
//            renderer.EnqueuePass(m_EndXrRenderingPass);
//        }

//#if UNITY_EDITOR
//        if (renderingData.cameraData.isSceneViewCamera)
//        {
//            m_SceneViewDepthCopyPass.Setup(DepthTexture);
//            renderer.EnqueuePass(m_SceneViewDepthCopyPass);
//        }
//#endif
//    }

//    bool CanCopyDepth(ref CameraData cameraData)
//    {
//        bool msaaEnabledForCamera = (int)cameraData.msaaSamples > 1;
//        bool supportsTextureCopy = SystemInfo.copyTextureSupport != CopyTextureSupport.None;
//        bool supportsDepthTarget = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
//        bool supportsDepthCopy = !msaaEnabledForCamera && (supportsDepthTarget || supportsTextureCopy);

//        // TODO:  We don't have support to highp Texture2DMS currently and this breaks depth precision.
//        // currently disabling it until shader changes kick in.
//        //bool msaaDepthResolve = msaaEnabledForCamera && SystemInfo.supportsMultisampledTextures != 0;
//        bool msaaDepthResolve = false;
//        return supportsDepthCopy || msaaDepthResolve;
//    }
//}
