﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EngineNS.EGui.Controls
{
    public class UContentBrowser : IRootForm, EGui.IPanel
    {
        bool mVisible = true;
        public bool Visible { get => mVisible; set => mVisible = value; }
        public uint DockId { get; set; } = uint.MaxValue;
        public ImGuiCond_ DockCond { get; set; } = ImGuiCond_.ImGuiCond_FirstUseEver;

        EGui.UIProxy.SearchBarProxy mSearchBar;

        public string Name = "";
        public bool CreateNewAssets = true;
        public string ExtNames { get; set; } = null;
        public Rtti.UTypeDesc MacrossBase = null;
        public string FilterText = "";
        public static IO.IAssetMeta GlobalSelectedAsset = null;
        public List<IO.IAssetMeta> SelectedAssets = new List<IO.IAssetMeta>();
        public Action<IO.IAssetMeta> ItemSelectedAction = null;

        public static string FolderOpenImgName = "uestyle/content/folderopen.srv";
        public static string FolderClosedImgName = "uestyle/content/folderclosed.srv";
        public static string PreFolderImgName = "uestyle/content/circle-arrow-left.srv";
        public static string NextFolderImgName = "uestyle/content/circle-arrow-right.srv";
        public static string FilterImgName = "uestyle/content/filter.srv";

        public void Cleanup()
        {
            GlobalSelectedAsset = null;
            SelectedAssets.Clear();
            mSearchBar?.Cleanup();
            mSearchBar = null;
        }

        public async Task<bool> Initialize()
        {
            await Thread.AsyncDummyClass.DummyFunc();

            if(mNewAssetMenuItem == null)
            {
                mNewAssetMenuItem = new UIProxy.MenuItemProxy()
                {
                    MenuName = "New Asset",
                };
                await mNewAssetMenuItem.Initialize();

                OnTypeChanged();
            }

            mSearchBar = new UIProxy.SearchBarProxy();
            await mSearchBar.Initialize();
            mSearchBar.InfoText = "Search Assets";
            //Rtti.UTypeDescManager.Instance.OnTypeChanged += OnTypeChanged;

            InitializeDirContextMenu();
            InitializeFilterMenu();

            if (UEngine.Instance.UIManager[FolderOpenImgName] == null)
                UEngine.Instance.UIManager[FolderOpenImgName] = new EGui.UIProxy.ImageProxy(RName.GetRName(FolderOpenImgName, RName.ERNameType.Engine));
            if (UEngine.Instance.UIManager[FolderClosedImgName] == null)
                UEngine.Instance.UIManager[FolderClosedImgName] = new EGui.UIProxy.ImageProxy(RName.GetRName(FolderClosedImgName, RName.ERNameType.Engine));
            if (UEngine.Instance.UIManager[PreFolderImgName] == null)
                UEngine.Instance.UIManager[PreFolderImgName] = new EGui.UIProxy.ImageProxy(RName.GetRName(PreFolderImgName, RName.ERNameType.Engine));
            if (UEngine.Instance.UIManager[NextFolderImgName] == null)
                UEngine.Instance.UIManager[NextFolderImgName] = new EGui.UIProxy.ImageProxy(RName.GetRName(NextFolderImgName, RName.ERNameType.Engine));
            if (UEngine.Instance.UIManager[FilterImgName] == null)
                UEngine.Instance.UIManager[FilterImgName] = new EGui.UIProxy.ImageProxy(RName.GetRName(FilterImgName, RName.ERNameType.Engine));

            return true;
        }
        Task SureBarTask = null;
        internal void SureSearchBar()
        {
            if (SureBarTask!=null && SureBarTask.IsCompleted==false)
            {
                return;
            }
            if (mSearchBar == null)
            {
                if (SureBarTask == null)
                {
                    mSearchBar = new UIProxy.SearchBarProxy();
                    SureBarTask = mSearchBar.Initialize();
                    mSearchBar.InfoText = "Search Assets";
                }
                else if(SureBarTask.IsCompleted)
                {
                    SureBarTask = null;
                }
            }
        }

        EGui.UIProxy.MenuItemProxy mNewAssetMenuItem;
        void OnTypeChanged()
        {
            mNewAssetMenuItem.CleanupSubMenus();

            foreach (var service in Rtti.UTypeDescManager.Instance.Services.Values)
            {
                foreach(var typeDesc in service.Types.Values)
                {
                    var atts = typeDesc.SystemType.GetCustomAttributes(typeof(IO.AssetCreateMenuAttribute), false);
                    if (atts.Length == 0)
                        continue;

                    var assetExtField = Rtti.UTypeDesc.GetField(typeDesc.SystemType, "AssetExt");
                    mNewAssetMenuItem.SubMenus.Add(new EGui.UIProxy.MenuItemProxy()
                    {
                        MenuName = ((IO.AssetCreateMenuAttribute)atts[0]).MenuName,
                        Shortcut = ((IO.AssetCreateMenuAttribute)atts[0]).Shortcut,
                        Action = (proxy, data)=>
                        {
                            EnqueueAssetImporter(UEngine.Instance.AssetMetaManager.ImportAsset(CurrentDir, typeDesc, (string)assetExtField.GetValue(null)), "");
                        }
                    });
                }
            }
        }
        void DrawDirContextMenu(string path)
        {
            if (ImGuiAPI.BeginPopupContextItem(path, ImGuiPopupFlags_.ImGuiPopupFlags_MouseButtonRight))
            {
                if (mDirContextMenu != null)
                {
                    Support.UAnyPointer menuData = new Support.UAnyPointer();
                    menuData.RefObject = path;
                    menuData.Value.SetStruct<stDirMenuData>(new stDirMenuData());
                    var drawList = ImGuiAPI.GetWindowDrawList();
                    for (int i = 0; i < mDirContextMenu.Count; i++)
                    {
                        mDirContextMenu[i].OnDraw(in drawList, in menuData);
                    }
                }
                ImGuiAPI.EndPopup();
            }
        }
        int mCurrentDirHistoryIdx = 0;
        List<RName> mDirHistory = new List<RName>();
        void PushHistory(RName dir)
        {
            if (CurrentDir == dir)
                return;
            CurrentDir = dir;
            if(mCurrentDirHistoryIdx + 1 < mDirHistory.Count)
            {
                mDirHistory.RemoveRange(mCurrentDirHistoryIdx + 1, mDirHistory.Count - mCurrentDirHistoryIdx - 1);
            }
            if (mDirHistory.Count > 20)
                mDirHistory.RemoveAt(0);
            mCurrentDirHistoryIdx = mDirHistory.Count;
            mDirHistory.Add(dir);
            mIsPreFolderDisable = false;
            mIsNextFolderDisable = true;
        }
        public RName CurrentDir;
        public void DrawDirectories(RName root)
        {
            ImGuiTreeNodeFlags_ flags = ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_OpenOnArrow | ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_SpanFullWidth;
            if (root == CurrentDir)
                flags |= ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_Selected;
            UEngine.Instance.GfxDevice.SlateRenderer.PushFont((int)EGui.Slate.UBaseRenderer.enFont.Font_Bold_13px);
            var treeNodeResult = ImGuiAPI.TreeNodeEx(root.RNameType.ToString(), flags);
            UEngine.Instance.GfxDevice.SlateRenderer.PopFont();
            DrawDirContextMenu(root.Address);
            if (treeNodeResult)
            {
                if (ImGuiAPI.IsItemActivated())
                {
                    PushHistory(root);
                }
                var dirs = IO.FileManager.GetDirectories(root.Address, "*.*", false);
                foreach (var i in dirs)
                {
                    var nextDirName = IO.FileManager.GetRelativePath(root.Address, i);
                    DrawTree(root.RNameType, root.Name, nextDirName);
                }
                ImGuiAPI.TreePop();
            }
        }
        struct stDirMenuData
        {
        }
        List<UIProxy.MenuItemProxy> mDirContextMenu;
        void InitializeDirContextMenu()
        {
            mDirContextMenu = new List<UIProxy.MenuItemProxy>()
            {
                new UIProxy.MenuItemProxy()
                {
                    MenuName = "Browser",
                    Action = (item, data)=>
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo("Explorer.exe");
                        psi.Arguments = "/e,/select," + data.RefObject.ToString().Replace("/", "\\");
                        System.Diagnostics.Process.Start(psi);
                    },
                },
                new UIProxy.MenuItemProxy()
                {
                    MenuName = "Create Folder",
                    Action = (item, data)=>
                    {
                        mCreateFolderDir = data.RefObject.ToString();
                    },
                },
            };
        }
        string mCreateFolderDir = null;
        string mNewFolderName = "NewFolder";
        void DrawCreateFolderDialog(string keyName)
        {
            if (string.IsNullOrEmpty(mCreateFolderDir))
                return;

            var pos = ImGuiAPI.GetWindowPos();
            var min = ImGuiAPI.GetWindowContentRegionMin();
            var max = ImGuiAPI.GetWindowContentRegionMax();
            var pivot = new Vector2(0.5f, 0.5f);
            ImGuiAPI.SetNextWindowPos((min + max) * 0.5f + pos, ImGuiCond_.ImGuiCond_Appearing, in pivot);

            var result = EGui.UIProxy.SingleInputDialog.Draw(keyName, "Folder Name:", ref mNewFolderName, (val) =>
            {
                if (string.IsNullOrEmpty(val))
                    return "Empty folder name";
                if (!Regex.IsMatch(val, @"^[a-zA-Z0-9\\_]+$"))
                    return "Invalid folder name!";
                if (IO.FileManager.DirectoryExists(mCreateFolderDir + mNewFolderName))
                    return $"Directory {mNewFolderName} is exist";

                return null;
            });
            switch(result)
            {
                case UIProxy.SingleInputDialog.enResult.OK:
                    IO.FileManager.CreateDirectory(mCreateFolderDir + mNewFolderName);
                    mNewFolderName = "NewFolder";
                    mCreateFolderDir = null;
                    break;
                case UIProxy.SingleInputDialog.enResult.Cancel:
                    mCreateFolderDir = null;
                    mNewFolderName = "NewFolder";
                    break;
            }
        }
        private unsafe void DrawTree(RName.ERNameType type, string parentDir, string dirName)
        {
            ImGuiTreeNodeFlags_ flags = ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_OpenOnArrow | ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_SpanFullWidth;
            var nextParent = parentDir + dirName + "/";
            var rn = RName.GetRName(nextParent, type);
            var textColor = EGui.UIProxy.StyleConfig.Instance.TextColor;
            if (rn == CurrentDir)
            {
                flags |= ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_Selected;
                textColor = 0xffffffff;
            }

            var style = ImGuiAPI.GetStyle();
            ImGuiAPI.PushID(dirName);
            var treeNodeResult = ImGuiAPI.TreeNodeEx("", flags, "");

            var cmdList = ImGuiAPI.GetWindowDrawList();
            var start = ImGuiAPI.GetItemRectMin();
            //var end = ImGuiAPI.GetItemRectMax();
            //cmdList.AddRect(start, end, 0xFF0000FF, 0, ImDrawFlags_.ImDrawFlags_None, 1);
            //ImGuiAPI.SameLine(0, -1);
            var curPos = ImGuiAPI.GetCursorScreenPos();
            start.X = curPos.X;
            var imgSize = 16;
            if (treeNodeResult)
            {
                var shadowImg = UEngine.Instance.UIManager[FolderOpenImgName] as EGui.UIProxy.ImageProxy;
                if (shadowImg != null)
                    shadowImg.OnDraw(cmdList, start, start + new Vector2(imgSize, imgSize), 0xff558fb6);
            }
            else
            {
                start.X += style->IndentSpacing;
                var shadowImg = UEngine.Instance.UIManager[FolderClosedImgName] as EGui.UIProxy.ImageProxy;
                if (shadowImg != null)
                    shadowImg.OnDraw(cmdList, start, start + new Vector2(imgSize, imgSize), 0xff558fb6);
            }
            start.X += imgSize + style->ItemSpacing.X;
            cmdList.AddText(start, textColor, dirName, null);
            //ImGuiAPI.SameLine(0, 32);
            //ImGuiAPI.Text("_" + dirName);

            var path = RName.GetRName(nextParent, type).Address;
            DrawDirContextMenu(path);
            if (treeNodeResult)
            {   
                if (ImGuiAPI.IsItemActivated())
                {
                    PushHistory(rn);
                }

                var dirs = IO.FileManager.GetDirectories(path, "*.*", false);
                foreach(var i in dirs)
                {
                    if(IO.FileManager.FileExists(i + IO.IAssetMeta.MetaExt))
                        continue;
                    var nextDirName = IO.FileManager.GetRelativePath(path, i);
                    DrawTree(type, nextParent, nextDirName);
                }
                ImGuiAPI.TreePop();
            }
            else
            {
                if (ImGuiAPI.IsItemActivated())
                {
                    PushHistory(rn);
                }
            }

            ImGuiAPI.PopID();
        }
        public unsafe void DrawFiles(RName dir, in Vector2 size)
        {
            var cmdlist = ImGuiAPI.GetWindowDrawList();
            var itemSize = new Vector2(100, 140);
                CreateNewAssets = true;
            
            //var cldPos = ImGuiAPI.GetWindowPos();
            //var cldMin = ImGuiAPI.GetWindowContentRegionMin();
            //var cldMax = ImGuiAPI.GetWindowContentRegionMax();
            //cldMin += cldPos;
            //cldMax += cldPos;
            //////cldMin.Y += 30;
            //cmdlist.PushClipRect(in cldMin, in cldMax, true);
            var style = ImGuiAPI.GetStyle();
            var width = ImGuiAPI.GetWindowContentRegionWidth();
            ImGuiAPI.PushStyleVar(ImGuiStyleVar_.ImGuiStyleVar_ItemSpacing, new Vector2(8, 8));
            ImGuiAPI.PushStyleColor(ImGuiCol_.ImGuiCol_Header, 0x00000000);
            ImGuiAPI.PushStyleColor(ImGuiCol_.ImGuiCol_HeaderHovered, 0x00000000);
            ImGuiAPI.PushStyleColor(ImGuiCol_.ImGuiCol_HeaderActive, 0x00000000);
            var files = IO.FileManager.GetFiles(dir.Address, "*" + IO.IAssetMeta.MetaExt, mWithChildFolders);
            float curPos = 0;
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                file = file.Substring(0, file.Length - IO.IAssetMeta.MetaExt.Length);
                var name = IO.FileManager.GetRelativePath(dir.Address, file);
                if (!string.IsNullOrEmpty(ExtNames))
                {
                    var splits = ExtNames.Split(',');
                    var ext = IO.FileManager.GetExtName(name);
                    bool find = false;
                    for (int extIdx = 0; extIdx < splits.Length; extIdx++)
                    {
                        if (string.Equals(ext, splits[extIdx], StringComparison.OrdinalIgnoreCase))
                        {
                            find = true;
                            break;
                        }
                    }
                    if (!find)
                        continue;

                    if (MacrossBase != null && ext == Bricks.CodeBuilder.UMacross.AssetExt)
                    {
                        var ameta1 = UEngine.Instance.AssetMetaManager.GetAssetMeta(RName.GetRName(dir.Name + name, dir.RNameType)) as Bricks.CodeBuilder.UMacrossAMeta;
                        if (ameta1 == null)
                            continue;

                        if (ameta1.BaseTypeStr != MacrossBase.TypeString)
                        {
                            continue;
                        }
                    }
                }
                

                var filterName = IO.FileManager.GetPureName(name);
                if (!string.IsNullOrEmpty(FilterText))
                {
                    if (filterName.Contains(FilterText, StringComparison.OrdinalIgnoreCase) == false)
                        continue;
                }

                var ameta = UEngine.Instance.AssetMetaManager.GetAssetMeta(RName.GetRName(dir.Name + name, dir.RNameType));
                if (ameta == null)
                    continue;
                var assetTypeName = ameta.GetAssetTypeName();
                if ((mActiveFiltersCount > 0) && !((UIProxy.MenuItemProxy)mFilterMenus[assetTypeName]).Selected)
                    continue;

                DrawItem(in cmdlist, ameta.Icon, ameta, in itemSize);
                curPos += itemSize.X + style->ItemSpacing.X;
                if (curPos + itemSize.X < width)
                {
                    ImGuiAPI.SameLine(0, style->ItemSpacing.X);
                }
                else
                {
                    curPos = 0;
                }
            }

            var metafiles = IO.FileManager.GetFiles(dir.Address, "*.metadata", false);
            if (metafiles.Length > 0)
            {
                var ameta = new Rtti.UMetaVersionMeta();
                ameta.TypeStr = Rtti.UTypeDescGetter<Rtti.UMetaVersion>.TypeDesc.TypeString;
                foreach (var i in metafiles)
                {
                    var name = IO.FileManager.GetPureName(i);

                    var rootType = UEngine.Instance.FileManager.GetRootDirType(i);
                    var rPath = IO.FileManager.GetRelativePath(UEngine.Instance.FileManager.GetRoot(rootType), i);
                    if (rootType == IO.FileManager.ERootDir.Game)
                        ameta.SetAssetName(RName.GetRName(rPath, RName.ERNameType.Game));
                    else if (rootType == IO.FileManager.ERootDir.Engine)
                        ameta.SetAssetName(RName.GetRName(rPath, RName.ERNameType.Engine));
                    else
                        continue;

                    DrawItem(in cmdlist, ameta.Icon, ameta, in itemSize);
                    curPos += itemSize.X + style->ItemSpacing.X;
                    if (curPos + itemSize.X < width)
                    {
                        ImGuiAPI.SameLine(0, 2);
                    }
                    else
                    {
                        curPos = 0;
                    }
                }
            }
            ImGuiAPI.PopStyleVar(1);
            ImGuiAPI.PopStyleColor(3);
            //cmdlist.PopClipRect();

            ////////////////////////////////////////////////////////////
            //drawList.AddRect(ref min, ref max, 0xFF0000FF, 0, ImDrawFlags_.ImDrawFlags_None, 1);
            ////////////////////////////////////////////////////////////

            if (CreateNewAssets && ImGuiAPI.BeginPopupContextWindow("##ContentFilesMenuWindow", ImGuiPopupFlags_.ImGuiPopupFlags_MouseButtonRight))
            {
                var drawList = ImGuiAPI.GetWindowDrawList();
                var menuData = new Support.UAnyPointer();

                mNewAssetMenuItem.OnDraw(in drawList, in menuData);
                ImGuiAPI.EndPopup();
            }
        }
        private void DrawItem(in ImDrawList cmdlist, UUvAnim icon, IO.IAssetMeta ameta, in Vector2 sz)
        {
            ImGuiAPI.PushID($"##{ameta.GetAssetName().Name}");
            ImGuiAPI.Selectable("", ref ameta.IsSelected, ImGuiSelectableFlags_.ImGuiSelectableFlags_None, in sz);
            if (ImGuiAPI.IsItemVisible())
            {
                if(ImGuiAPI.IsWindowFocused(ImGuiFocusedFlags_.ImGuiFocusedFlags_RootAndChildWindows | ImGuiFocusedFlags_.ImGuiFocusedFlags_DockHierarchy))
                {
                    if (ImGuiAPI.IsMouseDoubleClicked(ImGuiMouseButton_.ImGuiMouseButton_Left) && ImGuiAPI.IsItemClicked(ImGuiMouseButton_.ImGuiMouseButton_Left))
                    {
                        var mainEditor = UEngine.Instance.GfxDevice.SlateApplication as Editor.UMainEditorApplication;
                        if (mainEditor != null)
                        {
                            var type = Rtti.UTypeDesc.TypeOf(ameta.TypeStr).SystemType;
                            if (type != null)
                            {
                                var attrs = type.GetCustomAttributes(typeof(Editor.UAssetEditorAttribute), false);
                                if (attrs.Length > 0)
                                {
                                    var editorAttr = attrs[0] as Editor.UAssetEditorAttribute;
                                    var task = mainEditor.AssetEditorManager.OpenEditor(mainEditor, editorAttr.EditorType, ameta.GetAssetName(), null);
                                }
                            }
                        }

                        GlobalSelectedAsset = ameta;
                        for (var i = 0; i < SelectedAssets.Count; i++)
                        {
                            SelectedAssets[i].IsSelected = false;
                        }
                        SelectedAssets.Clear();
                        ameta.IsSelected = true;
                        SelectedAssets.Add(ameta);
                        // todo: multi select
                        ItemSelectedAction?.Invoke(ameta);
                    }
                    if (ImGuiAPI.IsItemClicked(ImGuiMouseButton_.ImGuiMouseButton_Left))
                    {
                        GlobalSelectedAsset = ameta;
                        for(var i=0; i<SelectedAssets.Count; i++)
                        {
                            SelectedAssets[i].IsSelected = false;
                        }
                        SelectedAssets.Clear();
                        SelectedAssets.Add(ameta);
                        // todo: multi select
                        ItemSelectedAction?.Invoke(ameta);
                    }
                    if (ImGuiAPI.IsItemHovered(ImGuiHoveredFlags_.ImGuiHoveredFlags_None))
                    {
                        CtrlUtility.DrawHelper(ameta.GetAssetName().Name, ameta.Description);
                        if(ImGuiAPI.IsMouseDragging(ImGuiMouseButton_.ImGuiMouseButton_Left, 8))
                        {
                            if (ItemDragging.GetCurItem() == null)
                            {
                                var curItem = new UItemDragging.UItem();
                                curItem.Tag = ameta;
                                curItem.AMeta = ameta;
                                curItem.Size = sz;
                                curItem.Browser = this;
                                ItemDragging.SetCurItem(curItem, () =>
                                {
                                    curItem.AMeta.OnDragTo(UEngine.Instance.ViewportSlateManager.GetPressedViewport());
                                    return;
                                });
                            }
                        }
                    }
                }
                ameta.ShowIconTime = UEngine.Instance.CurrentTickCount;
                ameta.OnDraw(in cmdlist, in sz, this);
            }
            ImGuiAPI.PopID();
        }
        Dictionary<string, UIProxy.IUIProxyBase> mFilterMenus = new Dictionary<string, UIProxy.IUIProxyBase>();
        int mActiveFiltersCount = 0;
        bool mWithChildFolders = false;
        void InitializeFilterMenu()
        {
            mFilterMenus.Clear();
            mFilterMenus["##null"] = new UIProxy.MenuItemProxy()
            {
                MenuName = "Clear Filters",
                Action = (item, data)=>
                {
                    foreach(var menuItem in mFilterMenus)
                    {
                        if (menuItem.Key.Contains("##"))
                            continue;
                        var menu = menuItem.Value as EGui.UIProxy.MenuItemProxy;
                        if(menu != null)
                            menu.Selected = false;
                    }
                    mActiveFiltersCount = 0;
                }
            };
            mFilterMenus["##show child"] = new UIProxy.MenuItemProxy()
            {
                MenuName = "With child folders",
                Action = (item, data)=>
                {
                    mWithChildFolders = !mWithChildFolders;
                    item.Selected = mWithChildFolders;
                }
            };
            mFilterMenus["##sep0"] = new UIProxy.NamedMenuSeparator()
            {
                Name = "Asset Types",
            };
            foreach (var service in Rtti.UTypeDescManager.Instance.Services.Values)
            {
                foreach(var typeDesc in service.Types.Values)
                {
                    if(typeDesc.IsSubclassOf(typeof(IO.IAssetMeta)))
                    {
                        var inst = Rtti.UTypeDescManager.CreateInstance(typeDesc) as IO.IAssetMeta;
                        var name = inst.GetAssetTypeName();
                        var menu = new UIProxy.MenuItemProxy()
                        {
                            MenuName = name,
                            Action = (item, data) =>
                            {
                                item.Selected = !item.Selected;
                                if (item.Selected)
                                    mActiveFiltersCount++;
                                else
                                    mActiveFiltersCount--;
                            },
                        };
                        mFilterMenus[name] = menu;
                    }
                }
            }
        }
        void DrawFilterMenu()
        {
            if (ImGuiAPI.BeginPopup("AssetFilterMenus", ImGuiWindowFlags_.ImGuiWindowFlags_NoMove))
            {
                if(mFilterMenus != null)
                {
                    var menuData = new Support.UAnyPointer();
                    var drawList = ImGuiAPI.GetWindowDrawList();
                    foreach(var menu in mFilterMenus.Values)
                    {
                        menu.OnDraw(in drawList, menuData);
                    }
                }
                ImGuiAPI.EndPopup();
            }
        }
        bool mIsPreFolderMouseDown = false;
        bool mIsPreFolderMouseHover = false;
        bool mIsPreFolderDisable = true;
        bool mIsNextFolderMouseDown = false;
        bool mIsNextFolderMouseHover = false;
        bool mIsNextFolderDisable = true;
        bool mIsFilterMouseDown = false;
        bool mIsFilterMouseHover = false;
        bool[] mIsDirMouseDown = new bool[256];
        bool[] mIsDirMouseHover = new bool[256];
        bool[] mIsDirSplitMouseDown = new bool[256];
        bool[] mIsDirSplitMouseHover = new bool[256];
        void DrawToolbar(in ImDrawList cmd)
        {
            EGui.UIProxy.Toolbar.BeginToolbar(cmd);
            if (EGui.UIProxy.ToolbarIconButtonProxy.DrawButton(cmd,
                ref mIsPreFolderMouseDown,
                ref mIsPreFolderMouseHover,
                UEngine.Instance.UIManager[PreFolderImgName] as EGui.UIProxy.ImageProxy,
                "", mIsPreFolderDisable))
            {
                mCurrentDirHistoryIdx--;
                if (mCurrentDirHistoryIdx <= 0)
                {
                    mIsPreFolderDisable = true;
                    mCurrentDirHistoryIdx = 0;
                }
                CurrentDir = mDirHistory[mCurrentDirHistoryIdx];
                if (mCurrentDirHistoryIdx < mDirHistory.Count - 1)
                    mIsNextFolderDisable = false;
            }
            if (EGui.UIProxy.ToolbarIconButtonProxy.DrawButton(cmd,
                ref mIsNextFolderMouseDown,
                ref mIsNextFolderMouseHover,
                UEngine.Instance.UIManager[NextFolderImgName] as EGui.UIProxy.ImageProxy,
                "", mIsNextFolderDisable))
            {
                mCurrentDirHistoryIdx++;
                if (mCurrentDirHistoryIdx >= mDirHistory.Count - 1)
                {
                    mCurrentDirHistoryIdx = mDirHistory.Count - 1;
                    mIsNextFolderDisable = true;
                }
                CurrentDir = mDirHistory[mCurrentDirHistoryIdx];
                if (mCurrentDirHistoryIdx > 0)
                    mIsPreFolderDisable = false;
            }
            if (CurrentDir != null)
            {
                var root = RName.GetRName("", CurrentDir.RNameType);
                var dirName = IO.FileManager.GetRelativePath(root.Address, CurrentDir.Address).TrimEnd('/');
                dirName = CurrentDir.RNameType + "/" + dirName;
                var dirSplits = dirName.Split('/');
                for (int i = 0; i < dirSplits.Length; i++)
                {
                    if (i >= mIsDirMouseDown.Length)
                        break;
                    if (EGui.UIProxy.ToolbarIconButtonProxy.DrawButton(cmd,
                        ref mIsDirMouseDown[i], ref mIsDirMouseHover[i], null, dirSplits[i]))
                    {
                        if (i != dirSplits.Length - 1)
                        {
                            if (i == 0)
                                PushHistory(root);
                            else
                            {
                                string tempDir = "";
                                for (int j = 0; j <= i - 1; j++)
                                    tempDir += dirSplits[j + 1] + "/";
                                tempDir.TrimEnd('/');
                                PushHistory(RName.GetRName(tempDir, CurrentDir.RNameType));
                            }
                        }
                    }
                    if (i < dirSplits.Length - 1)
                    {
                        if (EGui.UIProxy.ToolbarIconButtonProxy.DrawButton(cmd,
                            ref mIsDirSplitMouseDown[i], ref mIsDirSplitMouseHover[i], null, ">"))
                        {

                        }
                    }
                }
            }
            EGui.UIProxy.Toolbar.EndToolbar();
        }

        public UItemDragging ItemDragging = new UItemDragging();
        Vector2 LeftSize = Vector2.Zero;
        Vector2 RightSize;
        struct stAssetImporter
        {
            public IO.IAssetCreateAttribute Creater;
            public string FileName;
        }
        Queue<stAssetImporter> mAssetImporterQueue = new Queue<stAssetImporter>();
        public void EnqueueAssetImporter(IO.IAssetCreateAttribute creater, string fileName)
        {
            var importer = new stAssetImporter()
            {
                Creater = creater,
                FileName = fileName,
            };
            mAssetImporterQueue.Enqueue(importer);
        }
        IO.IAssetCreateAttribute mAssetImporter;
        public string CurrentImporterFile;
        public bool DrawInWindow = true;
        public unsafe void OnDraw()
        {
            if (Visible == false)
                return;

            //            ImGuiAPI.SetNextWindowDockID(DockId, DockCond);
            var name = Name;
            if (string.IsNullOrEmpty(name))
                name = "ContentBrowser";
            bool draw = true;
            if (DrawInWindow)
            {
                ImGuiAPI.SetNextWindowSize(new Vector2(800, 300), ImGuiCond_.ImGuiCond_FirstUseEver);
                draw = EGui.UIProxy.DockPanelProxy.Begin(name, null, ImGuiWindowFlags_.ImGuiWindowFlags_None | ImGuiWindowFlags_.ImGuiWindowFlags_NoScrollbar);
            }
            else
            {
                ImGuiAPI.PushStyleVar(ImGuiStyleVar_.ImGuiStyleVar_FramePadding, UIProxy.StyleConfig.Instance.PanelFramePadding);
                ImGuiAPI.PushStyleColor(ImGuiCol_.ImGuiCol_WindowBg, UIProxy.StyleConfig.Instance.PanelBackground);
                ImGuiAPI.PushStyleColor(ImGuiCol_.ImGuiCol_ChildBg, UIProxy.StyleConfig.Instance.PanelBackground);
            }
            if (draw)
            {
                var cmd = ImGuiAPI.GetWindowDrawList();
                var style = ImGuiAPI.GetStyle();
                DrawToolbar(cmd);

                //                if (ImGuiAPI.IsWindowDocked())
                //                {
                //                    DockId = ImGuiAPI.GetWindowDockID();
                //                }
                ImGuiAPI.Columns(2, null, true);

                if (LeftSize.X == 0)
                {
                    var cltSize = ImGuiAPI.GetWindowContentRegionWidth();
                    ImGuiAPI.SetColumnWidth(0, ((float)cltSize) * 0.3f);
                }

                //bool open = true;
                ImGuiAPI.PushStyleColor(ImGuiCol_.ImGuiCol_ChildBg, 0xFF1A1A1A);
                if (ImGuiAPI.BeginChild("LeftWindow", in Vector2.MinusOne, true, ImGuiWindowFlags_.ImGuiWindowFlags_NoMove))
                {
                    //var winMin = ImGuiAPI.GetWindowPos();
                    //var winMax = winMin + ImGuiAPI.GetWindowSize();
                    //cmd.AddRect(winMin - style->WindowPadding, winMax + style->WindowPadding, 0xff0000ff, 0, ImDrawFlags_.ImDrawFlags_None, 1);
                    var min = ImGuiAPI.GetWindowContentRegionMin();
                    var max = ImGuiAPI.GetWindowContentRegionMax();
                    LeftSize = max - min;

                    ImGuiAPI.PushStyleColor(ImGuiCol_.ImGuiCol_Header, UIProxy.StyleConfig.Instance.TVHeader);
                    ImGuiAPI.PushStyleColor(ImGuiCol_.ImGuiCol_HeaderActive, UIProxy.StyleConfig.Instance.TVHeaderActive);
                    ImGuiAPI.PushStyleColor(ImGuiCol_.ImGuiCol_HeaderHovered, UIProxy.StyleConfig.Instance.TVHeaderHovered);
                    DrawDirectories(RName.GetRName("", RName.ERNameType.Game));
                    DrawDirectories(RName.GetRName("", RName.ERNameType.Engine));
                    ImGuiAPI.PopStyleColor(3);
                }
                ImGuiAPI.EndChild();
                ImGuiAPI.PopStyleColor(1);
                
                ImGuiAPI.NextColumn();

                if (CurrentDir != null)
                {
                    //var cmdlist = ImGuiAPI.GetWindowDrawList();
                    if (mSearchBar != null)
                    {
                        mSearchBar.Width = ImGuiAPI.GetColumnWidth(1) - (style->WindowPadding.X) * 2 - 24;
                        if (mSearchBar.OnDraw(in cmd, in Support.UAnyPointer.Default))
                            FilterText = mSearchBar.SearchText;
                    }
                    else
                    {
                        SureSearchBar();
                    }
                    var frameHeight = ImGuiAPI.GetFrameHeight() + style->FramePadding.Y;
                    if (EGui.UIProxy.ToolbarIconButtonProxy.DrawButton(cmd,
                        ref mIsFilterMouseDown,
                        ref mIsFilterMouseHover,
                        UEngine.Instance.UIManager[FilterImgName] as EGui.UIProxy.ImageProxy,
                        "", false, frameHeight))
                    {
                        ImGuiAPI.OpenPopup("AssetFilterMenus", ImGuiPopupFlags_.ImGuiPopupFlags_None);
                    }
                    if(mActiveFiltersCount > 0)
                    {
                        var pos = ImGuiAPI.GetItemRectMin() + new Vector2(16, 20);
                        cmd.AddCircleFilled(pos, 5, 0xff0000ff, 16);
                    }
                    DrawFilterMenu();

                    if (ImGuiAPI.BeginChild("RightWindow", in Vector2.MinusOne, false, ImGuiWindowFlags_.ImGuiWindowFlags_None))
                    {
                        var min = ImGuiAPI.GetWindowContentRegionMin();
                        var max = ImGuiAPI.GetWindowContentRegionMax();
                        RightSize = max - min;

                        if(UEngine.Instance.InputSystem.IsDropFiles)
                        {
                            var pos = ImGuiAPI.GetWindowPos();
                            var size = ImGuiAPI.GetWindowSize();
                            var mousePos = new Vector2(UEngine.Instance.InputSystem.Mouse.MouseX, UEngine.Instance.InputSystem.Mouse.MouseY);
                            if(mousePos.X >= pos.X && mousePos.X <= (pos.X + size.X) &&
                               mousePos.Y >= pos.Y && mousePos.Y <= (pos.Y + size.Y))
                            {
                                List<Rtti.UTypeDesc> importers = new List<Rtti.UTypeDesc>();
                                foreach (var service in Rtti.UTypeDescManager.Instance.Services.Values)
                                {
                                    foreach (var typeDesc in service.Types.Values)
                                    {
                                        var attrs = typeDesc.GetCustomAttributes(typeof(IO.IAssetCreateAttribute), true);
                                        if (attrs.Length > 0)
                                        {
                                            //var importer = attrs[0] as IO.IAssetCreateAttribute;
                                            //importers.Add(importer);
                                            importers.Add(typeDesc);
                                        }
                                    }
                                }

                                for (int i = 0; i < UEngine.Instance.InputSystem.DropFiles.Count; i++)
                                {
                                    var file = UEngine.Instance.InputSystem.DropFiles[i];
                                    var fileExt = IO.FileManager.GetExtName(file);
                                    for (int j = 0; j < importers.Count; j++)
                                    {
                                        try
                                        {
                                            var attrs = importers[j].GetCustomAttributes(typeof(IO.IAssetCreateAttribute), true);
                                            if (((IO.IAssetCreateAttribute)(attrs[0])).IsAssetSource(fileExt))
                                            {
                                                var assetExtField = Rtti.UTypeDesc.GetField(importers[j].SystemType, "AssetExt");
                                                EnqueueAssetImporter(UEngine.Instance.AssetMetaManager.ImportAsset(CurrentDir, importers[j], (string)assetExtField.GetValue(null)), file);
                                                break;
                                            }
                                        }
                                        catch (Exception)
                                        {

                                        }
                                    }
                                }
                                UEngine.Instance.InputSystem.ClearFilesDrop();
                            }
                        }

                        DrawFiles(CurrentDir, in RightSize);

                    }
                    ImGuiAPI.EndChild();
                }
                    
                ImGuiAPI.NextColumn();

                ImGuiAPI.Columns(1, null, true);

                if (!string.IsNullOrEmpty(mCreateFolderDir))
                {
                    var pathName = IO.FileManager.GetLastestPathName(mCreateFolderDir);
                    if(UEngine.Instance.FileManager.GetRoot(IO.FileManager.ERootDir.Game) == mCreateFolderDir)
                    {
                        pathName = "Game";
                    }
                    else if(UEngine.Instance.FileManager.GetRoot(IO.FileManager.ERootDir.Engine) == mCreateFolderDir)
                    {
                        pathName = "Engine";
                    }
                    else if(UEngine.Instance.FileManager.GetRoot(IO.FileManager.ERootDir.Editor) == mCreateFolderDir)
                    {
                        pathName = "Editor";
                    }
                    var keyName = $"Create Folder in {pathName}";
                    EGui.UIProxy.SingleInputDialog.Open(keyName);
                    DrawCreateFolderDialog(keyName);
                }

            }
            if (DrawInWindow)
                EGui.UIProxy.DockPanelProxy.End();
            else
            {
                ImGuiAPI.PopStyleVar(1);
                ImGuiAPI.PopStyleColor(2);
            }

            if (mAssetImporter != null)
            {
                if(mAssetImporter.OnDraw(this))
                {
                    mAssetImporter = null;
                    CurrentImporterFile = "";
                }
            }
            else if(mAssetImporterQueue.Count > 0)
            {
                var importer = mAssetImporterQueue.Dequeue();
                mAssetImporter = importer.Creater;
                CurrentImporterFile = importer.FileName;
            }
        }
    }
}
