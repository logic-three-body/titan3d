﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.IO
{
    public class UAssetOperator
    {
        public struct UAssetModifier
        {
            public RName Source;
            public string SourcePath;
            public RName.ERNameType SourceType;
            public string TargetPath;
            public RName.ERNameType TargetType;
        }
        public List<UAssetModifier> Modifiers { get; set; } = new List<UAssetModifier>();
        public Dictionary<RName, IAsset> mDirtyAssets = new Dictionary<RName, IAsset>();
        public bool AddModifier(RName source, string targetPath, RName.ERNameType targetType)
        {
            foreach(var i in Modifiers)
            {
                if (i.Source == source)
                    return false;
            }

            if (source.Name == targetPath && source.RNameType == targetType)
                return false;

            var address = RName.GetAddress(targetType, targetPath) + IAssetMeta.MetaExt;
            if (IO.FileManager.FileExists(address))
            {//不支持覆盖
                return false;
            }

            UAssetModifier mdf = new UAssetModifier();
            mdf.Source = source;
            mdf.SourcePath = source.Name;
            mdf.SourceType = source.RNameType;
            mdf.TargetPath = targetPath;
            mdf.TargetType = targetType;

            Modifiers.Add(mdf);
            return true;
        }
        public async System.Threading.Tasks.Task Execute()
        {
            mDirtyAssets.Clear();
            foreach (var i in Modifiers)
            {
                var ameta = UEngine.Instance.AssetMetaManager.GetAssetMeta(i.Source);
                if (i.Source != ameta.GetAssetName())
                {
                    System.Diagnostics.Debug.Assert(false);
                    continue;
                }
                await UEngine.Instance.AssetMetaManager.GetAssetHolder(ameta, mDirtyAssets);
            }
            foreach (var i in Modifiers)
            {
                var ameta = UEngine.Instance.AssetMetaManager.GetAssetMeta(i.Source);
                IAsset asset = await ameta.LoadAsset();

                asset.SaveAssetTo(new RName(i.TargetPath, i.TargetType));

                var src = i.Source.Address;
                UEngine.Instance.AssetMetaManager.RemoveAMeta(ameta);
                var nameSets = RName.RNameManager.Instance.mNameSets[(int)i.Source.RNameType];
                nameSets.Remove(i.Source.Name);
                i.Source.Name = i.TargetPath;
                i.Source.RNameType = i.TargetType;
                var tarNameSets = RName.RNameManager.Instance.mNameSets[(int)i.Source.RNameType];
                tarNameSets.Add(i.Source.Name, i.Source);
                var tar = i.Source.Address;

                UEngine.Instance.AssetMetaManager.RegAsset(ameta);

                if (IO.FileManager.FileExists(src + ".snap"))
                    IO.FileManager.CopyFile(src + ".snap", tar + ".snap");

                ameta.SaveAMeta();
            }
            foreach (var i in mDirtyAssets)
            {
                i.Value.SaveAssetTo(i.Key);
            }
            foreach (var i in Modifiers)
            {
                var ameta = UEngine.Instance.AssetMetaManager.GetAssetMeta(i.Source);
                ameta.DeleteAsset(i.SourcePath, i.SourceType);
            }
            Modifiers.Clear();
            mDirtyAssets.Clear();
        }
    }
}

namespace EngineNS.UTest
{
    [UTest.UTest]
    public class UTest_UAssetOperator
    {
        public void UnitTestEntrance()
        {
            Action action = async () =>
            {
                var assetOp = new IO.UAssetOperator();
                var rn = RName.GetRName(@"axis\axis_focus_matins.uminst", RName.ERNameType.Game);
                var ameta = UEngine.Instance.AssetMetaManager.GetAssetMeta(rn);
                if (ameta != null)
                {
                    assetOp.AddModifier(rn, @"material\axis\axis_focus_matins.uminst", RName.ERNameType.Engine);
                    await assetOp.Execute();

                    assetOp.AddModifier(rn, @"axis\axis_focus_matins.uminst", RName.ERNameType.Game);
                    await assetOp.Execute();
                }
            };
            //action();
        }
    }
}
