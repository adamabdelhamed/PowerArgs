using System;
using System.Threading;

namespace PowerArgs.Cli.Physics
{
    public interface ISpaceTimeUI: ILifetime
    {
        Event SizeChanged { get; }
        void Invoke(Action a);
        void Add(SpacialElement element);
        void Remove(SpacialElement element);
        float Width { get; }
        float Height { get; }
        void UpdateBounds(SpacialElement e, float x, float y, int z, float w, float h);
        SpaceTime SpaceTime { get; }
        RealTimeViewingFunction RealTimeViewing { get; set; }
        Event AfterUpdate { get; }
        LocF CameraTopLeft { get; set; }
    }

    public class SpaceTimeUIHost
    {
        private AutoResetEvent resetHandle;
        private bool resizedSinceLastRender;
        private ISpaceTimeUI ui;
        private LocF lastCamera;
        public SpaceTimeUIHost(ISpaceTimeUI ui)
        {
            this.ui = ui;
            resetHandle = new AutoResetEvent(false);
            ui.SpaceTime.Invoke(() =>
            {
                ui.RealTimeViewing = new RealTimeViewingFunction(ui.SpaceTime) { Enabled = true };
                ui.SpaceTime.EndOfCycle.SubscribeForLifetime(() => UpdateViewInternal(), ui);
            });

            ui.OnDisposed(() => resetHandle.Set());

            ui.SizeChanged.SubscribeForLifetime(() => resizedSinceLastRender = true, ui);
        }

       
        private void UpdateViewInternal()
        {
            if (ui.SpaceTime.AddedElements.Count == 0 && ui.SpaceTime.ChangedElements.Count == 0 && ui.SpaceTime.RemovedElements.Count == 0)
            {
                return;
            }
            resetHandle.Reset();
            ui.Invoke(() =>
            {
                foreach (var e in ui.SpaceTime.AddedElements)
                {
                    ui.Add(e);
                    SizeAndLocate(e);
                }

                foreach (var e in ui.SpaceTime.ChangedElements)
                {
                    SizeAndLocate(e);
                }

                foreach (var e in ui.SpaceTime.RemovedElements)
                {
                    ui.Remove(e);
                }

                var cameraChanged = (ui.CameraTopLeft == null ^ lastCamera == null) || (ui.CameraTopLeft != null && ui.CameraTopLeft.Equals(lastCamera) == false);
                if (resizedSinceLastRender || cameraChanged)
                {
                    lastCamera = ui.CameraTopLeft;
                    foreach(var e in ui.SpaceTime.Elements)
                    {
                        SizeAndLocate(e);
                    }
                }

                resetHandle.Set();
            });

            resetHandle.WaitOne();
            resizedSinceLastRender = false;
            ui.SpaceTime.ClearChanges();
            ui.AfterUpdate.Fire();
        }
        private void SizeAndLocate(SpacialElement e)
        {
            float eW = e.Width;
            float eH = e.Height;

            float eL = e.Left;
            float eT = e.Top;

            if (eW < .5f && eW > 0)
            {
                eL -= .5f;
                eW = 1;
            }

            if (eH < .5f && eH > 0)
            {
                eT -= .5f;
                eH = 1;
            }

            float wPer = eW / ui.SpaceTime.Width;
            float hPer = eH / ui.SpaceTime.Height;

            float xPer = eL / ui.SpaceTime.Width;
            float yPer = eT / ui.SpaceTime.Height;


            float x = xPer * ui.Width;
            float y = yPer * ui.Height;
            float w = wPer * ui.Width;
            float h = hPer * ui.Height;

            x -= ui.CameraTopLeft.Left;
            y -= ui.CameraTopLeft.Top;            

            ui.UpdateBounds(e, x, y, e.ZIndex, w, h);
        }
    }
}
