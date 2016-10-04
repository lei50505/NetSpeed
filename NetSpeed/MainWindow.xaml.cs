using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NetSpeed.source;

namespace NetSpeed
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate void TextBlockDownTextGate(string text);
        private void TextBlockDownText(string text)
        {
            if (TextBlockDown.Dispatcher.Thread != Thread.CurrentThread)
            {
                TextBlockDownTextGate sg = new TextBlockDownTextGate(TextBlockDownText);
                Dispatcher.Invoke(sg, new object[] { text });
            }
            else
            {
                TextBlockDown.Text = text;
            }
        }
        private delegate void TextBlockUpTextGate(string text);
        private void TextBlockUpText(string text)
        {
            if (TextBlockUp.Dispatcher.Thread != Thread.CurrentThread)
            {
                TextBlockUpTextGate sg = new TextBlockUpTextGate(TextBlockUpText);
                Dispatcher.Invoke(sg, new object[] { text });
            }
            else
            {
                TextBlockUp.Text = text;
            }
        }

        List<PerformanceCounter> downCounters = new List<PerformanceCounter>();
        List<PerformanceCounter> upCounters = new List<PerformanceCounter>();

        private string ConfigWindowLeftKey = "ConfigWindowLeft";
        private string ConfigWindowTopKey = "ConfigWindowTop";
        public MainWindow()
        {
            InitializeComponent();

            //判断多开
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);
            if (processes.Length > 1)
            {
                Application.Current.Shutdown();
            }

            //设置初始位置
            string ConfigWindowLeftValue = UConfig.get(ConfigWindowLeftKey);
            string ConfigWindowTopValue = UConfig.get(ConfigWindowTopKey);

            if (ConfigWindowLeftValue == null || ConfigWindowTopValue == null)
            {
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;

                Left = screenWidth - 150;
                Top = screenHeight - 130;
            }
            else
            {
                Left = Convert.ToDouble(ConfigWindowLeftValue);
                Top = Convert.ToDouble(ConfigWindowTopValue);
            }





        }
        long oldDown = 0;
        long oldUp = 0;
        long curDown = 0;
        long curUp = 0;
        double downSpeed = 0;
        double upSpeed = 0;
        private void run(object source, ElapsedEventArgs e)
        {
            curDown = 0;
            curUp = 0;
            foreach (PerformanceCounter downCounter in downCounters)
            {
                curDown += downCounter.NextSample().RawValue;
            }
            foreach (PerformanceCounter upCounter in upCounters)
            {
                curUp += upCounter.NextSample().RawValue;
            }

            downSpeed = (curDown - oldDown) / 1024.0;
            upSpeed = (curUp - oldUp) / 1024.0;

            if (downSpeed >= 0 && upSpeed >= 0)
            {
                TextBlockDownText(downSpeed.ToString("#0.00 kbps"));
                TextBlockUpText(upSpeed.ToString("#0.00 kbps"));
            }
            oldDown = curDown;
            oldUp = curUp;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (FlagMenuItemLockMoveIsChecked == true)
            {
                return;
            }
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
            double currentWindowLeft = this.Left;
            double currentWindowTop = this.Top;
            UConfig.add(ConfigWindowLeftKey, currentWindowLeft.ToString());
            UConfig.add(ConfigWindowTopKey, currentWindowTop.ToString());
        }








        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            this.ResizeMode = ResizeMode.NoResize;

            string ConfigMenuItemLockMoveIsCheckedValue = UConfig.get(ConfigMenuItemLockMoveIsCheckedKey);
            if (ConfigMenuItemLockMoveIsCheckedValue == null)
            {
                FlagMenuItemLockMoveIsChecked = false;
                MenuItemLockMove.IsChecked = false;
            }
            else
            {
                FlagMenuItemLockMoveIsChecked = Convert.ToBoolean(ConfigMenuItemLockMoveIsCheckedValue);
                MenuItemLockMove.IsChecked = Convert.ToBoolean(ConfigMenuItemLockMoveIsCheckedValue);
            }

            string ConfigMenuItemTopMostIsCheckedValue = UConfig.get(ConfigMenuItemTopMostIsCheckedKey);

            if (ConfigMenuItemTopMostIsCheckedKey == null)
            {
                this.Topmost = false;
                MenuItemTopMost.IsChecked = false;
            }
            else
            {
                this.Topmost = Convert.ToBoolean(ConfigMenuItemTopMostIsCheckedValue);
                MenuItemTopMost.IsChecked = Convert.ToBoolean(ConfigMenuItemTopMostIsCheckedValue);
            }


            //设置网卡
            PerformanceCounterCategory category = new PerformanceCounterCategory("Network Interface");
            foreach (string name in category.GetInstanceNames())
            {
                downCounters.Add(new PerformanceCounter("Network Interface", "Bytes Received/sec", name));
                upCounters.Add(new PerformanceCounter("Network Interface", "Bytes Sent/sec", name));
            }

            foreach (PerformanceCounter downCounter in downCounters)
            {
                oldDown += downCounter.NextSample().RawValue;
            }
            foreach (PerformanceCounter upCounter in upCounters)
            {
                oldUp += upCounter.NextSample().RawValue;
            }

            //启动定时器
            System.Timers.Timer t = new System.Timers.Timer(1000);
            t.Elapsed += new ElapsedEventHandler(run);
            t.Enabled = true;
        }

        private bool FlagMenuItemLockMoveIsChecked = false;
        private void MenuItemLockMove_Click(object sender, RoutedEventArgs e)
        {
            if (MenuItemLockMove.IsChecked == true)
            {
                MenuItemLockMove.IsChecked = false;
                FlagMenuItemLockMoveIsChecked = false;
                UConfig.add(ConfigMenuItemLockMoveIsCheckedKey, "false");
                return;
            }
            FlagMenuItemLockMoveIsChecked = true;
            MenuItemLockMove.IsChecked = true;
            UConfig.add(ConfigMenuItemLockMoveIsCheckedKey, "true");
        }

        private string ConfigMenuItemTopMostIsCheckedKey = "ConfigMenuItemTopMostIsChecked";
        private string ConfigMenuItemLockMoveIsCheckedKey = "ConfigMenuItemLockMoveIsChecked";

        private void MenuItemTopMost_Click(object sender, RoutedEventArgs e)
        {
            if (MenuItemTopMost.IsChecked == false)
            {
                this.Topmost = true;
                MenuItemTopMost.IsChecked = true;
                UConfig.add(ConfigMenuItemTopMostIsCheckedKey, "true");
                return;
            }
            this.Topmost = false;
            MenuItemTopMost.IsChecked = false;
            UConfig.add(ConfigMenuItemTopMostIsCheckedKey, "false");
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
