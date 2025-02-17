﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Dynamic;

namespace EngineNS.EGui.UIProxy
{
    public interface IUIProxyBase
    {
        void Cleanup();
        System.Threading.Tasks.Task<bool> Initialize();
        bool OnDraw(in ImDrawList drawList, in Support.UAnyPointer drawData);
    }
    public class UIManager : UModule<UEngine>
    {
        Dictionary<string, IUIProxyBase> mDic = new Dictionary<string, IUIProxyBase>();

        public int Count => mDic.Count;

        public IUIProxyBase this[string key]
        {
            get
            {
                IUIProxyBase item = null;
                mDic.TryGetValue(key, out item);
                return item;
            }
            set
            {
                lock(mDic)
                {
                    mDic[key] = value;
                }
            }
        }
        public IUIProxyBase GetUIProxy(string key, in EngineNS.Thickness uvMargin)
        {
            IUIProxyBase item = null;
            if (mDic.TryGetValue(key, out item))
                return item;
            item = new EGui.UIProxy.BoxImageProxy(RName.GetRName(key, RName.ERNameType.Engine), uvMargin);
            mDic.Add(key, item);
            return item;
        }

        public override void Cleanup(UEngine host)
        {
            foreach(var item in mDic.Values)
            {
                item.Cleanup();
            }
            mDic.Clear();
        }
    }

    public class UIPanelProxy
    {
        public static bool BeginPanel(string str_id, in Vector2 size, bool border, ImGuiWindowFlags_ flags)
        {
            return ImGuiAPI.BeginChild(str_id, size, border, flags);
        }
    }
}

namespace EngineNS
{
    public partial class UEngine
    {
        public EGui.UIProxy.UIManager UIManager { get; } = new EGui.UIProxy.UIManager();
    }
}