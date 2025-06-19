using System;
using System.Windows;
using System.Windows.Interop;

namespace EveIndyCalc
{
    public partial class ProductionPlannerWindow : Window
    {
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int HTCLIENT = 1;
        private const int WM_NCHITTEST = 0x0084;

        public ProductionPlannerWindow()
        {
            InitializeComponent();
            SourceInitialized += (s, e) =>
            {
                IntPtr handle = (new WindowInteropHelper(this)).Handle;
                HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void Expand_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                this.WindowState = WindowState.Maximized;
            else
                this.WindowState = WindowState.Normal;
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCHITTEST)
            {
                Point position = PointFromScreen(new Point((lParam.ToInt32() & 0xFFFF), (lParam.ToInt32() >> 16)));
                double edgeSize = 8;

                if (position.X <= edgeSize)
                {
                    if (position.Y <= edgeSize)
                    {
                        handled = true;
                        return (IntPtr)HTTOPLEFT;
                    }
                    else if (position.Y >= ActualHeight - edgeSize)
                    {
                        handled = true;
                        return (IntPtr)HTBOTTOMLEFT;
                    }

                    handled = true;
                    return (IntPtr)HTLEFT;
                }
                else if (position.X >= ActualWidth - edgeSize)
                {
                    if (position.Y <= edgeSize)
                    {
                        handled = true;
                        return (IntPtr)HTTOPRIGHT;
                    }
                    else if (position.Y >= ActualHeight - edgeSize)
                    {
                        handled = true;
                        return (IntPtr)HTBOTTOMRIGHT;
                    }

                    handled = true;
                    return (IntPtr)HTRIGHT;
                }
                else if (position.Y <= edgeSize)
                {
                    handled = true;
                    return (IntPtr)HTTOP;
                }
                else if (position.Y >= ActualHeight - edgeSize)
                {
                    handled = true;
                    return (IntPtr)HTBOTTOM;
                }
            }

            return IntPtr.Zero;
        }
    }
} 