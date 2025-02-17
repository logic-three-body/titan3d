﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Graphics.Mesh
{
    [Rtti.Meta]
    public class UMeshPrimitivesAMeta : IO.IAssetMeta
    {
        public override string GetAssetExtType()
        {
            return UMeshPrimitives.AssetExt;
        }
        public override string GetAssetTypeName()
        {
            return "VMS";
        }
        public override async System.Threading.Tasks.Task<IO.IAsset> LoadAsset()
        {
            return await UEngine.Instance.GfxDevice.MeshPrimitiveManager.GetMeshPrimitive(GetAssetName());
        }
        public override bool CanRefAssetType(IO.IAssetMeta ameta)
        {
            return false;
        }
        //public override void OnDrawSnapshot(in ImDrawList cmdlist, ref Vector2 start, ref Vector2 end)
        //{
        //    base.OnDrawSnapshot(in cmdlist, ref start, ref end);
        //    cmdlist.AddText(in start, 0xFFFFFFFF, "vms", null);
        //}
        protected override Color GetBorderColor()
        {
            return Color.LightYellow;
        }
    }

    [Rtti.Meta]
    [UMeshPrimitives.Import]
    [IO.AssetCreateMenu(MenuName = "Mesh")]
    public partial class UMeshPrimitives : AuxPtrType<NxRHI.FMeshPrimitives>, IO.IAsset
    {
        public const string AssetExt = ".vms";

        public partial class ImportAttribute : IO.CommonCreateAttribute
        {
            ~ImportAttribute()
            {
                //mFileDialog.Dispose();
            }
            string mSourceFile;
            ImGui.ImGuiFileDialog mFileDialog = UEngine.Instance.EditorInstance.FileDialog.mFileDialog;
            //EGui.Controls.PropertyGrid.PropertyGrid PGAsset = new EGui.Controls.PropertyGrid.PropertyGrid();
            public override void DoCreate(RName dir, Rtti.UTypeDesc type, string ext)
            {
                mDir = dir;
                var noused = PGAsset.Initialize();
                //mDesc.Desc.SetDefault();
                //PGAsset.SingleTarget = mDesc;
            }
            public override unsafe bool OnDraw(EGui.Controls.UContentBrowser ContentBrowser)
            {
                //we also can import from other types
                return FBXCreateCreateDraw(ContentBrowser);
            }

            public unsafe partial bool FBXCreateCreateDraw(EGui.Controls.UContentBrowser ContentBrowser);
        }
        public UMeshPrimitives()
        {
            mCoreObject = NxRHI.FMeshPrimitives.CreateInstance();
        }
        public UMeshPrimitives(NxRHI.FMeshPrimitives iMeshPrimitives)
        {
            mCoreObject = iMeshPrimitives;
            System.Diagnostics.Debug.Assert(mCoreObject.IsValidPointer);
        }
        public void PushAtom(uint index, in EngineNS.NxRHI.FMeshAtomDesc desc)
        {
            mCoreObject.PushAtom(index, in desc);
        }
        #region IAsset
        public override void Dispose()
        {
            base.Dispose();
        }
        public IO.IAssetMeta CreateAMeta()
        {
            var result = new UMeshPrimitivesAMeta();
            return result;
        }
        public IO.IAssetMeta GetAMeta()
        {
            return UEngine.Instance.AssetMetaManager.GetAssetMeta(AssetName);
        }
        public void UpdateAMetaReferences(IO.IAssetMeta ameta)
        {
            ameta.RefAssetRNames.Clear();
        }
        public void SaveAssetTo(RName name)
        {
            var ameta = this.GetAMeta();
            if (ameta != null)
            {
                UpdateAMetaReferences(ameta);
                ameta.SaveAMeta();
            }
            //这里需要存盘的情况很少，正常来说vms是fbx导入的时候生成的，不是保存出来的
            var rc = UEngine.Instance?.GfxDevice.RenderContext;
            var xnd = new IO.CXndHolder("UMeshPrimitives", 0, 0);
            unsafe
            {
                mCoreObject.Save2Xnd(rc.mCoreObject, xnd.RootNode.mCoreObject);
            }
            var attr = xnd.RootNode.mCoreObject.GetOrAddAttribute("PartialSkeleton",0,0);
            var ar = attr.GetWriter(512);
            ar.Write(PartialSkeleton);
            attr.ReleaseWriter(ref ar);
            xnd.SaveXnd(name.Address);
        }
        [Rtti.Meta]
        public RName AssetName
        {
            get;
            set;
        }
        #endregion
        [Rtti.Meta]
        public Animation.SkeletonAnimation.Skeleton.USkinSkeleton PartialSkeleton
        {
            get;
            set;
        }
        public static UMeshPrimitives LoadXnd(UMeshPrimitiveManager manager, IO.CXndHolder xnd)
        {
            var result = new UMeshPrimitives();
            unsafe
            {
                var ret = result.mCoreObject.LoadXnd(UEngine.Instance.GfxDevice.RenderContext.mCoreObject, "", xnd.mCoreObject, true);
                if (ret == false)
                    return null;
                var attr = xnd.RootNode.mCoreObject.TryGetAttribute("PartialSkeleton");
                if (attr.IsValidPointer)
                {
                    var ar = attr.GetReader(manager);
                    IO.ISerializer partialSkeleton = null;
                    ar.Read(out partialSkeleton, manager);
                    attr.ReleaseReader(ref ar);
                    if(partialSkeleton is Animation.SkeletonAnimation.Skeleton.USkinSkeleton)
                    {
                        result.PartialSkeleton = partialSkeleton as Animation.SkeletonAnimation.Skeleton.USkinSkeleton;
                    }
                }
                return result;
            }
        }

        private UMeshDataProvider mMeshDataProvider;
        public async System.Threading.Tasks.Task LoadMeshDataProvider()
        {
            if (mMeshDataProvider != null || AssetName == null)
                return;

            if (mMeshDataProvider == null)
            {
                var result = await UEngine.Instance.EventPoster.Post(() =>
                {
                    using (var xnd = IO.CXndHolder.LoadXnd(AssetName.Address))
                    {
                        if (xnd != null)
                        {
                            var tmp = new UMeshDataProvider();

                            var ok = tmp.mCoreObject.LoadFromMeshPrimitive(xnd.RootNode.mCoreObject, NxRHI.EVertexStreamType.VST_FullMask);
                            if (ok == false)
                                return false;

                            mMeshDataProvider = tmp;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }, Thread.Async.EAsyncTarget.AsyncIO);
            }
        }
        public UMeshDataProvider MeshDataProvider
        {
            get
            {
                return mMeshDataProvider;
            }
        }
    }
    public class UMeshPrimitiveManager
    {
        ~UMeshPrimitiveManager()
        {
            mUnitSphere?.Dispose();
            mUnitSphere = null;
        }
        UMeshPrimitives mUnitSphere;
        public UMeshPrimitives UnitSphere
        {
            get
            {
                if (mUnitSphere == null)
                {
                    mUnitSphere = Graphics.Mesh.UMeshDataProvider.MakeSphere(1.0f, 15, 15, 0xfffffff).ToMesh();
                }
                return mUnitSphere;
            }
        }
        UMeshPrimitives mUnitBox;
        public UMeshPrimitives UnitBox
        {
            get
            {
                if (mUnitBox == null)
                {
                    mUnitBox = Graphics.Mesh.UMeshDataProvider.MakeBox(0.5f, 0.5f, 0.5f, 1.0f, 1.0f, 1.0f, 0xfffffff).ToMesh();
                }
                return mUnitBox;
            }
        }
        public Dictionary<RName, UMeshPrimitives> Meshes { get; } = new Dictionary<RName, UMeshPrimitives>();
        public async System.Threading.Tasks.Task Initialize()
        {
            await GetMeshPrimitive(RName.GetRName("axis/movex.vms", RName.ERNameType.Engine));
        }
        public UMeshPrimitives FindMeshPrimitive(RName name)
        {
            UMeshPrimitives result;
            if (Meshes.TryGetValue(name, out result))
                return result;
            return null;
        }
        public async System.Threading.Tasks.Task<UMeshPrimitives> GetMeshPrimitive(RName name)
        {
            UMeshPrimitives result;
            if (Meshes.TryGetValue(name, out result))
                return result;

            result = await UEngine.Instance.EventPoster.Post(() =>
            {
                using (var xnd = IO.CXndHolder.LoadXnd(name.Address))
                {
                    if (xnd != null)
                    {
                        var mesh = UMeshPrimitives.LoadXnd(this, xnd);
                        if (mesh == null)
                            return null;

                        mesh.AssetName = name;
                        return mesh;
                    }
                    else
                    {
                        return null;
                    }
                }
            }, Thread.Async.EAsyncTarget.AsyncIO);

            if (result != null)
            {
                Meshes[name] = result;
                return result;
            }

            return null;
        }
        public void UnsafeRenameForCook(RName name, RName newName)
        {
            UMeshPrimitives result;
            if (Meshes.TryGetValue(name, out result) == false)
                return;

            Meshes.Remove(name);
            result.GetAMeta().SetAssetName(newName);
            result.AssetName = newName;
            Meshes.Add(newName, result);
        }
    }
}

namespace EngineNS.Graphics.Pipeline
{
    public partial class UGfxDevice
    {
        public Mesh.UMeshPrimitiveManager MeshPrimitiveManager { get; } = new Mesh.UMeshPrimitiveManager();
    }
}
