﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Graphics.Pipeline
{
    public class UHitProxy
    {
        public enum EHitproxyType
        {
            None = 0,
            Root = 1,
            FollowParent = 2,
        }
        public uint ProxyId;
        public WeakReference<IProxiable> ProxyObject;
        public bool IsPoxyObject(IProxiable obj)
        {
            IProxiable refObj;
            if (ProxyObject.TryGetTarget(out refObj))
            {
                return refObj == obj;
            }
            return false;
        }
        public Vector4 ConvertHitProxyIdToVector4()
        {
            return new Vector4(((ProxyId >> 24) & 0x000000ff) / 255.0f, ((ProxyId >> 16) & 0x000000ff) / 255.0f, ((ProxyId >> 8) & 0x000000ff) / 255.0f,
                ((ProxyId >> 0) & 0x000000ff) / 255.0f);
        }
        public static UInt32 ConvertCpuTexColorToHitProxyId(IntColor PixelColor)
        {
            return ((UInt32)PixelColor.R << 24 | (UInt32)PixelColor.G << 16 | (UInt32)PixelColor.B << 8 | (UInt32)PixelColor.A << 0);
        }
    }
    public interface IProxiable
    {
        UHitProxy HitProxy { get; set; }
        UHitProxy.EHitproxyType HitproxyType { get; set; }
        void OnHitProxyChanged();
        void GetHitProxyDrawMesh(List<Graphics.Mesh.UMesh> meshes);
    }
    public class UHitproxyManager
    {
        private Dictionary<UInt32, UHitProxy> Proxies
        {
            get;
        } = new Dictionary<UInt32, UHitProxy>();
        private UInt32 HitProxyAllocatorId = 0;
        public void Cleanup()
        {

        }
        public UHitProxy MapProxy(IProxiable proxiable)
        {
            lock (Proxies)
            {
                if (proxiable.HitProxy != null)
                    return proxiable.HitProxy;

                if (HitProxyAllocatorId == uint.MaxValue)
                {
                    Profiler.Log.WriteLine(Profiler.ELogTag.Warning, "UHitproxy", "HitProxyAllocatorId == uint.MaxValue");
                    System.Diagnostics.Debug.Assert(false);
                    HitProxyAllocatorId = 0;
                    foreach (var i in Proxies)
                    {
                        IProxiable obj;                        
                        if(i.Value.ProxyObject.TryGetTarget(out obj))
                        {
                            obj.HitProxy.ProxyId = ++HitProxyAllocatorId;
                            obj.OnHitProxyChanged();
                        }
                    }
                }

                var result = new UHitProxy();
                result.ProxyId = ++HitProxyAllocatorId;
                result.ProxyObject = new WeakReference<IProxiable>(proxiable);
                proxiable.HitProxy = result;
                proxiable.OnHitProxyChanged();
                //proxiable.HitproxyType = proxiable.HitproxyType;

                Proxies.Add(result.ProxyId, result);

                return result;
            }
        }
        public void UnmapProxy(UInt32 id)
        {
            lock (Proxies)
            {
                UHitProxy proxy;
                if (Proxies.TryGetValue(id, out proxy))
                {
                    IProxiable result;
                    if (proxy.ProxyObject.TryGetTarget(out result))
                    {
                        result.HitProxy = null;
                    }
                    proxy.ProxyId = 0;
                    Proxies.Remove(id);
                }
            }
        }
        public IProxiable FindProxy(UInt32 id)
        {
            lock (Proxies)
            {
                UHitProxy proxy;
                if (Proxies.TryGetValue(id, out proxy))
                {
                    IProxiable result;
                    if (proxy.ProxyObject.TryGetTarget(out result))
                    {
                        return result;
                    }
                    else
                    {
                        Proxies.Remove(id);
                    }
                }
                return null;
            }
        }
        public void UnmapProxy(IProxiable proxy)
        {
            if (proxy.HitProxy == null)
                return;

            UnmapProxy(proxy.HitProxy.ProxyId);

            proxy.HitProxy = null;
        }
    }
}
