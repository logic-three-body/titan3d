﻿using EngineNS.Bricks.VXGI;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Graphics.Pipeline.Deferred
{
    public class UOpaqueShading : Shader.UShadingEnv
    {
        public UPermutationItem DisableAO
        {
            get;
            set;
        }
        public UPermutationItem DisablePointLights
        {
            get;
            set;
        }
        public UPermutationItem DisableShadow
        {
            get;
            set;
        }
        public UOpaqueShading()
        {
            CodeName = RName.GetRName("shaders/ShadingEnv/Mobile/MobileOpaque.cginc", RName.ERNameType.Engine);

            this.BeginPermutaion();
            DisableAO = this.PushPermutation<Shader.EPermutation_Bool>("ENV_DISABLE_AO", (int)Shader.EPermutation_Bool.BitWidth);
            DisablePointLights = this.PushPermutation<Shader.EPermutation_Bool>("ENV_DISABLE_POINTLIGHTS", (int)Shader.EPermutation_Bool.BitWidth);
            DisableShadow = this.PushPermutation<Shader.EPermutation_Bool>("DISABLE_SHADOW_ALL", (int)Shader.EPermutation_Bool.BitWidth);
            var editorMode = this.PushPermutation<Shader.EPermutation_Bool>("MODE_EDITOR", (int)Shader.EPermutation_Bool.BitWidth);

            DisableAO.SetValue((int)Shader.EPermutation_Bool.FalseValue);
            DisableShadow.SetValue((int)Shader.EPermutation_Bool.FalseValue);
            DisablePointLights.SetValue((int)Shader.EPermutation_Bool.FalseValue);
            editorMode.SetValue((int)Shader.EPermutation_Bool.TrueValue);

            UpdatePermutation();
        }
        public override NxRHI.EVertexStreamType[] GetNeedStreams()
        {
            return new NxRHI.EVertexStreamType[] { NxRHI.EVertexStreamType.VST_Position,
                NxRHI.EVertexStreamType.VST_Normal,
                NxRHI.EVertexStreamType.VST_Tangent,
                NxRHI.EVertexStreamType.VST_Color,
                NxRHI.EVertexStreamType.VST_LightMap,
                NxRHI.EVertexStreamType.VST_UV,};
        }
    }
    public class UTranslucentShading : Shader.UShadingEnv
    {
        public UTranslucentShading()
        {
            CodeName = RName.GetRName("shaders/ShadingEnv/Deferred/DeferredTranslucent.cginc", RName.ERNameType.Engine);
        }
        public override NxRHI.EVertexStreamType[] GetNeedStreams()
        {
            return new NxRHI.EVertexStreamType[] { NxRHI.EVertexStreamType.VST_Position,
                NxRHI.EVertexStreamType.VST_Normal,
                NxRHI.EVertexStreamType.VST_Tangent,
                NxRHI.EVertexStreamType.VST_Color,
                NxRHI.EVertexStreamType.VST_LightMap,
                NxRHI.EVertexStreamType.VST_UV,};
        }
    }
    public class UForwordNode : Common.UBasePassNode
    {
        public Common.URenderGraphPin ColorPinInOut = Common.URenderGraphPin.CreateInputOutput("Color");
        public Common.URenderGraphPin DepthPinInOut = Common.URenderGraphPin.CreateInputOutput("Depth");
        public Common.URenderGraphPin GizmosDepthPinOut = Common.URenderGraphPin.CreateOutput("GizmosDepth", true, EPixelFormat.PXF_D24_UNORM_S8_UINT);
        public UForwordNode()
        {
            Name = "UForwordNode";
        }
        public override void InitNodePins()
        {
            AddInputOutput(ColorPinInOut, NxRHI.EBufferType.BFT_RTV | NxRHI.EBufferType.BFT_SRV);
            AddInputOutput(DepthPinInOut, NxRHI.EBufferType.BFT_DSV | NxRHI.EBufferType.BFT_SRV);

            AddOutput(GizmosDepthPinOut, NxRHI.EBufferType.BFT_DSV | NxRHI.EBufferType.BFT_SRV);
        }
        public UOpaqueShading mOpaqueShading;
        public UTranslucentShading mTranslucentShading;
        public UPassDrawBuffers BasePass = new UPassDrawBuffers();
        public NxRHI.URenderPass RenderPass;
        public NxRHI.URenderPass GizmosRenderPass;

        public override async System.Threading.Tasks.Task Initialize(URenderPolicy policy, string debugName)
        {
            await Thread.AsyncDummyClass.DummyFunc();

            var dfPolicy = policy;// as UDeferredPolicy;
            var rc = UEngine.Instance.GfxDevice.RenderContext;
            BasePass.Initialize(rc, "ForwordPass");

            var PassDesc = new NxRHI.FRenderPassDesc();
            unsafe
            {
                PassDesc.NumOfMRT = 1;
                PassDesc.AttachmentMRTs[0].Format = ColorPinInOut.Attachement.Format;
                PassDesc.AttachmentMRTs[0].Samples = 1;
                PassDesc.AttachmentMRTs[0].LoadAction = NxRHI.EFrameBufferLoadAction.LoadActionDontCare;
                PassDesc.AttachmentMRTs[0].StoreAction = NxRHI.EFrameBufferStoreAction.StoreActionStore;
                PassDesc.m_AttachmentDepthStencil.Format = DepthPinInOut.Attachement.Format;// dfPolicy.BasePassNode.GBuffers.DepthStencil.AttachBuffer.Srv.mCoreObject.GetFormat(); //dsFmt;
                PassDesc.m_AttachmentDepthStencil.Samples = 1;
                PassDesc.m_AttachmentDepthStencil.LoadAction = NxRHI.EFrameBufferLoadAction.LoadActionDontCare;
                PassDesc.m_AttachmentDepthStencil.StoreAction = NxRHI.EFrameBufferStoreAction.StoreActionStore;
                PassDesc.m_AttachmentDepthStencil.StencilLoadAction = NxRHI.EFrameBufferLoadAction.LoadActionDontCare;
                PassDesc.m_AttachmentDepthStencil.StencilStoreAction = NxRHI.EFrameBufferStoreAction.StoreActionStore;
                //PassDesc.mFBClearColorRT0 = new Color4(1, 0, 0, 0);
                //PassDesc.mDepthClearValue = 1.0f;
                //PassDesc.mStencilClearValue = 0u;
            }
            RenderPass = UEngine.Instance.GfxDevice.RenderPassManager.GetPipelineState<NxRHI.FRenderPassDesc>(rc, in PassDesc);

            var GizmosPassDesc = new NxRHI.FRenderPassDesc();
            unsafe
            {
                GizmosPassDesc.NumOfMRT = 1;
                GizmosPassDesc.AttachmentMRTs[0].Format = ColorPinInOut.Attachement.Format;
                GizmosPassDesc.AttachmentMRTs[0].Samples = 1;
                GizmosPassDesc.AttachmentMRTs[0].LoadAction = NxRHI.EFrameBufferLoadAction.LoadActionDontCare;
                GizmosPassDesc.AttachmentMRTs[0].StoreAction = NxRHI.EFrameBufferStoreAction.StoreActionStore;
                GizmosPassDesc.m_AttachmentDepthStencil.Format = GizmosDepthPinOut.Attachement.Format;
                GizmosPassDesc.m_AttachmentDepthStencil.Samples = 1;
                GizmosPassDesc.m_AttachmentDepthStencil.LoadAction = NxRHI.EFrameBufferLoadAction.LoadActionClear;
                GizmosPassDesc.m_AttachmentDepthStencil.StoreAction = NxRHI.EFrameBufferStoreAction.StoreActionStore;
                GizmosPassDesc.m_AttachmentDepthStencil.StencilLoadAction = NxRHI.EFrameBufferLoadAction.LoadActionClear;
                GizmosPassDesc.m_AttachmentDepthStencil.StencilStoreAction = NxRHI.EFrameBufferStoreAction.StoreActionStore;
                //GizmosPassDesc.mFBClearColorRT0 = new Color4(1, 0, 0, 0);
                //GizmosPassDesc.mDepthClearValue = 1.0f;
                //GizmosPassDesc.mStencilClearValue = 0u;
            }
            GizmosRenderPass = UEngine.Instance.GfxDevice.RenderPassManager.GetPipelineState<NxRHI.FRenderPassDesc>(rc, in GizmosPassDesc);

            GBuffers.Initialize(dfPolicy, RenderPass);
            GBuffers.SetRenderTarget(dfPolicy, 0, ColorPinInOut);
            GBuffers.SetDepthStencil(dfPolicy, DepthPinInOut);
            GBuffers.TargetViewIdentifier = policy.DefaultCamera.TargetViewIdentifier;

            GGizmosBuffers.Initialize(dfPolicy, GizmosRenderPass);
            GGizmosBuffers.SetRenderTarget(dfPolicy, 0, ColorPinInOut);
            GGizmosBuffers.SetDepthStencil(dfPolicy, DepthPinInOut);
            GGizmosBuffers.TargetViewIdentifier = policy.DefaultCamera.TargetViewIdentifier;

            mOpaqueShading = UEngine.Instance.ShadingEnvManager.GetShadingEnv<UOpaqueShading>();
            mTranslucentShading = UEngine.Instance.ShadingEnvManager.GetShadingEnv<UTranslucentShading>();
        }
        public override void Cleanup()
        {
            if (mOpaqueShading == null)
                return;
            GBuffers?.Cleanup();
            GBuffers = null;

            GGizmosBuffers?.Cleanup();
            GGizmosBuffers = null;

            base.Cleanup();
        }
        public override void OnResize(URenderPolicy policy, float x, float y)
        {
            if (mOpaqueShading == null)
                return;
            if (GBuffers != null)
            {
                GBuffers.OnResize(x, y);

                if (GGizmosBuffers != null)
                {
                    GGizmosBuffers.OnResize(x, y);
                }
            }
        }
        [ThreadStatic]
        private static Profiler.TimeScope ScopeTick = Profiler.TimeScopeManager.GetTimeScope(typeof(UForwordNode), nameof(TickLogic));
        public override void TickLogic(GamePlay.UWorld world, URenderPolicy policy, bool bClear)
        {
            using (new Profiler.TimeScopeHelper(ScopeTick))
            {
                if (mOpaqueShading == null)
                    return;
                BasePass.ClearMeshDrawPassArray();

                foreach (var i in policy.VisibleMeshes)
                {
                    if (i.Atoms == null)
                        continue;

                    for (int j = 0; j < i.Atoms.Length; j++)
                    {
                        if (i.Atoms[j].Material == null)
                            continue;
                        var layer = i.Atoms[j].Material.RenderLayer;
                        if (layer == ERenderLayer.RL_Opaque)
                            continue;

                        var drawcall = i.GetDrawCall(GBuffers, j, policy, URenderPolicy.EShadingType.BasePass, this);
                        if (drawcall != null)
                        {
                            drawcall.BindGBuffer(policy.DefaultCamera, GBuffers);
                            //GGizmosBuffers.PerViewportCBuffer = GBuffers.PerViewportCBuffer;

                            BasePass.PushDrawCall(layer, drawcall);
                        }
                    }
                }

                var passClear = new NxRHI.FRenderPassClears();
                passClear.SetDefault();
                passClear.SetClearColor(0, new Color4(1, 0, 0, 0));
                GBuffers.BuildFrameBuffers(policy);
                GGizmosBuffers.BuildFrameBuffers(policy);
                BasePass.BuildRenderPass(policy, in GBuffers.Viewport, in passClear, GBuffers, GGizmosBuffers, "Forword:");
            }   
        }
        public override void TickSync(URenderPolicy policy)
        {
            if (mOpaqueShading == null)
                return;
            BasePass.SwapBuffer();
            //GBuffers?.Camera?.mCoreObject.UpdateConstBufferData(UEngine.Instance.GfxDevice.RenderContext.mCoreObject, 1);
        }
    }
}
