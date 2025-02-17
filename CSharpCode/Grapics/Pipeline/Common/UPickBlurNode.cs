﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Graphics.Pipeline.Common
{
    public class UPickBlurShading : Shader.UShadingEnv
    {
        public UPickBlurShading()
        {
            CodeName = RName.GetRName("shaders/ShadingEnv/Sys/pick/pick_blur.cginc", RName.ERNameType.Engine);
        }
        public override NxRHI.EVertexStreamType[] GetNeedStreams()
        {
            return new NxRHI.EVertexStreamType[] { NxRHI.EVertexStreamType.VST_Position,
                NxRHI.EVertexStreamType.VST_UV,};
        }
        public unsafe override void OnBuildDrawCall(URenderPolicy policy, NxRHI.UGraphicDraw drawcall)
        {
            //var cbIndex = drawcall.mCoreObject.FindCBufferIndex("cbPerShadingEnv");
            //if (cbIndex != 0xFFFFFFFF)
            //{
            //    if (PerShadingCBuffer == null)
            //    {
            //        PerShadingCBuffer = UEngine.Instance.GfxDevice.RenderContext.CreateConstantBuffer(gpuProgram, cbIndex);
            //        PerShadingCBuffer.SetMatrix(0, ref Matrix.mIdentity);
            //        var RenderColor = new Color4(1, 1, 1, 1);
            //        PerShadingCBuffer.SetValue(1, ref RenderColor);
            //    }
            //    drawcall.mCoreObject.BindCBufferAll(cbIndex, PerShadingCBuffer.mCoreObject.Ptr);
            //}
        }
        public unsafe override void OnDrawCall(Pipeline.URenderPolicy.EShadingType shadingType, NxRHI.UGraphicDraw drawcall, URenderPolicy policy, Mesh.UMesh mesh)
        {
            base.OnDrawCall(shadingType, drawcall, policy, mesh);

            var Manager = policy.TagObject as URenderPolicy;

            var pickBlurNode = Manager.FindFirstNode<Common.UPickBlurNode>();

            var index = drawcall.FindBinder("SourceTexture");
            if (index.IsValidPointer)
            {
                var attachBuffer = pickBlurNode.GetAttachBuffer(pickBlurNode.PickedPinIn);
                drawcall.BindSRV(index, attachBuffer.Srv);
            }

            index = drawcall.FindBinder("Samp_SourceTexture");
            if (index.IsValidPointer)
                drawcall.BindSampler(index, UEngine.Instance.GfxDevice.SamplerStateManager.DefaultState);
        }
    }    
    public class UPickBlurNode : USceenSpaceNode
    {
        public Common.URenderGraphPin PickedPinIn = Common.URenderGraphPin.CreateInput("Picked");
        public UPickBlurNode()
        {
            Name = "PickBlurNode";            
        }
        public override void InitNodePins()
        {
            AddInput(PickedPinIn, NxRHI.EBufferType.BFT_SRV);
            
            ResultPinOut.IsAutoResize = false;
            ResultPinOut.Attachement.Format = EPixelFormat.PXF_R16G16_FLOAT;
            base.InitNodePins();
        }
        public override async System.Threading.Tasks.Task Initialize(URenderPolicy policy, string debugName)
        {
            await base.Initialize(policy, debugName);
            ScreenDrawPolicy.mBasePassShading = UEngine.Instance.ShadingEnvManager.GetShadingEnv<UPickBlurShading>();
        }
        public override void OnLinkIn(URenderGraphLinker linker)
        {
            //ResultPinOut.Attachement.Format = PickedPinIn.Attachement.Format;
        }
        public override void FrameBuild()
        {
            
        }
        public override void OnResize(URenderPolicy policy, float x, float y)
        {
            float scaleFactor = 1.0f;
            var hitProxyNode = policy.FindFirstNode<UHitproxyNode>();
            if (hitProxyNode != null)
            {
                scaleFactor = hitProxyNode.ScaleFactor;
            }

            ResultPinOut.Attachement.Width = (uint)(x * scaleFactor);
            ResultPinOut.Attachement.Height = (uint)(y * scaleFactor);

            base.OnResize(policy, x * scaleFactor, y * scaleFactor);
        }
    }
}
