﻿using EngineNS.NxRHI;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Editor.Forms
{
    public class UTextureViewer : Editor.IAssetEditor, IRootForm
    {
        public RName AssetName { get; set; }
        protected bool mVisible = true;
        public bool Visible { get => mVisible; set => mVisible = value; }
        public uint DockId { get; set; }
        public ImGuiCond_ DockCond { get; set; } = ImGuiCond_.ImGuiCond_FirstUseEver;

        public NxRHI.USrView TextureSRV;
        public EGui.Controls.PropertyGrid.PropertyGrid TexturePropGrid = new EGui.Controls.PropertyGrid.PropertyGrid();
        ~UTextureViewer()
        {
            Cleanup();
        }
        public void Cleanup()
        {
            TexturePropGrid.Target = null;
            if (TextureSRV != null)
            {
                TextureSRV.FreeTextureHandle();
                TextureSRV = null;
            }
        }
        public async System.Threading.Tasks.Task<bool> Initialize()
        {
            return await TexturePropGrid.Initialize();
        }
        public IRootForm GetRootForm()
        {
            return this;
        }
        public async System.Threading.Tasks.Task<bool> OpenEditor(UMainEditorApplication mainEditor, RName name, object arg)
        {
            AssetName = name;
            TextureSRV = await UEngine.Instance.GfxDevice.TextureManager.GetOrNewTexture(name);
            if (TextureSRV == null)
                return false;

            TexturePropGrid.Target = TextureSRV;
            ImageSize.X = TextureSRV.PicDesc.Width;
            ImageSize.Y = TextureSRV.PicDesc.Height;
            return true;
        }
        public void OnCloseEditor()
        {
            Cleanup();
        }
        public float LeftWidth = 0;
        public Vector2 WindowSize = new Vector2(800, 600);
        public Vector2 ImageSize = new Vector2(512, 512);
        public float ScaleFactor = 1.0f;
        public unsafe void OnDraw()
        {
            if (Visible == false || TextureSRV == null)
                return;

            var pivot = new Vector2(0);
            ImGuiAPI.SetNextWindowSize(in WindowSize, ImGuiCond_.ImGuiCond_FirstUseEver);
            ImGuiAPI.SetNextWindowDockID(DockId, DockCond);
            if (ImGuiAPI.Begin(TextureSRV.AssetName.Name, ref mVisible, ImGuiWindowFlags_.ImGuiWindowFlags_None |
                ImGuiWindowFlags_.ImGuiWindowFlags_NoSavedSettings))
            {
                if (ImGuiAPI.IsWindowDocked())
                {
                    DockId = ImGuiAPI.GetWindowDockID();
                }
                DrawToolBar();
                ImGuiAPI.Separator();
                ImGuiAPI.Columns(2, null, true);
                if (LeftWidth == 0)
                {
                    ImGuiAPI.SetColumnWidth(0, 300);
                }
                LeftWidth = ImGuiAPI.GetColumnWidth(0);

                var min = ImGuiAPI.GetWindowContentRegionMin();
                var max = ImGuiAPI.GetWindowContentRegionMin();

                DrawLeft(ref min, ref max);
                ImGuiAPI.NextColumn();

                DrawRight(ref min, ref max);
                ImGuiAPI.NextColumn();

                ImGuiAPI.Columns(1, null, true);
            }
            ImGuiAPI.End();
        }
        protected void DrawToolBar()
        {
            var btSize = Vector2.Zero;
            if (EGui.UIProxy.CustomButton.ToolButton("Save", in btSize))
            {
                StbImageSharp.ImageResult image = NxRHI.USrView.LoadOriginImage(AssetName);
                using (var xnd = new IO.CXndHolder("USrView", 0, 0))
                {
                    NxRHI.USrView.SaveTexture(AssetName, xnd.RootNode.mCoreObject, image, this.TextureSRV.PicDesc);
                    xnd.SaveXnd(AssetName.Address);
                }
            }
            ImGuiAPI.SameLine(0, -1);
            if (EGui.UIProxy.CustomButton.ToolButton("SavePng", in btSize))
            {
                USrView.SaveOriginPng(AssetName);
            }
        }
        protected unsafe void DrawLeft(ref Vector2 min, ref Vector2 max)
        {
            var sz = new Vector2(-1);
            if (ImGuiAPI.BeginChild("LeftView", in sz, true, ImGuiWindowFlags_.ImGuiWindowFlags_None))
            {
                TexturePropGrid.OnDraw(true, false, false);
            }
            ImGuiAPI.EndChild();
        }
        protected unsafe void DrawRight(ref Vector2 min, ref Vector2 max)
        {
            if (ImGuiAPI.BeginChild("TextureView", in Vector2.MinusOne, true, ImGuiWindowFlags_.ImGuiWindowFlags_None))
            {
                if (ImGuiAPI.IsWindowFocused(ImGuiFocusedFlags_.ImGuiFocusedFlags_ChildWindows))
                {
                    if ( ImGuiAPI.GetIO().MouseWheel != 0)
                    {
                        ScaleFactor += ImGuiAPI.GetIO().MouseWheel * 0.1f;

                        if (ScaleFactor <= 0.1f)
                            ScaleFactor = 0.1f;
                        if (ScaleFactor >= 3.0f)
                            ScaleFactor = 3.0f;
                    }
                }
                var pos = ImGuiAPI.GetWindowPos();
                var drawlist = new ImDrawList(ImGuiAPI.GetWindowDrawList());
                var uv1 = new Vector2(0, 0);
                var uv2 = new Vector2(1, 1);
                var min1 = ImGuiAPI.GetWindowContentRegionMin();
                var max1 = min1 + ImageSize * ScaleFactor;

                min1 = min1 + pos;
                max1 = max1 + pos;
                drawlist.AddImage(TextureSRV.GetTextureHandle().ToPointer(), in min1, in max1, in uv1, in uv2, 0xFFFFFFFF);
                drawlist.AddRect(in min1, in max1, 0xFF00FF00, 0, ImDrawFlags_.ImDrawFlags_None, 0);
            }
            ImGuiAPI.EndChild();
        }
        public void OnEvent(in Bricks.Input.Event e)
        {
            
        }
    }
}

namespace EngineNS.NxRHI
{
    [Editor.UAssetEditor(EditorType = typeof(Editor.Forms.UTextureViewer))]
    public partial class USrView
    {
    }
}