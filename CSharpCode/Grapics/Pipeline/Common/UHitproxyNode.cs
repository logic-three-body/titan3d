﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Graphics.Pipeline.Common
{
    public class UHitproxyShading : Shader.UShadingEnv
    {
        public UHitproxyShading()
        {
            CodeName = RName.GetRName("shaders/ShadingEnv/Sys/pick/HitProxy.cginc", RName.ERNameType.Engine);
        }
        public override NxRHI.EVertexStreamType[] GetNeedStreams()
        {
            return new NxRHI.EVertexStreamType[] { NxRHI.EVertexStreamType.VST_Position};
        }
    }
    public class UHitproxyNode : URenderGraphNode
    {
        public Common.URenderGraphPin HitIdPinOut = Common.URenderGraphPin.CreateOutput("HitId", false, EPixelFormat.PXF_R8G8B8A8_UNORM);
        public Common.URenderGraphPin DepthPinOut = Common.URenderGraphPin.CreateOutput("Depth", false, EPixelFormat.PXF_D16_UNORM);
        public UHitproxyNode()
        {
            Name = "Hitproxy";
        }
        public override void InitNodePins()
        {
            AddOutput(HitIdPinOut, NxRHI.EBufferType.BFT_RTV | NxRHI.EBufferType.BFT_SRV);
            AddOutput(DepthPinOut, NxRHI.EBufferType.BFT_DSV | NxRHI.EBufferType.BFT_SRV);
        }
        #region GetHitproxy
        private Support.CBlobObject mHitProxyData = new Support.CBlobObject();
        unsafe private NxRHI.UGpuResource mReadableHitproxyTexture;
        private NxRHI.UFence mCopyFence;
        private Int32 Clamp(Int32 ValueIn, Int32 MinValue, Int32 MaxValue)
        {
            return ValueIn < MinValue ? MinValue : ValueIn < MaxValue ? ValueIn : MaxValue;
        }
        public IProxiable GetHitproxy(UInt32 MouseX, UInt32 MouseY)
        {
            var id = GetHitProxyID(MouseX, MouseY);
            if (id == 0)
                return null;
            return UEngine.Instance.GfxDevice.HitproxyManager.FindProxy(id);
        }

        public UInt32 GetHitProxyID(UInt32 MouseX, UInt32 MouseY)
        {
            unsafe
            {
                lock (this)
                {
                    return GetHitProxyIDImpl(MouseX, MouseY);
                }
            }
        }
        private const UInt32 mHitCheckRegion = 2;
        private const UInt32 mCheckRegionSamplePointCount = 25;
        private const UInt32 mCheckRegionCenter = 13;
        private UInt32[] mHitProxyIdArray = new UInt32[mCheckRegionSamplePointCount];
        private unsafe UInt32 GetHitProxyIDImpl(UInt32 MouseX, UInt32 MouseY)
        {
            MouseX = (UInt32)((float)MouseX * ScaleFactor);
            MouseY = (UInt32)((float)MouseY * ScaleFactor);

            if (mReadableHitproxyTexture == null)
                return 0;

            byte* pPixelData = (byte*)mHitProxyData.mCoreObject.GetData();
            if (pPixelData == (byte*)0)
                return 0;
            var pBitmapDesc = (NxRHI.FPixelDesc*)pPixelData;
            pPixelData += sizeof(NxRHI.FPixelDesc);

            IntColor* HitProxyIdArray = (IntColor*)pPixelData;

            Int32 RegionMinX = (Int32)(MouseX - mHitCheckRegion);
            Int32 RegionMinY = (Int32)(MouseY - mHitCheckRegion);
            Int32 RegionMaxX = (Int32)(MouseX + mHitCheckRegion);
            Int32 RegionMaxY = (Int32)(MouseY + mHitCheckRegion);

            RegionMinX = Clamp(RegionMinX, 0, (Int32)pBitmapDesc->Width - 1);
            RegionMinY = Clamp(RegionMinY, 0, (Int32)pBitmapDesc->Height - 1);
            RegionMaxX = Clamp(RegionMaxX, 0, (Int32)pBitmapDesc->Width - 1);
            RegionMaxY = Clamp(RegionMaxY, 0, (Int32)pBitmapDesc->Height - 1);

            Int32 RegionSizeX = RegionMaxX - RegionMinX + 1;
            Int32 RegionSizeY = RegionMaxY - RegionMinY + 1;

            if (RegionSizeX > 0 && RegionSizeY > 0)
            {
                if (HitProxyIdArray == null)
                {
                    Profiler.Log.WriteLine(Profiler.ELogTag.Error, "@Graphic", $"HitProxyError: Null Ptr!!!", "");
                    return 0;
                }

                int max = pBitmapDesc->Width * pBitmapDesc->Height;

                UInt32 HitProxyArrayIdx = 0;
                for (Int32 PointY = RegionMinY; PointY < RegionMaxY; PointY++)
                {
                    IntColor* TempHitProxyIdCache = &HitProxyIdArray[PointY * pBitmapDesc->Width];
                    for (Int32 PointX = RegionMinX; PointX < RegionMaxX; PointX++)
                    {
                        mHitProxyIdArray[HitProxyArrayIdx] = UHitProxy.ConvertCpuTexColorToHitProxyId(TempHitProxyIdCache[PointX]);
                        HitProxyArrayIdx++;
                    }
                }
                if (mHitProxyIdArray[mCheckRegionCenter] != 0)
                {
                    return mHitProxyIdArray[mCheckRegionCenter];
                }
                else
                {
                    for (UInt32 idx = 0; idx < mCheckRegionSamplePointCount; idx++)
                    {
                        if (mHitProxyIdArray[idx] != 0)
                        {
                            return mHitProxyIdArray[idx];
                        }
                    }
                }
            }
            return 0;
        }
        #endregion
        public UGraphicsBuffers GHitproxyBuffers { get; protected set; } = new UGraphicsBuffers();
        public UGraphicsBuffers GGizmosBuffers { get; protected set; } = new UGraphicsBuffers();
        public Common.UHitproxyShading mHitproxyShading;
        public UPassDrawBuffers HitproxyPass = new UPassDrawBuffers();
        public NxRHI.URenderPass HitproxyRenderPass;
        public NxRHI.URenderPass GizmosRenderPass;
        [Rtti.Meta]
        public float ScaleFactor { get; set; } = 0.5f;
        public override async System.Threading.Tasks.Task Initialize(URenderPolicy policy, string debugName)
        {
            await Thread.AsyncDummyClass.DummyFunc();

            var rc = UEngine.Instance.GfxDevice.RenderContext;
            HitproxyPass.Initialize(rc, debugName);

            var HitproxyPassDesc = new NxRHI.FRenderPassDesc();
            unsafe
            {
                HitproxyPassDesc.NumOfMRT = 1;
                HitproxyPassDesc.AttachmentMRTs[0].Format = HitIdPinOut.Attachement.Format;
                HitproxyPassDesc.AttachmentMRTs[0].Samples = 1;
                HitproxyPassDesc.AttachmentMRTs[0].LoadAction = NxRHI.EFrameBufferLoadAction.LoadActionClear;
                HitproxyPassDesc.AttachmentMRTs[0].StoreAction = NxRHI.EFrameBufferStoreAction.StoreActionStore;
                HitproxyPassDesc.m_AttachmentDepthStencil.Format = DepthPinOut.Attachement.Format;
                HitproxyPassDesc.m_AttachmentDepthStencil.Samples = 1;
                HitproxyPassDesc.m_AttachmentDepthStencil.LoadAction = NxRHI.EFrameBufferLoadAction.LoadActionClear;
                HitproxyPassDesc.m_AttachmentDepthStencil.StoreAction = NxRHI.EFrameBufferStoreAction.StoreActionStore;
                HitproxyPassDesc.m_AttachmentDepthStencil.StencilLoadAction = NxRHI.EFrameBufferLoadAction.LoadActionClear;
                HitproxyPassDesc.m_AttachmentDepthStencil.StencilStoreAction = NxRHI.EFrameBufferStoreAction.StoreActionStore;
                //HitproxyPassDesc.mFBClearColorRT0 = new Color4(0, 0, 0, 0);
                //HitproxyPassDesc.mDepthClearValue = 1.0f;
                //HitproxyPassDesc.mStencilClearValue = 0u;
            }            
            HitproxyRenderPass = UEngine.Instance.GfxDevice.RenderPassManager.GetPipelineState<NxRHI.FRenderPassDesc>(rc, in HitproxyPassDesc);

            var GizmosPassDesc = new NxRHI.FRenderPassDesc();
            unsafe
            {
                GizmosPassDesc.NumOfMRT = 1;
                GizmosPassDesc.AttachmentMRTs[0].Format = HitIdPinOut.Attachement.Format;
                GizmosPassDesc.AttachmentMRTs[0].Samples = 1;
                GizmosPassDesc.AttachmentMRTs[0].LoadAction = NxRHI.EFrameBufferLoadAction.LoadActionDontCare;
                GizmosPassDesc.AttachmentMRTs[0].StoreAction = NxRHI.EFrameBufferStoreAction.StoreActionStore;
                GizmosPassDesc.m_AttachmentDepthStencil.Format = DepthPinOut.Attachement.Format;
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

            mHitproxyShading = UEngine.Instance.ShadingEnvManager.GetShadingEnv<UHitproxyShading>();

            GHitproxyBuffers.Initialize(policy, HitproxyRenderPass);
            GHitproxyBuffers.SetRenderTarget(policy, 0, HitIdPinOut);
            GHitproxyBuffers.SetDepthStencil(policy, DepthPinOut);
            GHitproxyBuffers.TargetViewIdentifier = policy.DefaultCamera.TargetViewIdentifier;

            GGizmosBuffers.Initialize(policy, GizmosRenderPass);
            GGizmosBuffers.SetRenderTarget(policy, 0, HitIdPinOut);
            GGizmosBuffers.SetDepthStencil(policy, DepthPinOut);
            GGizmosBuffers.TargetViewIdentifier = policy.DefaultCamera.TargetViewIdentifier;

            mCopyFence = rc.CreateFence(new NxRHI.FFenceDesc(), "Copy Hitproxy Texture");
        }
        public unsafe override void Cleanup()
        {
            mReadableHitproxyTexture?.Dispose();
            mReadableHitproxyTexture = null;
            
            GHitproxyBuffers?.Cleanup();
            GHitproxyBuffers = null;

            GGizmosBuffers?.Cleanup();
            GGizmosBuffers = null;

            base.Cleanup();
        }
        public override unsafe void OnResize(URenderPolicy policy, float x, float y)
        {
            HitIdPinOut.Attachement.Width = (uint)(x * ScaleFactor);
            HitIdPinOut.Attachement.Height = (uint)(y * ScaleFactor);
            DepthPinOut.Attachement.Width = (uint)(x * ScaleFactor);
            DepthPinOut.Attachement.Height = (uint)(y * ScaleFactor);
            if (GHitproxyBuffers != null)
            {
                GHitproxyBuffers.OnResize(x * ScaleFactor, y * ScaleFactor);
            }
            if (GGizmosBuffers != null)
            {
                GGizmosBuffers.OnResize(x * ScaleFactor, y * ScaleFactor);
            }

            CopyTexDesc.SetDefault();
            CopyTexDesc.Usage = NxRHI.EGpuUsage.USAGE_STAGING;
            CopyTexDesc.CpuAccess = NxRHI.ECpuAccess.CAS_READ;
            CopyTexDesc.Width = HitIdPinOut.Attachement.Width;
            CopyTexDesc.Height = HitIdPinOut.Attachement.Height;
            CopyTexDesc.Depth = 0;
            CopyTexDesc.Format = HitIdPinOut.Attachement.Format;
            CopyTexDesc.ArraySize = 1;
            CopyTexDesc.BindFlags = 0;
            CopyTexDesc.MipLevels = 1;

            //CopyBufferFootPrint.Width = CopyTexDesc.Width;
            //CopyBufferFootPrint.Height = CopyTexDesc.Height;
            //CopyBufferFootPrint.Depth = 1;
            //CopyBufferFootPrint.Format = HitIdPinOut.Attachement.Format;
            //CopyBufferFootPrint.RowPitch = (uint)CoreSDK.GetPixelFormatByteWidth(CopyBufferFootPrint.Format) * HitIdPinOut.Attachement.Width;
            //var pAlignment = UEngine.Instance.GfxDevice.RenderContext.mCoreObject.GetGpuResourceAlignment();
            //if (CopyBufferFootPrint.RowPitch % pAlignment->TexturePitchAlignment > 0)
            //{
            //    CopyBufferFootPrint.RowPitch = (CopyBufferFootPrint.RowPitch / pAlignment->TexturePitchAlignment + 1) * pAlignment->TexturePitchAlignment;
            //}
            //mReadableHitproxyTexture = UEngine.Instance.GfxDevice.RenderContext.CreateTextureToCpuBuffer(in CopyTexDesc, in CopyBufferFootPrint);
            //mReadableHitproxyTexture.SetDebugName("Readback Hitproxy Buffer");

            mReadableHitproxyTexture?.Dispose();
            mReadableHitproxyTexture = null;
        }
        NxRHI.FTextureDesc CopyTexDesc = new NxRHI.FTextureDesc();
        NxRHI.FSubResourceFootPrint CopyBufferFootPrint = new NxRHI.FSubResourceFootPrint();
        bool IsHitproxyBuilding = false;
        [ThreadStatic]
        private static Profiler.TimeScope ScopeTick = Profiler.TimeScopeManager.GetTimeScope(typeof(UHitproxyNode), nameof(TickLogic));
        public override unsafe void TickLogic(GamePlay.UWorld world, URenderPolicy policy, bool bClear)
        {
            if (IsHitproxyBuilding)
                return;

            IsHitproxyBuilding = true;

            using (new Profiler.TimeScopeHelper(ScopeTick))
            {
                HitproxyPass.ClearMeshDrawPassArray();
                foreach (var i in policy.VisibleMeshes)
                {
                    if (i.Atoms == null)
                        continue;

                    if (i.IsDrawHitproxy)
                    {
                        for (int j = 0; j < i.Atoms.Length; j++)
                        {
                            var hpDrawcall = i.GetDrawCall(GHitproxyBuffers, j, policy, URenderPolicy.EShadingType.HitproxyPass, this);
                            if (hpDrawcall != null)
                            {
                                hpDrawcall.BindGBuffer(policy.DefaultCamera, GHitproxyBuffers);

                                var layer = i.Atoms[j].Material.RenderLayer;
                                HitproxyPass.PushDrawCall(layer, hpDrawcall);
                            }
                        }
                    }
                }

                {
                    //draw mesh first
                    var passClears = new NxRHI.FRenderPassClears();
                    passClears.SetDefault();
                    passClears.SetClearColor(0, new Color4(0, 0, 0, 0));
                    GHitproxyBuffers.BuildFrameBuffers(policy);
                    GGizmosBuffers.BuildFrameBuffers(policy);
                    HitproxyPass.BuildRenderPass(policy, in GHitproxyBuffers.Viewport, in passClears, GHitproxyBuffers, GGizmosBuffers, "Hitproxy:");
                }
            }   

            var rc = UEngine.Instance.GfxDevice.RenderContext;
            //copy to sys memory after draw all meshesr
            var cmdlist_post = HitproxyPass.PostCmds.DrawCmdList.mCoreObject;
            var attachBuffer = RenderGraph.AttachmentCache.FindAttachement(GHitproxyBuffers.RenderTargets[0].Attachement.AttachmentName);
            attachBuffer.Srv.mCoreObject.GetBufferAsTexture().SetDebugName("Hitproxy Source Texture");

            if (mReadableHitproxyTexture == null)
            {
                var rtTex = attachBuffer.Buffer as NxRHI.UTexture;
                mReadableHitproxyTexture = rtTex.CreateBufferData(0, NxRHI.ECpuAccess.CAS_READ, ref CopyBufferFootPrint);
            }
            var readTexture = mReadableHitproxyTexture;
            cmdlist_post.BeginCommand();
            fixed(NxRHI.FSubResourceFootPrint* pFootprint = &CopyBufferFootPrint)
            {
                var dstTex = readTexture as NxRHI.UTexture;
                var dstBf = readTexture as NxRHI.UBuffer;
                if (dstTex != null)
                {
                    cmdlist_post.CopyTextureRegion(dstTex.mCoreObject, 0, 0, 0, 0, attachBuffer.Srv.mCoreObject.GetBufferAsTexture(), 0, (NxRHI.FSubresourceBox*)IntPtr.Zero.ToPointer());
                }
                else if (dstBf != null)
                {
                    cmdlist_post.CopyTextureToBuffer(dstBf.mCoreObject, pFootprint, attachBuffer.Srv.mCoreObject.GetBufferAsTexture(), 0);
                }
            }
            cmdlist_post.EndCommand();
            UEngine.Instance.GfxDevice.RenderCmdQueue.QueueCmdlist(HitproxyPass.PostCmds.DrawCmdList);

            var fence = mCopyFence;
            UEngine.Instance.GfxDevice.RenderCmdQueue.QueueCmd((im_cmd, name) =>
            {
                rc.CmdQueue.IncreaseSignal(fence);
                var targetValue = fence.AspectValue;
                var postTime = Support.Time.GetTickCount();
                UEngine.Instance.EventPoster.PostTickSyncEvent((tickCount) =>
                {
                    if (readTexture != mReadableHitproxyTexture || tickCount - postTime > 1000)
                    {
                        IsHitproxyBuilding = false;
                        return true;
                    }
                    if (fence.CompletedValue >= targetValue)
                    {
                        var im_cmd = UEngine.Instance.GfxDevice.RenderContext.CmdQueue.GetIdleCmdlist(NxRHI.EQueueCmdlist.QCL_Read);
                        var gpuDataBlob = new Support.CBlobObject();
                        readTexture.GetGpuBufferDataPointer().FetchGpuData(im_cmd, 0, gpuDataBlob.mCoreObject);
                        //var ptr = (uint*)gpuDataBlob.mCoreObject.GetData();
                        //var num = gpuDataBlob.mCoreObject.GetSize() / 4;
                        //for (int i = 2; i < num; i++)
                        //{
                        //    if (ptr[i] != 0)
                        //    {
                        //        int xxx = 0;
                        //    }
                        //}
                        UEngine.Instance.GfxDevice.RenderContext.CmdQueue.ReleaseIdleCmdlist(im_cmd, NxRHI.EQueueCmdlist.QCL_Read);
                        NxRHI.ITexture.BuildImage2DBlob(mHitProxyData.mCoreObject, gpuDataBlob.mCoreObject, in CopyTexDesc);
                        IsHitproxyBuilding = false;

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });
            }, "Signal Ready");

            

            //UEngine.Instance.GfxDevice.RenderCmdQueue.QueueCmd((im_cmd, name) =>
            //{
            //    var bufferData = new Support.CBlobObject();
            //    mReadableHitproxyTexture.mCoreObject.FetchGpuDataAsImage2DBlob(im_cmd, 0, 0, mHitProxyData.mCoreObject);
            //    UEngine.Instance.GfxDevice.RenderContext.CmdQueue.SignalFence(mReadHitproxyFence, 0);

            //    IsHitproxyBuilding = false;
            //}, "Fetch Image");
        }
        public unsafe override void TickSync(URenderPolicy policy)
        {
            HitproxyPass.SwapBuffer();
        }
    }
}
