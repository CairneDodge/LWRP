//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEditor.ProjectWindowCallback;
//using UnityEngine;
//using UnityEngine.Experimental.Rendering;
//using UnityEngine.Experimental.Rendering.LightweightPipeline;

//public enum ShadowCascades
//{
//    NO_CASCADES = 0,
//    TWO_CASCADES,
//    FOUR_CASCADES,
//}

//public enum ShadowType
//{
//    NO_SHADOW = 0,
//    HARD_SHADOWS,
//    SOFT_SHADOWS,
//}

//public enum ShadowResolution
//{
//    _256 = 256,
//    _512 = 512,
//    _1024 = 1024,
//    _2048 = 2048,
//    _4096 = 4096,
//}

//public enum MSAAQuality
//{
//    Disabled = 1,
//    _2x = 2,
//    _4x = 4,
//    _8x = 8,
//}

///// <summary>
///// 降采样，在ForwardLitPass中使用
///// </summary>
//public enum Downsampling
//{
//    None = 0,
//    _2xBilinear,
//    _4xBox,
//    _4xBilineaar,
//}

//public enum DefaultMaterialType
//{
//    Standard = 0,
//    Particle,
//    Terrain,
//    UnityBuiltinDefault
//}

//public class LightweightPipelineAsset : RenderPipelineAsset, ISerializationCallbackReceiver
//{
//    //这两个路径用于查找scriptableObject，优先在asset中查找
//    public static readonly string s_SearchPathProject = "Assets";
//    public static readonly string s_SearchPathPackage = "Packages/com.unity.render-pipelines.lightweight";

//    Shader m_DefaultShader;

//    [SerializeField] int k_AssetVersion = 3;

//    #region Editor中包含的元素
//    [SerializeField] int m_MaxPixelLights = 4;

//    [SerializeField] bool m_SupportsVertexLight = false;

//    [SerializeField] bool m_RequireDepthTexture = false;

//    [SerializeField] bool m_RequireSoftParticles = false;

//    [SerializeField] bool m_RequireOpaqueTexture = false;

//    [SerializeField] bool m_SupportsHDR = false;

//    [SerializeField] MSAAQuality m_MSAA = MSAAQuality._4x;

//    /// <summary>
//    /// 渲染比例
//    /// </summary>
//    [SerializeField] float m_RenderScale = 1.0f;

//    [SerializeField] bool m_SupportsDynamicBatching = true;

//    /// <summary>
//    /// 定向阴影
//    /// </summary>
//    [SerializeField] bool m_DirectionalShadowsSupported = true;

//    [SerializeField] ShadowResolution m_ShadowAtlasResolution = ShadowResolution._2048;

//    [SerializeField] float m_ShadowDistance = 50.0f;

//    [SerializeField] ShadowCascades m_ShadowCascades = ShadowCascades.FOUR_CASCADES;

//    /// <summary>
//    /// 二级级联分界
//    /// </summary>
//    [SerializeField] float m_Cascade2Split = 0.25f;

//    /// <summary>
//    /// 四级级联分界
//    /// </summary>
//    [SerializeField] Vector3 m_Cascade4Split = new Vector3(0.06f, 0.2f, 0.467f);

//    /// <summary>
//    /// 支持非平行光阴影
//    /// </summary>
//    [SerializeField] bool m_LocalShadowsSupported = true;

//    /// <summary>
//    /// 非平行光分辨率
//    /// </summary>
//    [SerializeField] ShadowResolution m_LocalShadowsAtlasResolution = ShadowResolution._512;

//    [SerializeField] bool m_SoftShadowsSupported = false;
//    #endregion

//    [SerializeField] bool m_KeepAdditionalLightVariants = true;
//    [SerializeField] bool m_KeepVertexLightVariants = true;
//    [SerializeField] bool m_KeepDirectionalShadowVariants = true;
//    [SerializeField] bool m_KeepLocalShadowVariants = true;
//    [SerializeField] bool m_KeepSoftShadowVariants = true;

//    /// <summary>
//    /// 4个shader：BlitShader；CopyDepthShader；ScreenSpaceShadowShader；SamplingShader
//    /// </summary>
//    [SerializeField] LightweightPipelineResources m_ResourcesAsset;

//    [SerializeField] ShadowType m_ShadowType = ShadowType.HARD_SHADOWS;

//#if UNITY_EDITOR

//    /// <summary>
//    /// 三个材质
//    /// </summary>
//    [NonSerialized]
//    LightweightPipelineEditorResources m_EditorResourcesAsset;

//    [MenuItem("Assets/Create/Rendering/Lightweight Pipeline Asset", priority = CoreUtils.assetCreateMenuPriority1)]
//    static void CreateLightweightPipeline()
//    {
//        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateLightweightPipelineAsset>(),
//            "LightweightAsset.asset", null, null);
//    }

//    //[MenuItem("Assets/Create/Rendering/Lightweight Pipeline Resources", priority = CoreUtils.assetCreateMenuPriority1)]
//    static void CreateLightweightPipelineResources()
//    {
//        var instance = CreateInstance<LightweightPipelineResources>();
//        AssetDatabase.CreateAsset(instance, string.Format("Assets/{0}.asset", typeof(LightweightPipelineResources).Name));

//    }

//    //[MenuItem("Assets/Create/Rendering/Lightweight Pipeline Editor Resources", priority = CoreUtils.assetCreateMenuPriority1)]
//    static void CreateLightweightPipelineEditorResources()
//    {
//        var instance = CreateInstance<LightweightPipelineEditorResources>();
//        AssetDatabase.CreateAsset(instance, string.Format("Assets/{0}.asset", typeof(LightweightPipelineEditorResources).Name));

//    }

//    /// <summary>
//    /// 创建带初始化的窗口
//    /// </summary>
//    class CreateLightweightPipelineAsset : EndNameEditAction
//    {
//        public override void Action(int instanceId, string pathName, string resourceFile)
//        {
//            var instance = CreateInstance<LightweightPipelineAsset>();
//            instance.m_EditorResourcesAsset = LoadResourceFile<LightweightPipelineEditorResources>();
//            instance.m_ResourcesAsset = LoadResourceFile<LightweightPipelineResources>();
//            AssetDatabase.CreateAsset(instance, pathName);
//        }

//    }

//    /// <summary>
//    /// 加载ScriptableObject资源，先查找Asset文件夹，若不存在使用默认
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    /// <returns></returns>
//    static T LoadResourceFile<T>() where T : ScriptableObject
//    {
//        T resourceAsset = null;
//        var guids = AssetDatabase.FindAssets(typeof(T).Name + " t:scriptableobject", new[] { s_SearchPathProject });
//        foreach (string guid in guids)
//        {
//            string path = AssetDatabase.GUIDToAssetPath(guid);
//            resourceAsset = AssetDatabase.LoadAssetAtPath<T>(path);
//            if (resourceAsset != null)
//                break;
//        }

//        // There's currently an issue that prevents FindAssets from find resources withing the package folder.
//        if (resourceAsset == null)
//        {
//            string path = s_SearchPathPackage + "/LWRP/Data/" + typeof(T).Name + ".asset";
//            resourceAsset = AssetDatabase.LoadAssetAtPath<T>(path);
//        }
//        return resourceAsset;

//    }

//    LightweightPipelineEditorResources editorResources
//    {
//        get
//        {
//            if (m_EditorResourcesAsset == null)
//                m_EditorResourcesAsset = LoadResourceFile<LightweightPipelineEditorResources>();

//            return m_EditorResourcesAsset;
//        }
//    }
//#endif

//    LightweightPipelineResources resources
//    {
//        get
//        {
//#if UNITY_EDITOR
//            if (m_ResourcesAsset == null)
//                m_ResourcesAsset = LoadResourceFile<LightweightPipelineResources>();
//#endif
//            return m_ResourcesAsset;
//        }
//    }

//    /// <summary>
//    /// 创建渲染管线的方法
//    /// </summary>
//    /// <returns></returns>
//    protected override IRenderPipeline InternalCreatePipeline()
//    {
//        return new LightweightPipeline(this);
//    }

//    /// <summary>
//    /// 获得ScriptableObject中的材质资源
//    /// </summary>
//    /// <param name="materialType"></param>
//    /// <returns></returns>
//    Material GetMaterial(DefaultMaterialType materialType)
//    {
//#if UNITY_EDITOR
//        if (editorResources == null)
//            return null;

//        switch (materialType)
//        {
//            case DefaultMaterialType.Standard:
//                return editorResources.DefaultMaterial;

//            case DefaultMaterialType.Particle:
//                return editorResources.DefaultParticleMaterial;

//            case DefaultMaterialType.Terrain:
//                return editorResources.DefaultTerrainMaterial;

//            // Unity Builtin Default
//            default:
//                return null;
//        }
//#else
//            return null;
//#endif
//    }

//    /// <summary>
//    /// 获得版本号
//    /// </summary>
//    /// <returns></returns>
//    public int GetAssetVersion()
//    {
//        return k_AssetVersion;
//    }
//    /// <summary>
//    /// 最大逐像素光源数量
//    /// </summary>
//    public int maxPixelLights
//    {
//        get { return m_MaxPixelLights; }
//    }
//    /// <summary>
//    /// 支持顶点光照
//    /// </summary>
//    public bool supportsVertexLight
//    {
//        get { return m_SupportsVertexLight; }
//    }
//    /// <summary>
//    /// 支持深度纹理
//    /// </summary>
//    public bool supportsCameraDepthTexture
//    {
//        get { return m_RequireDepthTexture; }
//    }
//    /// <summary>
//    /// 支持软粒子
//    /// </summary>
//    public bool supportsSoftParticles
//    {
//        get { return m_RequireSoftParticles; }
//    }
//    /// <summary>
//    /// 支持不透明贴图
//    /// </summary>
//    public bool supportsCameraOpaqueTexture
//    {
//        get { return m_RequireOpaqueTexture; }
//    }
//    /// <summary>
//    /// 不透明降采样 Downsampling._2xBilinear
//    /// </summary>
//    public Downsampling opaqueDownsampling
//    {
//        get { return m_OpaqueDownsampling; }
//    }
//    /// <summary>
//    /// 支持HDR
//    /// </summary>
//    public bool supportsHDR
//    {
//        get { return m_SupportsHDR; }
//    }
//    /// <summary>
//    /// MSAA的程度
//    /// </summary>
//    public int msaaSampleCount
//    {
//        get { return (int)m_MSAA; }
//        set { m_MSAA = (MSAAQuality)value; }
//    }
//    /// <summary>
//    /// 渲染比例
//    /// </summary>
//    public float renderScale
//    {
//        get { return m_RenderScale; }
//        set { m_RenderScale = value; }
//    }
//    /// <summary>
//    /// 支持动态批处理
//    /// </summary>
//    public bool supportsDynamicBatching
//    {
//        get { return m_SupportsDynamicBatching; }
//    }
//    /// <summary>
//    /// 支持定向阴影
//    /// </summary>
//    public bool supportsDirectionalShadows
//    {
//        get { return m_DirectionalShadowsSupported; }
//    }
//    /// <summary>
//    /// 定向阴影图集分辨率
//    /// </summary>
//    public int directionalShadowAtlasResolution
//    {
//        get { return (int)m_ShadowAtlasResolution; }
//    }
//    /// <summary>
//    /// 阴影距离
//    /// </summary>
//    public float shadowDistance
//    {
//        get { return m_ShadowDistance; }
//        set { m_ShadowDistance = value; }
//    }
//    /// <summary>
//    /// 阴影级联（1、2、4）
//    /// </summary>
//    public int cascadeCount
//    {
//        get
//        {
//            switch (m_ShadowCascades)
//            {
//                case ShadowCascades.TWO_CASCADES:
//                    return 2;
//                case ShadowCascades.FOUR_CASCADES:
//                    return 4;
//                default:
//                    return 1;
//            }
//        }
//    }
//    /// <summary>
//    /// 阴影级联2级
//    /// </summary>
//    public float cascade2Split
//    {
//        get { return m_Cascade2Split; }
//    }
//    /// <summary>
//    /// 阴影级联4级
//    /// </summary>
//    public Vector3 cascade4Split
//    {
//        get { return m_Cascade4Split; }
//    }
//    /// <summary>
//    /// 支持非平行光阴影
//    /// </summary>
//    public bool supportsLocalShadows
//    {
//        get { return m_LocalShadowsSupported; }
//    }
//    /// <summary>
//    /// 非平行光阴影分辨率
//    /// </summary>
//    public int localShadowAtlasResolution
//    {
//        get { return (int)m_LocalShadowsAtlasResolution; }
//    }
//    /// <summary>
//    /// 支持软阴影
//    /// </summary>
//    public bool supportsSoftShadows
//    {
//        get { return m_SoftShadowsSupported; }
//    }
//    /// <summary>
//    /// 自定义Shader变量分离，False
//    /// </summary>
//    public bool customShaderVariantStripping
//    {
//        get { return false; }
//    }

//    //以下内容，关系到LightweightPipelineCore中的 static PipelineCapabilities s_PipelineCapabilities
//    public bool keepAdditionalLightVariants
//    {
//        get { return m_KeepAdditionalLightVariants; }
//    }

//    public bool keepVertexLightVariants
//    {
//        get { return m_KeepVertexLightVariants; }
//    }

//    public bool keepDirectionalShadowVariants
//    {
//        get { return m_KeepDirectionalShadowVariants; }
//    }

//    public bool keepLocalShadowVariants
//    {
//        get { return m_KeepLocalShadowVariants; }
//    }

//    public bool keepSoftShadowVariants
//    {
//        get { return m_KeepSoftShadowVariants; }
//    }
//    /// <summary>
//    /// Lightweight-Default.mat
//    /// </summary>
//    /// <returns></returns>
//    public override Material GetDefaultMaterial()
//    {
//        return GetMaterial(DefaultMaterialType.Standard);
//    }
//    /// <summary>
//    /// Lightweight-DefaultParticle.mat
//    /// </summary>
//    /// <returns></returns>
//    public override Material GetDefaultParticleMaterial()
//    {
//        return GetMaterial(DefaultMaterialType.Particle);
//    }
//    /// <summary>
//    /// null
//    /// </summary>
//    /// <returns></returns>
//    public override Material GetDefaultLineMaterial()
//    {
//        return GetMaterial(DefaultMaterialType.UnityBuiltinDefault);
//    }
//    /// <summary>
//    /// Lightweight-DefaultTerrain.mat
//    /// </summary>
//    /// <returns></returns>
//    public override Material GetDefaultTerrainMaterial()
//    {
//        return GetMaterial(DefaultMaterialType.Terrain);
//    }
//    /// <summary>
//    /// null
//    /// </summary>
//    /// <returns></returns>
//    public override Material GetDefaultUIMaterial()
//    {
//        return GetMaterial(DefaultMaterialType.UnityBuiltinDefault);
//    }
//    /// <summary>
//    /// null
//    /// </summary>
//    /// <returns></returns>
//    public override Material GetDefaultUIOverdrawMaterial()
//    {
//        return GetMaterial(DefaultMaterialType.UnityBuiltinDefault);
//    }
//    /// <summary>
//    /// null
//    /// </summary>
//    /// <returns></returns>
//    public override Material GetDefaultUIETC1SupportedMaterial()
//    {
//        return GetMaterial(DefaultMaterialType.UnityBuiltinDefault);
//    }
//    /// <summary>
//    /// null
//    /// </summary>
//    /// <returns></returns>
//    public override Material GetDefault2DMaterial()
//    {
//        return GetMaterial(DefaultMaterialType.UnityBuiltinDefault);
//    }
//    /// <summary>
//    /// 获取默认shader
//    /// LightweightShaderUtils内有静态数据
//    /// 实际字符串为"LightweightPipeline/Standard (Physically Based)"
//    /// Lightweight-Default.mat也使用该Shader
//    /// </summary>
//    /// <returns></returns>
//    public override Shader GetDefaultShader()
//    {
//        if (m_DefaultShader == null)
//            m_DefaultShader = Shader.Find(LightweightShaderUtils.GetShaderPath(ShaderPathID.STANDARD_PBS));
//        return m_DefaultShader;
//    }

//    /// <summary>
//    /// "Hidden/LightweightPipeline/BlitShader"
//    /// </summary>
//    public Shader blitShader
//    {
//        get { return resources != null ? resources.BlitShader : null; }
//    }
//    /// <summary>
//    /// "Hidden/LightweightPipeline/CopyDepthShader"
//    /// </summary>
//    public Shader copyDepthShader
//    {
//        get { return resources != null ? resources.CopyDepthShader : null; }
//    }
//    /// <summary>
//    /// "Hidden/LightweightPipeline/ScreenSpaceShadows"
//    /// </summary>
//    public Shader screenSpaceShadowShader
//    {
//        get { return resources != null ? resources.ScreenSpaceShadowShader : null; }
//    }
//    /// <summary>
//    /// "Hidden/LightweightPipeline/SamplingShader"
//    /// </summary>
//    public Shader samplingShader
//    {
//        get { return resources != null ? resources.SamplingShader : null; }
//    }

//    public void OnBeforeSerialize()
//    {
//    }

//    /// <summary>
//    /// m_ShadowType已弃用，将旧版资源的m_ShadowType转为m_SoftShadowsSupported字段
//    /// </summary>
//    public void OnAfterDeserialize()
//    {
//        if (k_AssetVersion < 3)
//        {
//            k_AssetVersion = 3;
//            m_SoftShadowsSupported = (m_ShadowType == ShadowType.SOFT_SHADOWS);
//        }
//    }

//}

//public class LightweightPipelineResources : ScriptableObject
//{
//    public Shader BlitShader;
//    public Shader CopyDepthShader;
//    public Shader ScreenSpaceShadowShader;
//    public Shader SamplingShader;
//}

//public class LightweightPipelineEditorResources : ScriptableObject
//{
//    public Material DefaultMaterial;
//    public Material DefaultParticleMaterial;
//    public Material DefaultTerrainMaterial;
//}
