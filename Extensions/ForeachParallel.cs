using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Rhino;
using SolidUtils.GUI;

namespace SolidUtils
{
    public enum ForeachParallelResultEnum
    {
        Ok, Terminated
    }


    public static class ForeachParallel
    {
        /// <summary>
        /// Executes action with some timeout - to protect from neverendless actions.
        /// Slows down execution by 30%, so better dont use it if not neccessary.
        /// </summary>
        /// <param name="timeout">timeout for execution in milliseconds</param>
        /// <param name="threadName">thread name</param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static bool RunActionWithTimeout(int timeout, string threadName, Action predicate)
        {
            uint windowsThreadId = 0;
            Thread thread = new Thread(() =>
            {
                windowsThreadId = Win32.GetCurrentThreadId(); // remember task thread id so we will be able to suspend if later to be able to abort it
                predicate();
            }) { Name = threadName };
            thread.Start();
            if (!thread.Join(timeout))
            {
                log.info(g.ForeachParallel, "Thread {0} is running for more than {1} milliseconds and will be terminated", threadName, timeout);
                thread.Abort(); // terminate thread
                if (!thread.Join(100)) // wait for short time - check if thread is not willing terminate because it is running unmanaged code
                {
                    //_Process.SuspendThread(windowsThreadId); // suspends the thread and thus allows C# to terminate thread - because thread is now in wait/suspend state
                    _Process.TerminateThread(windowsThreadId);
                }
                return thread.Join(100); // wait for short time - hopefully thread is finished
            }
            return true;
        }

        public delegate ForeachParallelResultEnum OnShowProgressDelegate(int itemIndex, int progressCurrent, int progressMax);


        public static ForeachParallelResultEnum _ForeachParallel<TSource>(this ICollection<TSource> collection, string caption, Action<TSource> action, bool forceSingleTreading = false, OnShowProgressDelegate OnProgress = null)
        {
            // force sinlge threading in debug mode just to improve debuging step-in-step-out
            if (!Shared.UseMultithreading && Shared.IsDebugMode)
            {
                forceSingleTreading = true;
            }
            List<TSource> list = (collection is List<TSource>) 
                ? (List<TSource>)collection 
                : collection.ToList();

            Shared.IsForeachParallelInProgress = true;
            try
            {
                var progressWait = 0;
                var progressCurrent = 0;
                var progressMax = list.Count;
                if (forceSingleTreading)
                {
                    for (var itemIndex = 0; itemIndex < list.Count; itemIndex++)
                    {
                        if (OnProgress != null && OnProgress(itemIndex, progressCurrent, progressMax) == ForeachParallelResultEnum.Terminated)
                        {
                            break;
                        }
                        progressCurrent++;
                        var item = list[itemIndex];
                        //action(item)
                        Shared.TryCatchAction(() => action(item), g.ForeachParallel, item);
                    }
                }
                else
                {
                    int maxDegreeOfParallelism = 1;
                    if (Shared.UseMultithreading)
                    {
                        maxDegreeOfParallelism = Shared.UseMultithreading_MaxThreadsCount;
                        if (maxDegreeOfParallelism == 0) maxDegreeOfParallelism = Shared.Environment_ProcessorCount;
                        if (maxDegreeOfParallelism < 0) maxDegreeOfParallelism = Shared.Environment_ProcessorCount + maxDegreeOfParallelism;  //dicrease CPU count by adding negative value
                        if (maxDegreeOfParallelism <= 0) maxDegreeOfParallelism = Shared.Environment_ProcessorCount;
                    }
                    maxDegreeOfParallelism++; // additional thread for updater thread
                    var parallelOptions = new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = maxDegreeOfParallelism
                    };

                    if (OnProgress == null)
                    {
                        Parallel.ForEach(list, parallelOptions, (item, state) =>
                        {
                            Shared.TryCatchActionFast(() => action(item));
                        });
                    }
                    else
                    {
                        var showProgressLockSyncObject = new object();
                        if (OnProgress == null || OnProgress(-1, progressCurrent, progressMax) != ForeachParallelResultEnum.Terminated)
                        {
                            var list_indexes = new List<int>();
                            list_indexes.Add(-1); // one additional index to handle update thread
                            for (int i = 0; i < list.Count; i++) list_indexes.Add(i); // iota
                            //log.temp("Parallel.ForEach    list.Count={0}", list.Count);

                            Parallel.ForEach(list_indexes, parallelOptions, (itemIndex, state) =>
                            {
                                //log.temp("itemIndex={0}", itemIndex);
                                if (itemIndex == -1) 
                                {
                                    // this tread#0 solves issue with annoying modal window 'Switch '
                                    var sleepedTimes = 0;
                                    while (!state.IsStopped)
                                    {
                                        Thread.Sleep(1); // wait for a little to be quick when progress can be stoped - this is very important for reducing consumed progress time
                                        sleepedTimes++;
                                        if (progressCurrent >= progressMax)
                                        {
                                            state.Stop();
                                        }
                                        else
                                        {
                                            if (sleepedTimes > 50)
                                            {
                                                sleepedTimes = 0;
                                                if (OnProgress != null)
                                                {
                                                    lock (showProgressLockSyncObject)
                                                    {
                                                        if (OnProgress(-1, progressCurrent, progressMax) == ForeachParallelResultEnum.Terminated)
                                                        {
                                                            state.Stop();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    // wiat until all started items finished 
                                    int waitTimesCount = 0;
                                    while (progressWait > progressCurrent)
                                    {
                                        Thread.Sleep(50);
                                        if (OnProgress != null) OnProgress(-1, -1, -1);
                                        waitTimesCount++;
                                        if (waitTimesCount%3 == 1)
                                        {
                                            //log.temp("waiting {0} items to finish", progressWait - progressCurrent);
                                            if (ColoredProgress.Window != null)
                                            {
                                                ColoredProgress.Window.CurrentProgress.Text = "Waiting for {0} threads to finish... Please wait..."._Format(progressWait - progressCurrent);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (!state.IsStopped)
                                    {
                                        Interlocked.Increment(ref progressWait);
                                        var item = list[itemIndex];
                                        try
                                        {
                                            if (OnProgress == null || Shared.MultithreadingTimeout == 0)
                                            {
                                                Shared.TryCatchAction(() => action(item), g.ForeachParallel, item);
                                            }
                                            else
                                            {
                                                string captionOnItem = caption + " " + item.ToString();
                                                RunActionWithTimeout(Shared.MultithreadingTimeout, captionOnItem, () => Shared.TryCatchAction(() => action(item), g.ForeachParallel, item));
                                            }
                                        }
                                        catch (FailedToFixIssue ex)
                                        {
                                            // nothing
                                            //log.info(g.ForeachParallel, "FailedToFixIssue");
                                        }
                                        catch (ThreadAbortException ex)
                                        {
                                            // nothing
                                            //log.info(g.ForeachParallel, "ThreadAbortException");
                                        }
                                        catch (Exception ex) // we must use this because 'Shared.TryCatchAction' may not catch exception if options was disabled
                                        {
                                            throw; // rethrow only if this is not a expected issue
                                        }
                                        finally // we must use this because 'Shared.TryCatchAction' may not catch exception if options was disabled
                                        {
                                            if (OnProgress != null)
                                            {
                                                lock (showProgressLockSyncObject)
                                                {
                                                    if (OnProgress(itemIndex, progressCurrent, progressMax) == ForeachParallelResultEnum.Terminated)
                                                    {
                                                        state.Stop();
                                                    }
                                                }
                                            }
                                            Interlocked.Increment(ref progressCurrent);
                                        }
                                    }
                                }
                            });
                        }
                    }
                }

                //
                // end 
                //
                if (OnProgress != null)
                {
                    OnProgress(0, progressMax, progressMax);
                }
                return progressCurrent == progressMax
                ? ForeachParallelResultEnum.Ok
                : ForeachParallelResultEnum.Terminated;
            }
            finally
            {
                Shared.IsForeachParallelInProgress = false;
            }
        }


        public static ForeachParallelResultEnum _ForeachParallel_WithProgressWindow<TSource>(this ICollection<TSource> list, string caption, Action<TSource> action, bool forceSingleTreading = false)
        {
            if (list.Count == 0)
            {
                return ForeachParallelResultEnum.Ok;
            }

            using (new log.GroupDEBUG(g.ForeachParallel, caption))
            {
                return ColoredProgress.ExecuteParallelMethodInDialogWindow(caption, () => list._ForeachParallel(caption, action, forceSingleTreading, ColoredProgress.ShowProgressStatic));
            }
        }


        public static ForeachParallelResultEnum _ForeachParallel_WithOrWithoutProgressWindow<TSource>(this ICollection<TSource> list, bool showProgressWindow, string caption, Action<TSource> action, bool forceSingleTreading = false)
        {
            if (showProgressWindow)
            {
                return list._ForeachParallel_WithProgressWindow(caption, action, forceSingleTreading);
            }
            else
            {
                using (new log.GroupDEBUG(g.ForeachParallel, caption))
                {
                    return list._ForeachParallel(caption, action, forceSingleTreading);
                }
            }
        }


        private static ForeachParallelResultEnum ShowProgressDummy(int itemIndex, int progressCurrent, int progressMax)
        {
            return ForeachParallelResultEnum.Ok;
        }
    }
}
