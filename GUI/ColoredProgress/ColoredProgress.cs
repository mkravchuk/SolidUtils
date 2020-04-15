using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Rhino;
using Rhino.UI;
using SolidUtils.GUI.StatusListProgress;

namespace SolidUtils.GUI
{
    public partial class ColoredProgress : Form
    {
        public object showProgressLockSyncObject = new object();
        public bool CancelRequested = false;
        public Func<ForeachParallelResultEnum> ForeachFuncSavedBeforeShowModal { get; set; }
        public Stopwatch WatchUpdateProgress = Stopwatch.StartNew();
        public Stopwatch StartOperation = Stopwatch.StartNew();
        public static ColoredProgress Window { get; set; }
        public StatusItem CurrentProgress { get; set; }
        public bool Wait150millisecondsBeforeShowAnyProgress { get; set; }
        public static bool IsCancelRequested
        {
            get { return Window != null && Window.CancelRequested; }
        }

        public ColoredProgress()
        {
            InitializeComponent();


            // Anti-flicker fix:
            //   With WS_EX_COMPOSITED set, all descendants of a window get bottom-to-top painting order using double-buffering. 
            //   Bottom-to-top painting order allows a descendent window to have translucency (alpha) and transparency (color-key) effects, 
            //   but only if the descendent window also has the WS_EX_TRANSPARENT bit set. 
            //   Double-buffering allows the window and its descendents to be painted without flicker.
            int style = NativeWinAPI.GetWindowLong(this.Handle, NativeWinAPI.GWL_EXSTYLE);
            style |= NativeWinAPI.WS_EX_COMPOSITED;
            NativeWinAPI.SetWindowLong(this.Handle, NativeWinAPI.GWL_EXSTYLE, style);

            Window = this;
            Init();
        }

        public void Init()
        {
            WatchUpdateProgress = Stopwatch.StartNew();
            WatchUpdateProgress.Start();
            StartOperation = Stopwatch.StartNew();
            StartOperation.Start();
            Wait150millisecondsBeforeShowAnyProgress = true;

            progress.Text = log.Group.StackCount > 0
                ? log.Group.StackOfCaptions[1]
                : "";
            progress.Items.Clear();
            CancelRequested = false;
            buttonCancel.Enabled = true;
            //log.temp("!!!!!  cleared");
        }

        public static ColoredProgress AlocateOrReuseWindow()
        {
            if (Window == null)
            {
                //log.temp("ColoredProgress.AlocateOrReuseWindow");
                //lock (Shared.RhinoLock)
                //{
                //RhinoApp.SetFocusToMainWindow(); - takes 50 milliseconds - no use of this function
                //}
                //
                Window = new ColoredProgress();
            }

            if (!Window.Visible)
            {
                Window.Init();
            }

            return Window;
        }

        // dont wait 150 milliseonds before show progress - show it imideatelly
        public static void DisableWait150millisecondsBeforeShowAnyProgress()
        {
            if (Window == null)
            {
                log.warn(g.Temp, "ColoredProgress.ForceToShowProgress - window is not allocated");
            }
            if (Window != null)
            {
                Window.Wait150millisecondsBeforeShowAnyProgress = false;
                lock (Shared.RhinoLock)
                {
                    RhinoApp.Wait(); // process mouse event. also show progress window
                }
            }
        }

        public static void CloseWindow()
        {
            if (Window != null)
            {
                // do nothing - lets do not destroy window - just hide it - speed optimization
                //Window.Dispose();
                //Window = null;
                //log.temp("ColoredProgress.CloseWindow");
                Window.Hide();
            }
        }

        public int GetProgressItemsCount()
        {
            return progress.Items.Count;
        }

        public void ShrinkProgressItems(int maxCount)
        {
            while (progress.Items.Count > maxCount) progress.Items.RemoveAt(progress.Items.Count - 1);
            CurrentProgress = progress.Items[progress.Items.Count - 1];
        }

        public void AddProgress(string caption)
        {
            int MaximumItemsAllowed = Shared.FILEGROUPOPERATIONS_ENABLED ? 10 : 6;

            // show maximum 6 items
            if (progress.Items.Count >= MaximumItemsAllowed)
            {
                progress.Items.RemoveAt(Shared.FILEGROUPOPERATIONS_ENABLED ? 1 : 0);
            }
            // the 6-th (last) item will be reused if we want to push more than 6 progresses - it is made to be shure that some progress activity is shown to user
            if (progress.Items.Count < MaximumItemsAllowed)
            {
                progress.Items.Add(new StatusItem());
            }
            CurrentProgress = progress.Items[progress.Items.Count - 1];
            CurrentProgress.Text = caption;
            CurrentProgress.Status = StatusItem.CurrentStatus.Running;

            if (Shared.FILEGROUPOPERATIONS_ENABLED)
            {
                if (progress.Items.Count == 1)
                {
                    CurrentProgress.CustomEmptySpaceToNextItem = 15;
                }
                else
                {
                    CurrentProgress.CustomFontSize = 5;
                    CurrentProgress.CustomPaddingY = 0;
                }
            }

            //log.temp("!!!!!  added" + progress.Items.Count);
            //labelTitle.Text = caption;
        }

        public static void ExecuteMethodInDialogWindow(string caption, Action a, bool wait150millisecondsBeforeShowAnyProgress = true)
        {
            using (new log.Group(g.ForeachParallel, caption))
            {
                ExecuteParallelMethodInDialogWindow("", () =>
                {
                    //a();
                    Shared.TryCatchAction(a, g.ForeachParallel, "caption");
                    return ForeachParallelResultEnum.Ok;
                }, wait150millisecondsBeforeShowAnyProgress);
            }
        }


        public static ForeachParallelResultEnum ExecuteParallelMethodInDialogWindow(string progressCaption, Func<ForeachParallelResultEnum> foreachFunc, bool wait150millisecondsBeforeShowAnyProgress = true)
        {
            //return foreachFunc();
            //if (Shared.FILEGROUPOPERATIONS_ENABLED)
            //{
            //    return foreachFunc();
            //}
            //log.temp("Progress: " + progressCaption);

            //
            // allocate window
            //
            AlocateOrReuseWindow();

            //
            // Add progress item
            //
            var addProgress = !String.IsNullOrEmpty(progressCaption);
            if (addProgress)
            {
                Window.AddProgress(progressCaption);
                //log.temp("added");
            }

            //
            // run foreach method
            //
            try
            {
                if (!Window.Visible) // if ColoredProgress window is not opened - open if as dialog and execute method inside dialog itself
                {
                    if (!wait150millisecondsBeforeShowAnyProgress) DisableWait150millisecondsBeforeShowAnyProgress();
                    using (new WaitCursor())
                    {
                        Window.ForeachFuncSavedBeforeShowModal = foreachFunc;
                        lock (Shared.RhinoLock)
                        {
                            RhinoApp.Wait();
                        }
                        var h = RhinoApp.MainWindow();
                        var res = Window.ShowDialog(h); // here we will execute 'ForeachFuncSavedBeforeShowModal'                        
                        return res == DialogResult.OK ? ForeachParallelResultEnum.Ok : ForeachParallelResultEnum.Terminated;
                    }
                }
                else
                {
                    return foreachFunc();
                }
            }
            finally
            {
                if (addProgress)
                {
                    Window.CurrentProgress.Status = StatusItem.CurrentStatus.Complete;
                }
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            CancelRequested = true;
            buttonCancel.Enabled = false;
        }

        

        public ForeachParallelResultEnum ShowProgress(int itemIndex, int progressCurrent, int progressMax)
        {
            if (itemIndex == -1 && progressCurrent == -1 && progressMax == -1)
            {
                UpdateProgressGUIonly();
                return ForeachParallelResultEnum.Terminated;
            }

            // itemIndex will be '-1' if this method is called from thread#0 that is responsible for continious update of this progress window
            if (CancelRequested)
            {
                return ForeachParallelResultEnum.Terminated;
            }

            // Parallel.Foreach - use main thread as pool thread - so one thread is main thread - so synchronize call from this thread only - it will be enough. Sync from other thread not working for now.
            //if (!RhinoApp.MainApplicationWindow.InvokeRequired)
            if (Shared.IsExecutingInMainThread)
            {
                UpdateProgressSynced(progressCurrent, progressMax);
            }
            else
            {
                lock (Shared.RhinoInvokeLock)
                {
                    //this._InvokeIfRequired((c) => c.UpdateProgressSynced(progressCurrent, progressMax));
                    RhinoApp.MainApplicationWindow.Invoke((Action)(() => Window.UpdateProgressSynced(progressCurrent, progressMax)));
                }
            }
            //Thread.Sleep(100);
            return ForeachParallelResultEnum.Ok;
        }

        public static ForeachParallelResultEnum ShowProgressStatic(int itemIndex, int progressCurrent, int progressMax)
        {
            if (Window != null)
            {
                return Window.ShowProgress(itemIndex, progressCurrent, progressMax);
            }
            return ForeachParallelResultEnum.Ok;
        }

        public void UpdateProgressGUIonly()
        {
            //labelTimeElapsed.Text = StartOperation.Elapsed.ToString("mm\\:ss\\.ff");
            //log.temp("progress={0}  elapsedFromLastUpdate={1}", progressCurrent, elapsedFromLastUpdate);
            labelTimeElapsed.Text = StartOperation.Elapsed.ToString("mm\\:ss");
            //Application.DoEvents();
            //this.Update();
            //this.Refresh();

            lock (Shared.RhinoLock)
            {
                //RhinoApp.SetFocusToMainWindow();
                RhinoApp.Wait(); // process mouse event. also show first time our form
                //RhinoApp.ReleaseMouseCapture();
            }
        }

        public void UpdateProgressSynced(int progressCurrent, int progressMax)
        {
            lock (showProgressLockSyncObject)
            {
                var elapsedFromFormCreated = StartOperation.Elapsed.TotalMilliseconds;
                if (Wait150millisecondsBeforeShowAnyProgress && elapsedFromFormCreated < 150) return; // show form only if operation is longer 0.15 seconds

                progressCurrent = Convert.ToInt32(((double)progressCurrent / progressMax) * 100);
                progressMax = 100;
                var currentGUIProgressValue = CurrentProgress == null ? 0 : CurrentProgress.Value;

                var elapsedFromLastUpdate = WatchUpdateProgress.Elapsed.TotalMilliseconds;
                if ((progressCurrent != currentGUIProgressValue // progress changed
                     && elapsedFromLastUpdate > 40) // and elapsed 0.04 seconds,  - 25 times per second - smooth progress works slower for 10% but looks much better and looks like programm works faster for 30%
                    || progressCurrent == 0 // or progress just started
                    || elapsedFromLastUpdate > 200 // or 1/5 of second elapsed - this is very important to show seconds and prevent annoying 'Switch to...' modal window
                    )
                {

                    if (CurrentProgress != null && progressCurrent >= 0)
                    {
                        if (CurrentProgress.Maximum != progressMax)
                        {
                            CurrentProgress.Maximum = progressMax;
                        }
                        CurrentProgress.Value = progressCurrent;
                    }

                    UpdateProgressGUIonly();

                    if (progressCurrent >= 0)
                    {
                        WatchUpdateProgress.Restart();
                    }
                }
            }
        }

        //public static void UpdateProgressSyncedStatic(int progressCurrent, int progressMax)
        //{
        //    if (Window != null)
        //    {
        //        Window.UpdateProgressSynced(progressCurrent, progressMax);
        //    }
        //}

        private ForeachParallelResultEnum ExecuteForeachFunc(Func<ForeachParallelResultEnum> foreachFunc)
        {
            var ok = foreachFunc();
            if (CancelRequested || ok == ForeachParallelResultEnum.Terminated)
            {
                return ForeachParallelResultEnum.Terminated;
            }
            else
            {
                return ForeachParallelResultEnum.Ok;
            }
        }

        private void ColoredProgress_Shown(object sender, EventArgs e)
        {
            var res = ExecuteForeachFunc(ForeachFuncSavedBeforeShowModal);
            if (res == ForeachParallelResultEnum.Terminated)
            {
                DialogResult = DialogResult.Cancel;
            }
            else
            {
                DialogResult = DialogResult.OK;
            }
        }


    }
}
