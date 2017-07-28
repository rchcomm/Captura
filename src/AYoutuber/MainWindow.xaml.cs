﻿using Captura.Models;
using Captura.ViewModels;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Captura
{
    public partial class MainWindow
    {        
        public MainWindow()
        {
            ServiceProvider.Register<IRegionProvider>(ServiceName.RegionProvider, new RegionSelector());

            ServiceProvider.Register<IMessageProvider>(ServiceName.Message, new MessageProvider());

            ServiceProvider.Register<IWebCamProvider>(ServiceName.WebCam, new WebCamProvider());
                        
            InitializeComponent();

            if (string.IsNullOrEmpty(Settings.Instance.FFMpegFolder))
            {
                Settings.Instance.FFMpegFolder = Path.Combine(System.Environment.CurrentDirectory, "Modules");
            }

            if (App.CmdOptions.Tray)
                Hide();

            ServiceProvider.Register<Action<bool>>(ServiceName.Minimize, minimize =>
            {
                WindowState = minimize ? WindowState.Minimized : WindowState.Normal;
            });

            // Register for Windows Messages
            ComponentDispatcher.ThreadPreprocessMessage += (ref MSG Message, ref bool Handled) =>
            {
                const int WmHotkey = 786;

                if (Message.message == WmHotkey)
                {
                    var id = Message.wParam.ToInt32();

                    ServiceProvider.RaiseHotKeyPressed(id);
                }
            };

            ServiceProvider.Register<ISystemTray>(ServiceName.SystemTray, new SystemTray(SystemTray));

            Closing += (s, e) =>
            {
                if (!TryExit())
                    e.Cancel = true;
            };

            var mainViewModel = DataContext as MainViewModel;
            mainViewModel.Init(!App.CmdOptions.NoPersist, true, !App.CmdOptions.Reset, !App.CmdOptions.NoHotkeys);

            mainViewModel.WorkViewModel.NewWorkCommand = new DelegateCommand(() => {
                if (ServiceProvider.Messenger.ShowYesNo("New Work?", "New Work"))
                {
                    Settings.Instance.LastWorkNumber++;
                }
            });

            mainViewModel.WorkViewModel.EditWorkCommand = new DelegateCommand(() => {
                TimelineWindow timelineWindow = new TimelineWindow();
                timelineWindow.DataContext = new TimelineViewModel(Settings.Instance.LastWorkNumber, Settings.Instance.OutPathWithWork());
                timelineWindow.Height = 800;
                timelineWindow.Width = 1200;
                timelineWindow.Show();
            });
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (WindowState == WindowState.Minimized && Settings.Instance.MinimizeToTray)
                Hide();
        }

        void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();

            e.Handled = true;
        }

        void MinButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        void SystemTray_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                Hide();
            }
            else
            {
                Show();

                WindowState = WindowState.Normal;

                Activate();
            }
        }

        bool TryExit()
        {
            var vm = DataContext as MainViewModel;

            if (vm.RecorderState == RecorderState.Recording)
            {
                if (!ServiceProvider.Messenger.ShowYesNo("A Recording is in progress. Are you sure you want to exit?", "Confirm Exit"))
                    return false;
            }

            vm.Dispose();
            SystemTray.Dispose();
            Application.Current.Shutdown();

            return true;
        }

        void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            TryExit();
        }
    }
}
