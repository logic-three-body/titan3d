﻿using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EngineNS.Graphics.Pipeline.Shader
{
    [Rtti.Meta]
    public partial class UMaterialAMeta : IO.IAssetMeta
    {
        public override string GetAssetExtType()
        {
            return UMaterial.AssetExt;
        }
        public override string GetAssetTypeName()
        {
            return "Material";
        }
        public override async System.Threading.Tasks.Task<IO.IAsset> LoadAsset()
        {
            return await UEngine.Instance.GfxDevice.MaterialManager.GetMaterial(GetAssetName());
        }
        public override void DeleteAsset(string name, RName.ERNameType type)
        {
            var address = RName.GetAddress(type, name);
            IO.FileManager.DeleteFile(address);
            IO.FileManager.DeleteFile(address + IO.IAssetMeta.MetaExt);
        }
        public override bool CanRefAssetType(IO.IAssetMeta ameta)
        {
            //必须是TextureAsset
            return true;
        }
        protected override Color GetBorderColor()
        {
            return Color.Gold;
        }
        //public override void OnDrawSnapshot(in ImDrawList cmdlist, ref Vector2 start, ref Vector2 end)
        //{
        //    base.OnDrawSnapshot(in cmdlist, ref start, ref end);
        //    cmdlist.AddText(in start, 0xFFFFFFFF, "Mtl", null);
        //}
    }
    [Rtti.Meta]
    [UMaterial.MaterialImport]
    [IO.AssetCreateMenu(MenuName = "Material")]
    public partial class UMaterial : IO.ISerializer, IO.IAsset, IShaderCodeProvider
    {
        public const string AssetExt = ".material";
        public UMaterial()
        {
            mPipelineDesc.SetDefault();
        }
        public bool IsEditingMaterial { get; set; }

        #region SystemVar
        public class VSInput
        {
            public Vector3 vPosition;
	        public Vector3 vNormal;
	        public Vector4 vTangent;
	        public Vector4 vColor;
	        public Vector2 vUV;
	        public Vector2 vLightMap;
	        public UInt32_4 vSkinIndex;
	        public Vector4 vSkinWeight;
	        public UInt32_4 vTerrainIndex;
	        public UInt32_4 vTerrainGradient;
	        public Vector3 vInstPos;
	        public Vector4 vInstQuat;
	        public Vector4 vInstScale;
	        public UInt32_4 vF4_1;
	        public Vector4 vF4_2;
	        public Vector4 vF4_3;
            public uint vInstanceId;
        }
        public class PSInput
        {
            public Vector4 vPosition;
            public Vector3 vNormal;
	        public Vector4 vColor;
            public Vector2 vUV;
	        public Vector3 vWorldPos;   //the 4th channel is unused just for now;
	        public Vector4 vTangent;
	        public Vector4 vLightMap;
	        public Vector4 psCustomUV0;
	        public Vector4 psCustomUV1;
	        public Vector4 psCustomUV2;
	        public Vector4 psCustomUV3;
	        public Vector4 psCustomUV4;
	        public UInt32_4 PointLightIndices;
	        public UInt32_4 vF4_1;
	        public Vector4 vF4_2;
	        public Vector4 vF4_3;
            public UInt32_4 SpecialData;
        }
        public class MTLOutput
        {
            public Vector3 mAlbedo;
            public Vector3 mNormal;
            public float mMetallic;
            public float mRough;   //in the editer,we call it smoth,so rough = 1.0f - smoth;
            public float mAbsSpecular;
            public float mTransmit;
            public Vector3 mEmissive;
            public float mFuzz;
            public float mIridescence;
            public float mDistortion;
            public float mAlpha;
            public float mAlphaTest;
            public Vector3 mVertexOffset;
            public Vector3 mSubAlbedo;
            public float mAO;
            public float mMask;
            public Vector3 mShadowColor;
            public float mDeepShadow;
            public Vector3 mMoodColor;
        };
        #endregion

        public override string ToString()
        {
            string result = $"Materai:{AssetName}\n";
            return result;
        }
        protected Hash160 mMaterialHash;
        [Rtti.Meta]
        [Browsable(false)]
        public virtual Hash160 MaterialHash
        {
            get
            {
                return mMaterialHash;
            }
            set
            {
                mMaterialHash = value;
            }
        }
        public Hash160 GetHash()
        {
            string result = DefineCode?.TextCode;
            result += SourceCode?.TextCode;
            mMaterialHash = Hash160.CreateHash160(result);
            return mMaterialHash;
        }
        internal uint mSerialId = 0;
        [Browsable(false)]
        public virtual uint SerialId
        {
            get => mSerialId;
            set
            {
                mSerialId = value;
                if (PerMaterialCBuffer != null)
                    this.UpdateUniformVars(PerMaterialCBuffer, PerMaterialCBuffer.ShaderBinder);
            }
        }
        public class MaterialImportAttribute : IO.CommonCreateAttribute
        {
            public override void DoCreate(RName dir, Rtti.UTypeDesc type, string ext)
            {
                base.DoCreate(dir, type, ext);

                var material = (mAsset as UMaterial);
                material.mPipelineDesc.SetDefault();
                material.UpdateShaderCode(true);
            }
        }
        #region IAsset
        public virtual IO.IAssetMeta CreateAMeta()
        {
            var result = new UMaterialAMeta();
            return result;
        }
        public virtual IO.IAssetMeta GetAMeta()
        {
            return UEngine.Instance.AssetMetaManager.GetAssetMeta(AssetName);
        }
        public virtual void UpdateAMetaReferences(IO.IAssetMeta ameta)
        {
            ameta.RefAssetRNames.Clear();
            foreach (var i in UsedRSView)
            {
                if (i.Value == null)
                    continue;
                ameta.AddReferenceAsset(i.Value);
            }
        }
        public virtual void SaveAssetTo(RName name)
        {
            var ameta = this.GetAMeta();
            if (ameta != null)
            {
                UpdateAMetaReferences(ameta);
                ameta.SaveAMeta();
            }

            var typeStr = Rtti.UTypeDescManager.Instance.GetTypeStringFromType(this.GetType());
            var xnd = new IO.CXndHolder(typeStr, 0, 0);
            using (var attr = xnd.NewAttribute("Material", 0, 0))
            {
                var ar = attr.GetWriter(512);
                ar.Write(this);
                attr.ReleaseWriter(ref ar);
                xnd.RootNode.AddAttribute(attr);
            }

            xnd.SaveXnd(name.Address);
        }
        public static bool ReloadXnd(UMaterial material, UMaterialManager manager, IO.CXndNode node)
        {
            var attr = node.TryGetAttribute("Material");
            if (attr.NativePointer != IntPtr.Zero)
            {
                var ar = attr.GetReader(null);
                try
                {
                    ar.ReadTo(material, null);
                    material.UpdateShaderCode(false);
                    material.SerialId++;
                }
                catch (Exception ex)
                {
                    Profiler.Log.WriteException(ex);
                }
                attr.ReleaseReader(ref ar);
            }
            return true;
        }
        [Rtti.Meta]
        [RName.PGRName(ReadOnly = true)]
        public RName AssetName
        {
            get;
            set;
        }
        #endregion

        [EGui.Controls.PropertyGrid.PGCustomValueEditor(HideInPG = true)]
        public virtual UMaterial ParentMaterial
        {
            get { return this; }
            protected set { }
        }
        public virtual void OnPreRead(object tagObject, object hostObject, bool fromXml)
        {

        }
        public virtual void OnPropertyRead(object tagObject, System.Reflection.PropertyInfo prop, bool fromXml)
        {

        }
        public virtual void GetDefines(List<KeyValuePair<string, string>> vars)
        {
        }
        public static UMaterial LoadXnd(UMaterialManager manager, IO.CXndNode node)
        {
            IO.ISerializer result = null;
            var attr = node.TryGetAttribute("Material");
            if (attr.NativePointer != IntPtr.Zero)
            {
                var ar = attr.GetReader(null);
                ar.Read(out result, null);
                attr.ReleaseReader(ref ar);
            }

            var material = result as UMaterial;
            if (material != null)
            {
                material.UpdateShaderCode(false);
                return material;
            }
            return null;
        }
        public enum ELightingMode
        {
            Stand,
            Unlight,
            Skin,
            Transmit,
            Hair,
            Eye,
        }
        [Rtti.Meta]
        public ELightingMode LightingMode 
        { 
            get; 
            set; 
        } = ELightingMode.Stand;
        protected ERenderLayer mRenderLayer = ERenderLayer.RL_Opaque;
        [Rtti.Meta]
        public ERenderLayer RenderLayer
        {
            get => mRenderLayer;
            set
            {
                mRenderLayer = value;
                SerialId++;
            }
        }
        [Rtti.Meta]
        public bool AlphaTest
        {
            get;
            set;
        } = true;
        [Rtti.Meta]
        public List<string> UserDefines { get; set; } = new List<string>();
        internal virtual void UpdateShaderCode(bool EmptyMaterial)
        {
            var codeBuilder = new Bricks.CodeBuilder.Backends.UHLSLCodeGenerator();
            string sourceCode = "";
            codeBuilder.AddLine($"#ifndef _Material_H_", ref sourceCode);
            codeBuilder.AddLine($"#define _Material_H_", ref sourceCode);

            foreach (var i in this.UsedRSView)
            {
                codeBuilder.AddLine($"{i.ShaderType} {i.Name} DX_AUTOBIND;", ref sourceCode);
            }

            foreach (var i in this.UsedSamplerStates)
            {
                codeBuilder.AddLine($"SamplerState {i.Name} DX_AUTOBIND;", ref sourceCode);
            }

            //Defines.AddDefine("USE_VS_UV", "1");
            //Defines.AddDefine("USE_VS_Color", "1");

            if (EmptyMaterial)
            {
                this.HLSLCode = "void DO_VS_MATERIAL_IMPL(in PS_INPUT input, inout MTL_OUTPUT mtl)\n{\n}\n";
                this.HLSLCode += "void DO_PS_MATERIAL_IMPL(in PS_INPUT input, inout MTL_OUTPUT mtl)\n"+
                    "{\n" +
                        "mtl.mAlbedo = float3(0.5,0.5,0.5);\n" +
                        "mtl.mMetallic = 1.0f;\n" +
                        "mtl.mRough = 0.5f;\n" +
                        "mtl.mEmissive = float3(0.1,0.1,0.1);\n" +
                    "}\n";
                //codeBuilder.AddLine("void DO_PS_MATERIAL_IMPL(in PS_INPUT input, inout MTL_OUTPUT mtl)");
                //codeBuilder.PushBrackets();
                //codeBuilder.AddLine("mtl.mAlbedo = float3(0.5,0.5,0.5);");
                //codeBuilder.AddLine("mtl.mMetallic = 1.0f;");
                //codeBuilder.AddLine("mtl.mRough = 0.5f;");
                //codeBuilder.AddLine("mtl.mEmissive = float3(0.1,0.1,0.1);");
                //codeBuilder.PopBrackets();
                sourceCode += this.HLSLCode;
                //codeBuilder.AppendCode(this.HLSLCode, false, true);
            }
            else
            {
                sourceCode += this.HLSLCode;
                //codeBuilder.AppendCode(this.HLSLCode, false, true);
                if (sourceCode.IndexOf("DO_VS_MATERIAL_IMPL") < 0)
                {
                    codeBuilder.AddLine("void DO_VS_MATERIAL_IMPL(in PS_INPUT input, inout MTL_OUTPUT mtl)", ref sourceCode);
                    codeBuilder.PushSegment(ref sourceCode);
                    codeBuilder.PopSegment(ref sourceCode);
                }
            }

            codeBuilder.AddLine("#undef DO_VS_MATERIAL", ref sourceCode);
            codeBuilder.AddLine("#define DO_VS_MATERIAL DO_VS_MATERIAL_IMPL", ref sourceCode);
            codeBuilder.AddLine("#undef DO_PS_MATERIAL", ref sourceCode);
            codeBuilder.AddLine("#define DO_PS_MATERIAL DO_PS_MATERIAL_IMPL", ref sourceCode);

            switch (LightingMode)
            {
                case ELightingMode.Unlight:
                    codeBuilder.AddLine("#define MTL_ID_UNLIT", ref sourceCode);
                    break;
                case ELightingMode.Skin:
                    codeBuilder.AddLine("#define MTL_ID_SKIN", ref sourceCode);
                    break;
                case ELightingMode.Transmit:
                    codeBuilder.AddLine("#define MTL_ID_TRANSMIT", ref sourceCode);
                    break;
                case ELightingMode.Hair:
                    codeBuilder.AddLine("#define MTL_ID_HAIR", ref sourceCode);
                    break;
                case ELightingMode.Eye:
                    codeBuilder.AddLine("#define MTL_ID_EYE", ref sourceCode);
                    break;
            }

            if (AlphaTest)
            {
                codeBuilder.AddLine("#define ALPHA_TEST", ref sourceCode);
            }

            if (UserDefines != null)
            {
                foreach (var i in UserDefines)
                {
                    codeBuilder.AddLine($"#define {i}", ref sourceCode);
                }
            }

            codeBuilder.AddLine("#endif//_Material_H_", ref sourceCode);
            SourceCode.TextCode = sourceCode;

            string uniformVarsCode = "";
            foreach (var i in this.UsedUniformVars)
            {
                if (i.VarType == "float4")
                {
                    uniformVarsCode += $"{i.VarType} {i.Name} = float4({i.Value});";
                }
                else
                {
                    uniformVarsCode += $"{i.VarType} {i.Name};";
                }
            }
            DefineCode.TextCode = uniformVarsCode;

            mPerMaterialCBuffer = null;

            mMaterialHash = GetHash();
        }
        [EGui.Controls.PropertyGrid.PGCustomValueEditor(HideInPG = true)]
        public NxRHI.UShaderCode DefineCode { get; } = new NxRHI.UShaderCode();
        [EGui.Controls.PropertyGrid.PGCustomValueEditor(HideInPG = true)]
        public NxRHI.UShaderCode SourceCode { get; } = new NxRHI.UShaderCode();
        [EGui.Controls.PropertyGrid.PGCustomValueEditor(HideInPG = true)]
        public NxRHI.UShaderDefinitions Defines { get; } = new NxRHI.UShaderDefinitions();

        #region Data
        
        #region Code&Graph
        [Rtti.Meta(Flags = Rtti.MetaAttribute.EMetaFlags.DiscardWhenCooked)]
        [Browsable(false)]
        public string GraphXMLString
        {
            get;
            set;
        }
        string mHLSLCode;
        [Rtti.Meta(Flags = Rtti.MetaAttribute.EMetaFlags.DiscardWhenCooked)]
        [Browsable(false)]
        public string HLSLCode
        {
            get => mHLSLCode;
            set
            {
                mHLSLCode = value;
                MaterialHash = GetHash();
            }
        }
        #endregion
        #region Texture
        public class NameRNamePair : IO.BaseSerializer
        {
            public override void OnPreRead(object tagObject, object hostObject, bool fromXml)
            {
                HostMaterial = hostObject as UMaterial;
            }
            UMaterial HostMaterial;
            [Rtti.Meta()]
            public string Name { get; set; }
            RName mValue;
            [Rtti.Meta]
            [RName.PGRName(FilterExts = NxRHI.USrView.AssetExt)]
            public RName Value
            {
                get => mValue;
                set
                {
                    mValue = value;
                    if (HostMaterial != null)
                        HostMaterial.SerialId++;
                }
            }
            [Rtti.Meta]
            public string ShaderType { get; set; } = "Texture2D";
            public NameRNamePair Clone(UMaterial mtl)
            {
                var result = new NameRNamePair();
                result.HostMaterial = mtl;
                result.Name = Name;
                result.mValue = mValue;
                return result;
            }
            public object SrvObject { get; set; } = null;
        }
        protected List<NameRNamePair> mUsedRSView = new List<NameRNamePair>();
        [Rtti.Meta]
        [Category("Variable")]
        public List<NameRNamePair> UsedRSView { get => mUsedRSView; }
        public NameRNamePair FindSRV(string name)
        {
            foreach (var i in mUsedRSView)
            {
                if (i.Name == name)
                    return i;
            }
            return null;
        }
        public int NumOfSRV
        {
            get
            {
                return UsedRSView.Count;
            }
        }
        public string GetNameOfSRV(int index)
        {
            if (index < 0 || index >= UsedRSView.Count)
                return null;
            return UsedRSView[index]?.Name;
        }
        public virtual async System.Threading.Tasks.Task<NxRHI.USrView> GetSRV(int index)
        {
            var srv = UsedRSView[index].SrvObject as NxRHI.USrView;
            if (srv != null)
                return srv;
            return await UEngine.Instance.GfxDevice.TextureManager.GetTexture(UsedRSView[index].Value);
        }
        public NxRHI.USrView TryGetSRV(int index)
        {
            var srv = UsedRSView[index].SrvObject as NxRHI.USrView;
            if (srv != null)
                return srv;
            return UEngine.Instance.GfxDevice.TextureManager.TryGetTexture(UsedRSView[index].Value);
        }
        #endregion
        #region Sampler
        public class NameSamplerStateDescPair : IO.BaseSerializer
        {
            public NameSamplerStateDescPair()
            {
                mValue.SetDefault();
            }
            public override void OnPreRead(object tagObject, object hostObject, bool fromXml)
            {
                HostMaterial = hostObject as UMaterial;
            }
            UMaterial HostMaterial;
            [Rtti.Meta()]
            public string Name { get; set; }
            internal NxRHI.FSamplerDesc mValue;
            [Rtti.Meta]
            public NxRHI.FSamplerDesc Value
            {
                get => mValue;
                set
                {
                    mValue = value;
                    if (HostMaterial != null)
                        HostMaterial.SerialId++;
                }
            }
            public NameSamplerStateDescPair Clone(UMaterial mtl)
            {
                var result = new NameSamplerStateDescPair();
                result.HostMaterial = mtl;
                result.Name = Name;
                result.mValue = mValue;
                return result;
            }
        }
        protected List<NameSamplerStateDescPair> mUsedSamplerStates = new List<NameSamplerStateDescPair>();
        [Rtti.Meta]
        [Category("Variable")]
        public List<NameSamplerStateDescPair> UsedSamplerStates { get => mUsedSamplerStates; }
        public int NumOfSampler
        {
            get
            {
                return mUsedSamplerStates.Count;
            }
        }
        public string GetNameOfSampler(int index)
        {
            if (index < 0 || index >= mUsedSamplerStates.Count)
                return null;
            return mUsedSamplerStates[index].Name;
        }
        public NxRHI.USampler GetSampler(int index)
        {
            if (mUsedSamplerStates[index].mValue.m_AddressU == 0)
            {
                mUsedSamplerStates[index].mValue.SetDefault();
            }
            //if (mUsedSamplerStates[index].mValue.m_AddressV == 0)
            //{
            //    mUsedSamplerStates[index].mValue.m_AddressV = EAddressMode.ADM_WRAP;
            //}
            //if (mUsedSamplerStates[index].mValue.m_AddressW == 0)
            //{
            //    mUsedSamplerStates[index].mValue.m_AddressW = EAddressMode.ADM_WRAP;
            //}
            return UEngine.Instance.GfxDevice.SamplerStateManager.GetPipelineState(UEngine.Instance.GfxDevice.RenderContext, in mUsedSamplerStates[index].mValue);
        }
        public NameSamplerStateDescPair FindSampler(string name)
        {
            foreach (var i in mUsedSamplerStates)
            {
                if (i.Name == name)
                    return i;
            }
            return null;
        }
        #endregion
        #region UniformVar
        public class NameValuePair : IO.BaseSerializer
        {
            public override void OnPreRead(object tagObject, object hostObject, bool fromXml)
            {
                HostMaterial = hostObject as UMaterial;
            }
            UMaterial HostMaterial;
            [Rtti.Meta]
            public string VarType { get; set; }
            [Rtti.Meta]
            public string Name { get; set; }
            string mValue;
            [Rtti.Meta]
            public string Value
            {
                get => mValue;
                set
                {
                    GamePlay.Action.UAction.OnChanged(HostMaterial, this, "Value", mValue, value);
                    mValue = value;
                    if (HostMaterial != null)
                        HostMaterial.SerialId++;
                }
            }
            public bool SetValue(in Vector4 v)
            {
                if (VarType != "float4")
                    return false;
                Value = v.ToString();
                return true;
            }
            public bool SetValue(in Vector3 v)
            {
                if (VarType != "float3")
                    return false;
                Value = v.ToString();
                return true;
            }
            public bool SetValue(in Vector2 v)
            {
                if (VarType != "float2")
                    return false;
                Value = v.ToString();
                return true;
            }
            public bool SetValue(float v)
            {
                if (VarType != "float")
                    return false;
                Value = v.ToString();
                return true;
            }
            public NameValuePair Clone(UMaterial mtl)
            {
                var result = new NameValuePair();
                result.HostMaterial = mtl;
                result.VarType = VarType;
                result.Name = Name;
                result.mValue = mValue;
                return result;
            }
        }
        protected List<NameValuePair> mUsedUniformVars = new List<NameValuePair>();
        [Rtti.Meta]
        [Category("Variable")]
        public List<NameValuePair> UsedUniformVars { get => mUsedUniformVars; }
        public NameValuePair FindVar(string name)
        {
            foreach(var i in mUsedUniformVars)
            {
                if (i.Name == name)
                    return i;
            }
            return null;
        }
        public virtual int NumOfUniformVars
        {
            get
            {
                return UsedUniformVars.Count;
            }
        }
        public virtual string GetNameOfUniformVar(int index)
        {
            if (index < 0 || index >= UsedUniformVars.Count)
                return null;
            return UsedUniformVars[index]?.Name;
        }
        internal unsafe virtual void UpdateUniformVars(NxRHI.UCbView cBuffer, NxRHI.FShaderBinder binder)
        {
            foreach (var i in UsedUniformVars)
            {
                var desc = binder.FindField(i.Name);
                switch (desc.Type)
                {
                    case NxRHI.EShaderVarType.SVT_Float:
                        {
                            switch (desc.Columns)
                            {
                                case 1:
                                    {
                                        var v = System.Convert.ToSingle(i.Value);
                                        cBuffer.SetValue(desc, in v);
                                    }
                                    break;
                                case 2:
                                    {
                                        var v = Vector2.FromString(i.Value);
                                        cBuffer.SetValue(desc, in v);
                                    }
                                    break;
                                case 3:
                                    {
                                        var v = Vector3.FromString(i.Value);
                                        cBuffer.SetValue(desc, in v);
                                    }
                                    break;
                                case 4:
                                    {
                                        var v = Vector4.FromString(i.Value);
                                        cBuffer.SetValue(desc, in v);
                                    }
                                    break;
                            }
                        }
                        break;
                    case NxRHI.EShaderVarType.SVT_Int:
                        {
                            switch (desc.Columns)
                            {
                                case 1:
                                    {
                                        var v = System.Convert.ToInt32(i.Value);
                                        cBuffer.SetValue(desc, in v);
                                    }
                                    break;
                                case 2:
                                    {
                                        var v = Vector2.FromString(i.Value);
                                        cBuffer.SetValue(desc, in v);
                                    }
                                    break;
                                case 3:
                                    {
                                        var v = Vector3.FromString(i.Value);
                                        cBuffer.SetValue(desc, in v);
                                    }
                                    break;
                                case 4:
                                    {
                                        var v = Vector4.FromString(i.Value);
                                        cBuffer.SetValue(desc, in v);
                                    }
                                    break;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion

        #endregion

        #region RHIResource
        protected NxRHI.FGpuPipelineDesc mPipelineDesc;
        NxRHI.UGpuPipeline mPipeline;
        public NxRHI.UGpuPipeline Pipeline
        {
            get
            {
                if (mPipeline == null)
                    UpdatePipeline();
                return mPipeline;
            }
        }
        private void UpdatePipeline()
        {
            mPipeline = UEngine.Instance.GfxDevice.PipelineManager.GetPipelineState(UEngine.Instance.GfxDevice.RenderContext, in mPipelineDesc);
        }
        NxRHI.UCbView mPerMaterialCBuffer;
        [Browsable(false)]
        public NxRHI.UCbView PerMaterialCBuffer 
        {
            get
            {
                return mPerMaterialCBuffer;
            }
        }
        public bool CreateCBuffer(UEffect effect)
        {
            var binder = effect.ShaderEffect.FindBinder("cbPerMaterial");
            if (binder == null)
                return false;
            mPerMaterialCBuffer = UEngine.Instance.GfxDevice.RenderContext.CreateCBV(binder);
            return true;
        }
        [Rtti.Meta]
        [Category("State")]
        public NxRHI.FRasterizerDesc Rasterizer
        {
            get => mPipelineDesc.m_Rasterizer;
            set
            {
                mPipelineDesc.m_Rasterizer = value; 
                UpdatePipeline();
            }
    }
        [Rtti.Meta]
        [Category("State")]
        public NxRHI.FDepthStencilDesc DepthStencil
        {
            get => mPipelineDesc.m_DepthStencil;
            set
            {
                mPipelineDesc.m_DepthStencil = value;
                UpdatePipeline();
            }
        }
        [Rtti.Meta]
        [Category("State")]
        public NxRHI.FBlendDesc Blend
        {
            get => mPipelineDesc.m_Blend;
            set
            {
                mPipelineDesc.m_Blend = value;
                UpdatePipeline();
            }
        }
        #endregion
    }
    public class UMaterialManager
    {
        public void Cleanup()
        {
            Materials.Clear();
        }
        public async System.Threading.Tasks.Task Initialize(UGfxDevice device)
        {
            await Thread.AsyncDummyClass.DummyFunc();
            ScreenMaterial = new UMaterial();

            var dsDesc = new NxRHI.FDepthStencilDesc();
            dsDesc.SetDefault();
            dsDesc.m_DepthEnable = 0;
            dsDesc.m_DepthWriteMask = 0;
            ScreenMaterial.DepthStencil = dsDesc;

            PxDebugMaterial = await this.CreateMaterial(RName.GetRName("material/sysdft_color.material", RName.ERNameType.Engine));
        }
        public UMaterial ScreenMaterial;
        public UMaterial PxDebugMaterial;
        public Dictionary<RName, UMaterial> Materials { get; } = new Dictionary<RName, UMaterial>();
        public async System.Threading.Tasks.Task<UMaterial> CreateMaterial(RName rn)
        {
            UMaterial result;
            result = await UEngine.Instance.EventPoster.Post(() =>
            {
                using (var xnd = IO.CXndHolder.LoadXnd(rn.Address))
                {
                    if (xnd != null)
                    {
                        var material = UMaterial.LoadXnd(this, xnd.RootNode);
                        if (material == null)
                            return null;

                        material.AssetName = rn;
                        return material;
                    }
                    else
                    {
                        return null;
                    }
                }
            }, Thread.Async.EAsyncTarget.AsyncIO);

            return result;
        }
        public async System.Threading.Tasks.Task<bool> ReloadMaterial(RName rn)
        {
            UMaterial result;
            if (Materials.TryGetValue(rn, out result) == false)
                return true;

            var ok = await UEngine.Instance.EventPoster.Post(() =>
            {
                using (var xnd = IO.CXndHolder.LoadXnd(rn.Address))
                {
                    if (xnd != null)
                    {
                        return UMaterial.ReloadXnd(result, this, xnd.RootNode);
                    }
                    else
                    {
                        return false;
                    }
                }
            }, Thread.Async.EAsyncTarget.AsyncIO);

            var effects = UEngine.Instance.GfxDevice.EffectManager.Effects;
            foreach (var i in effects)
            {
                if (i.Value.Desc.MaterialName == rn)
                {
                    if (i.Value.Desc.MaterialHash != result.MaterialHash)
                    {
                        await i.Value.RefreshEffect(result);
                    }
                }
            }

            return ok;
        }
        public async System.Threading.Tasks.Task<UMaterial> GetMaterial(RName rn)
        {
            if (rn == null)
                return null;

            UMaterial result;
            if (Materials.TryGetValue(rn, out result))
                return result;

            result = await UEngine.Instance.EventPoster.Post(() =>
            {
                using (var xnd = IO.CXndHolder.LoadXnd(rn.Address))
                {
                    if (xnd != null)
                    {
                        var material = UMaterial.LoadXnd(this, xnd.RootNode);
                        if (material == null)
                            return null;

                        material.AssetName = rn;
                        return material;
                    }
                    else
                    {
                        return null;
                    }
                }   
            }, Thread.Async.EAsyncTarget.AsyncIO);

            if (result != null)
            {
                Materials[rn] = result;
                return result;
            }

            return null;
        }
    }
}
