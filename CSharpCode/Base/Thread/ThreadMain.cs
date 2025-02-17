﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Thread
{
    public class ThreadMain : ContextThread
    {
        [ThreadStatic]
        private static Profiler.TimeScope ScopeTickSync = Profiler.TimeScopeManager.GetTimeScope(typeof(ThreadMain), nameof(TickSync));
        [ThreadStatic]
        private static Profiler.TimeScope ScopeWaitTickLogic = Profiler.TimeScopeManager.GetTimeScope(typeof(ThreadMain), "WaitTickLogic");
        public override void Tick()
        {
            BeforeFrame();

            RenderMT();

            TickSync();
            FContextTickableManager.GetInstance().ThreadTick();
        }
        private void BeforeFrame()
        {
            //UEngine.Instance.BeforeFrame();
        }
        private void TickSync()
        {
            using(new Profiler.TimeScopeHelper(ScopeTickSync))
            {
                TickStage = 1;
#if PWindow
                var saved = System.Threading.SynchronizationContext.Current;
                System.Threading.SynchronizationContext.SetSynchronizationContext(null);
                this.TickAwaitEvent();
                System.Threading.SynchronizationContext.SetSynchronizationContext(saved);
#else
                this.TickAwaitEvent();
#endif

                UEngine.Instance.TickSync();

                TickStage = 0;
            }
        }
        private void RenderMT()
        {
            UEngine.Instance.ThreadLogic.LogicEnd.Reset();
            UEngine.Instance.ThreadLogic.LogicBegin.Set();

            UEngine.Instance.ThreadRHI.Tick();

            using (new Profiler.TimeScopeHelper(ScopeWaitTickLogic))
            {
                var evtIndex = System.Threading.WaitHandle.WaitAny(UEngine.Instance.ThreadLogic.LogicEndEvents);
                if (evtIndex == (int)ThreadLogic.EEndEvent.MacrossDebug)
                {
                    System.Diagnostics.Debug.Assert(Macross.UMacrossDebugger.Instance.CurrrentBreak != null);
                }
                //UEngine.Instance.ThreadLogic.mLogicEnd.WaitOne();
                UEngine.Instance.ThreadLogic.LogicEnd.Reset();
            }
        }
    }
}
