﻿using DarkMode_2.Models;
using DarkMode_2.Services.MessageQueue;
using DarkMode_2.ViewModels;
using DarkMOde_2.Services.Contracts;
using log4net;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using NHotkey;
using NHotkey.Wpf;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Windows.Devices.Geolocation;
using Windows.Devices.Sensors;
using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;
using MessageBox2 = System.Windows.Forms.MessageBox;

namespace DarkMode_2.Views;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : INavigationWindow
{
    private static readonly ILog log = LogManager.GetLogger(typeof(MainWindow));

    private LightSensor _lightsensor;

    private double _luxValue;

    private DispatcherTimer _timer;

    private readonly ITestWindowService _testWindowService;

    private readonly IThemeService _themeService;

    private readonly ITaskBarService _taskBarService;

    private LanguageHandler _languageHandler;

    private QueueService _queueService;
    public MainWindowViewModel ViewModel
    {
        get;
    }
    public Frame RootFrame { get; private set; }
    public INavigation RootNavigation { get; private set; }

    public MainWindow(MainWindowViewModel viewModel, IPageService pageService, ITaskBarService taskBarService, ITestWindowService testWindowService, IThemeService themeServices)
    {
        ViewModel = viewModel;
        DataContext = this;
        _taskBarService = taskBarService;
        _testWindowService = testWindowService;
        _themeService = themeServices;
        log.Info("DarkMode GUI运行中");
        InitializeComponent();

        // 判断是否为支持的操作系统
        string WinVersion = WindowsVersionHelper.GetWindowsEdition();
        if (WinVersion != "Windows 10" && WinVersion != "Windows 11")
        {
            MessageBox2.Show(LanguageHandler.GetLocalizedString("MainWindow_Tip1") + $"{WinVersion}", LanguageHandler.GetLocalizedString("MainWindow_Tip2"));
            Application.Current.Shutdown();
            log.Warn("不支持的操作系统");
        }

        //消息队列
        this._queueService = new QueueService();
        try
        {
            HotkeyManager.Current.AddOrReplace("Start", Key.D, ModifierKeys.Control | ModifierKeys.Alt, OnStart);
        }
        catch
        {
            MessageBox2.Show(LanguageHandler.GetLocalizedString("MainWindow_Tip3"), LanguageHandler.GetLocalizedString("MainWindow_Tip4"));
            log.Warn("快捷键被占用");
        }

        SetPageService(pageService);

        //注册表初始化
        RegistryInit.RegistryInitialization();

        RegistryKey appkey = Registry.CurrentUser.OpenSubKey(@"Software\DarkMode2", true);
        //新功能注册表异常排除
        if (appkey.GetValue("SwitchMouse") == null)
        {
            RegistryInit.InsertRegistery("SwitchMouse", "false");
        }
        if (appkey.GetValue("AppVersion") == null)
        {
            RegistryInit.InsertRegistery("SwitchMouse", VersionControl.Version());
        }
        if (appkey.GetValue("InstallUpdate") == null)
        {
            RegistryInit.InsertRegistery("InstallUpdate", "false");
        }
        //设置初始化
        if (appkey.GetValue("TrayBar").ToString() == "false")
        {
            HideTrayBarIcon();
        }
        //主题跟随系统定时器
        var timerGetTime = new System.Windows.Forms.Timer();
        //设置定时器属性
        timerGetTime.Tick += new EventHandler(SwitchService);
        timerGetTime.Interval = 500;
        timerGetTime.Enabled = true;
        //开启定时器
        timerGetTime.Start();
        //自动更新日出日落时间
        if (appkey.GetValue("AutoUpdateTime").ToString() != "false" && appkey.GetValue("SunRiseSet").ToString() != "false")
        {
            AutoUpdataTime();
        }
        //感光模式
        if (appkey.GetValue("PhotosensitiveMode").ToString() == "true")
        {
            _lightsensor = LightSensor.GetDefault();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }
        appkey.Close();

    }

    // 自动检查更新
    private void AutoUpdate()
    {
        _queueService.AddMessage(async () =>
        {
            NewVersion update = new NewVersion();

            string res = await update.CheckUpdate();
            Match match = Regex.Match(res, @"\d+\.\d+\.\d+\.\d+");
            if (match.Success)
            {
                ToastHelper.ShowToast("AutoUpdata_title", "AutoUpdata_content");
                DownloadWindow window = new DownloadWindow(match.Groups[1].Value);
                window.ShowDialog();
            }
        });
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        _luxValue = GetLightIntensityValue();
    }

    private double GetLightIntensityValue()
    {
        return _lightsensor.GetCurrentReading().IlluminanceInLux;
    }

    //自动更新日出日落时间
    private void AutoUpdataTime()
    {
        _queueService.AddMessage(async () =>
        {
            try
            {
                //使用windows自带的位置服务获取当前位置
                var geoPosition = await new Geolocator().GetGeopositionAsync();
                var latitude = geoPosition.Coordinate.Point.Position.Latitude;
                var longitude = geoPosition.Coordinate.Point.Position.Longitude;

                var locationName = await new LocationService().GetLocationName(latitude, longitude);
                SunTimeResult result = TimeConverter.GetSunTime(DateTime.Now, longitude, latitude);
                DateTime date = DateTime.Now;
                string sunriseTime = result.SunriseTime.ToString("HH:mm");
                string sunsetTime = result.SunsetTime.ToString("HH:mm");

                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DarkMode2", true);
                key.SetValue("startTime", sunriseTime);
                key.SetValue("endTime", sunsetTime);
                key.Close();
            }
            catch (Exception ex)
            {
                MessageBox2.Show(LanguageHandler.GetLocalizedString("MainWindow_Tip5"), LanguageHandler.GetLocalizedString("MainWindow_Tip6"));
                log.Warn(ex.Message);
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DarkMode2", true);
                key.SetValue("SunRiseSet", "false");
                key.SetValue("AutoUpdateTime", "false");
                key.Close();
            }
        });
    }

    public void SwitchService(Object myObject, EventArgs myEventArgs)
    {
        _languageHandler = new LanguageHandler(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "i18n"));
        _languageHandler.ChangeLanguage(RegistryInit.GetSavedLanguageCode());
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DarkMode2", true);
        //判断是否在浅色时间段内
        string startLightTime = key.GetValue("startTime").ToString();
        string endLightTime = key.GetValue("endTime").ToString();
        TimeSpan _startLightTime = DateTime.Parse(startLightTime).TimeOfDay;
        TimeSpan _endLightTime = DateTime.Parse(endLightTime).TimeOfDay;
        DateTime dateTime = Convert.ToDateTime(DateTime.Now.ToString("t"));
        TimeSpan dspNow = dateTime.TimeOfDay;

        if (key.GetValue("PhotosensitiveMode").ToString() == "false")
        {
            if (dspNow > _startLightTime && dspNow < _endLightTime)
            {
                //在时间段内
                SwitchMode.switchMode("light");
                //Console.WriteLine("切换为浅色");
            }
            else
            {
                //不在时间段内
                SwitchMode.switchMode("dark");
                //Console.WriteLine("切换为深色");
            }
        }
        else
        {
            double luxValue = _luxValue;
            Console.WriteLine(luxValue.ToString());
            if (luxValue > 17.0)
            {
                SwitchMode.switchMode("light");
            }
            else
            {
                SwitchMode.switchMode("dark");
            }
        }
    }


    private void OnStart(object sender, HotkeyEventArgs e)
    {
        bool isWindowOpen = false;
        foreach (Window w in Application.Current.Windows)
        {
            if (w is SettingsWindow)
            {
                isWindowOpen = true;
                w.Activate();
            }
        }
        if (!isWindowOpen)
        {
            _testWindowService.Show<SettingsWindow>();
        }
    }

    private void HideTrayBarIcon()
    {
        NotifyIcon.Visibility = Visibility.Collapsed;
    }

    public Frame GetFrame() => RootFrame;

    public INavigation GetNavigation() => RootNavigation;

    public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

    public void SetPageService(IPageService pageService) { }

    public void ShowWindow() => Show();

    public void CloseWindow() => Close();

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DarkMode2", false);

        _languageHandler = new LanguageHandler(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "i18n"));
        _languageHandler.ChangeLanguage(RegistryInit.GetSavedLanguageCode());

        //隐藏窗口
        this.Hide();
        //自动更新
        if (key.GetValue("AutoUpdate").ToString() == "true")
        {
            AutoUpdate();
        }

        //消息通知

        if (key.GetValue("Notification").ToString() == "true")
        {
            ToastHelper.ShowToast("MainWindow_Tip7", "MainWindow_Tip8");
        }
        key.Close();
    }

    private void Start_OnClick(object sender, RoutedEventArgs e)
    {
        //打开SettingsWindow窗口
        bool isWindowOpen = false;
        foreach (Window w in Application.Current.Windows)
        {
            if (w is SettingsWindow)
            {
                isWindowOpen = true;
                w.Activate();
            }
        }
        if (!isWindowOpen)
        {
            _testWindowService.Show<SettingsWindow>();
        }
    }

    private void Exit_OnClick(object sender, RoutedEventArgs e)
    {
        //退出程序
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DarkMode2", true);
        key.SetValue("ProgramExit", "true");
        key.Close();
        ToastNotificationManagerCompat.Uninstall();
        Application.Current.Shutdown();
    }
}
