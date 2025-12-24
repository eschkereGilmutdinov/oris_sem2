using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Windows.Forms.Timer;

namespace ClientWinForm
{
    public static class Animations
    {
        public static Task FlyCardToHandAsync(
           Form hostForm,
           Image cardImage,
           Control deckControl,
           ListView handListView,
           Size? cardSize = null,
           int durationMs = 350,
           int arc = 40,
           int intervalMs = 15)
        {
            if (hostForm == null) throw new ArgumentNullException(nameof(hostForm));
            if (cardImage == null) throw new ArgumentNullException(nameof(cardImage));
            if (deckControl == null) throw new ArgumentNullException(nameof(deckControl));
            if (handListView == null) throw new ArgumentNullException(nameof(handListView));

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var size = cardSize ?? new Size(120, 170);

            var fly = new PictureBox
            {
                Image = cardImage,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = size,
                BackColor = Color.Transparent
            };

            var start = GetCenterOnForm(hostForm, deckControl);
            var end = GetHandTargetPointOnForm(hostForm, handListView);

            fly.Left = start.X - fly.Width / 2;
            fly.Top = start.Y - fly.Height / 2;

            hostForm.Controls.Add(fly);
            fly.BringToFront();

            var sw = Stopwatch.StartNew();
            var timer = new Timer { Interval = Math.Max(1, intervalMs) };

            void Cleanup()
            {
                try
                {
                    timer.Stop();
                    timer.Dispose();
                }
                catch { /* ignore */ }

                try
                {
                    if (!fly.IsDisposed)
                    {
                        hostForm.Controls.Remove(fly);
                        fly.Dispose();
                    }
                }
                catch { /* ignore */ }
            }

            hostForm.FormClosed += (_, __) =>
            {
                Cleanup();
                if (!tcs.Task.IsCompleted) tcs.TrySetCanceled();
            };

            timer.Tick += (_, __) =>
            {
                double p = sw.Elapsed.TotalMilliseconds / Math.Max(1, durationMs);
                if (p >= 1) p = 1;

                double e = EaseInOut(p);

                int x = (int)Math.Round(Lerp(start.X - fly.Width / 2.0, end.X - fly.Width / 2.0, e));
                int y = (int)Math.Round(Lerp(start.Y - fly.Height / 2.0, end.Y - fly.Height / 2.0, e));

                if (arc != 0)
                    y -= (int)Math.Round(Math.Sin(p * Math.PI) * arc);

                fly.Left = x;
                fly.Top = y;

                if (p >= 1)
                {
                    Cleanup();
                    tcs.TrySetResult(true);
                }
            };

            timer.Start();
            return tcs.Task;
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        private static double EaseInOut(double t) => t * t * (3 - 2 * t);

        private static Point GetCenterOnForm(Form hostForm, Control c)
        {
            var screen = c.Parent.PointToScreen(new Point(c.Left, c.Top));
            var p = hostForm.PointToClient(screen);
            return new Point(p.X + c.Width / 2, p.Y + c.Height / 2);
        }

        private static Point GetHandTargetPointOnForm(Form hostForm, ListView hand)
        {
            if (hand.Items.Count > 0)
            {
                var last = hand.Items[hand.Items.Count - 1];
                var r = last.GetBounds(ItemBoundsPortion.Entire);

                var local = new Point(r.Right + 10, r.Top + r.Height / 2);
                var screen = hand.PointToScreen(local);
                return hostForm.PointToClient(screen);
            }

            var centerLocal = new Point(hand.Width / 2, hand.Height / 2);
            var centerScreen = hand.PointToScreen(centerLocal);
            return hostForm.PointToClient(centerScreen);
        }
    }
}
