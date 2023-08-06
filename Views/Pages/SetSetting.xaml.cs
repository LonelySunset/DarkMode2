﻿using DarkMode_2.Models;
using DarkMode_2.ViewModels;
using DarkMOde_2.Services.Contracts;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Appearance;
using Wpf.Ui.Common;
using Wpf.Ui.Mvvm.Contracts;
using MessageBox = DarkMode_2.Models.MessageBox;

namespace DarkMode_2.Views.Pages;

/// <summary>
/// SetSetting.xaml 的交互逻辑
/// </summary>
public partial class SetSetting 
{

    private readonly ITestWindowService _testWindowService;

    private readonly IThemeService _themeService;

    private readonly ISnackbarService _snackbarService;
    private string ExceptionContent;

    [DllImport("winmm.dll")]
    public static extern bool PlaySound(String Filename, int Mod, int Flags);

    public SetSetting(ISnackbarService snackbarService, IThemeService themeServices, ITestWindowService testWindowService, SetSettingViewModel viewModel)
    {
        InitializeComponent();
        _snackbarService = snackbarService;
        _testWindowService = testWindowService;
        _themeService = themeServices;
        BingData();
        

        //设置初始化
        RegistryKey appkey = Registry.CurrentUser.OpenSubKey(@"Software\DarkMode2", true);
        //开机自启
        if (AutoStartManager.IsAutoStartEnabled())
        {
            Autostart.IsChecked = true;
        }
        //消息通知
        if (appkey.GetValue("Notification").ToString() == "true")
        {
            Notification.IsChecked = true;
        }
        //自动更新
        if(appkey.GetValue("AutoUpdate").ToString() == "true")
        {
            AutoUpdate.IsChecked = true;
        }
        //自动更新日出日落时间
        if (appkey.GetValue("SunRiseSet").ToString() == "false")
        {
            AutoUpdateTime.IsEnabled = false;
        }
        if (appkey.GetValue("AutoUpdateTime").ToString() == "true")
        {
            AutoUpdateTime.IsChecked = true;
        }
        //托盘栏图标
        if (appkey.GetValue("TrayBar").ToString() == "true")
        {
            TrayBar.IsChecked = true;
        }
        //语言
        if (appkey.GetValue("Language").ToString() == "zh-CN")
        {
            languageCombo.SelectedIndex = 0;
        }
        //主题色
        if (appkey.GetValue("ColorMode").ToString() == "Auto")
        {
            ColorCombo.SelectedIndex = 0;
        }else if(appkey.GetValue("ColorMode").ToString() == "Light")
        {
            ColorCombo.SelectedIndex = 1;
        }
        else if (appkey.GetValue("ColorMode").ToString() == "Dark")
        {
            ColorCombo.SelectedIndex = 2;
        }
        //更新渠道
        if(appkey.GetValue("UpdateChannels").ToString() == "Auto")
        {
            UpdateCombo.SelectedIndex = 0;
        }else if(appkey.GetValue("UpdateChannels").ToString() == "Github")
        {
            UpdateCombo.SelectedIndex = 1;
        }else if(appkey.GetValue("UpdateChannels").ToString() == "Gitee")
        {
            UpdateCombo.SelectedIndex = 2;
        }
        appkey.Close();
        
    }

    private void BingData()
    {
        Dictionary<int,string> data = new Dictionary<int,string>();
        data.Add(0, "简体中文(zh-CN)");
        //data.Add(1, "繁體中文(zh-HK)");
        //data.Add(2, "English(en-US)");
        //data.Add(3, "Русский(ru-RU)");
        //data.Add(4, "日本語(ja-JP)");

        Dictionary<int, string> data1 = new Dictionary<int, string>();
        data1.Add(0, "跟随系统");
        data1.Add(1, "浅色");
        data1.Add(2, "深色");

        Dictionary<int, string> data2 = new Dictionary<int, string>();
        data2.Add(0, "自动");
        data2.Add(1, "GitHub渠道");
        data2.Add(2, "Gitee渠道");

        languageCombo.ItemsSource = data;
        ColorCombo.ItemsSource = data1;
        UpdateCombo.ItemsSource= data2;
        
    }
    //开机自启
    private void Autostart_OnClick(object sender, RoutedEventArgs e)
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DarkMode2", true);
        if (AutoStartManager.IsAutoStartEnabled())
        {
            // 如果已经启用了开机自启，则禁用它
            AutoStartManager.DisableAutoStart();
            key.SetValue("DarkMode2", "false");
        }
        else
        {
            // 如果没有启用开机自启，则启用它
            AutoStartManager.EnableAutoStart();
            key.SetValue("DarkMode2", "true");
        }
        key.Close();
    }

    //自动更新
    private void AutoUpdate_onClick(object sender, RoutedEventArgs e)
    {
        //RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DarkMode2", true);
        //string state = key.GetValue("AutoUpdate").ToString();
        //if(state == "true")
        //{
        //    key.SetValue("AutoUpdate", "false");
        //    key.Close();
        //}else if(state == "false")
        //{
        //    key.SetValue("AutoUpdate", "true");
        //    key.Close();
        //}
        OpenSnackbar("该功能暂不可用");
        AutoUpdate.IsChecked = false;

    }
    //自动更新日出日落时间
    private void AutoUpdateTime_OnClick(object sender, RoutedEventArgs e)
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DarkMode2", true);
        string state = key.GetValue("AutoUpdateTime").ToString();
        if (state == "true")
        {
            try
            {
                key.SetValue("AutoUpdateTime", "false");
                key.Close();
            }
            catch (Exception ex)
            {
                MessageBox.OpenMessageBox("错误发生", ex.ToString());
                ExceptionContent = ex.ToString();
            }
        }
        else if (state == "false")
        {
            try
            {
                key.SetValue("AutoUpdateTime", "true");
                key.Close();
            }
            catch (Exception ex)
            {
                MessageBox.OpenMessageBox("错误发生", ex.ToString());
                ExceptionContent = ex.ToString();
            }
        }
    }
    //消息通知
    private void Notification_OnClick(object sender, RoutedEventArgs e)
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DarkMode2", true);
        string state = key.GetValue("Notification").ToString();
        if (state == "true")
        {
            Notification.IsChecked = false;
            try
            {
                key.SetValue("Notification", "false");
                key.Close();
            }
            catch (Exception ex)
            {
                MessageBox.OpenMessageBox("错误发生", ex.ToString());
                ExceptionContent = ex.ToString();
            }
        }
        else if (state == "false")
        {
            try
            {
                key.SetValue("Notification", "true");
                key.Close();
            }
            catch (Exception ex)
            {
                MessageBox.OpenMessageBox("错误发生", ex.ToString());
                ExceptionContent = ex.ToString();
            }
        }
    }

    

    //托盘图标
    private void TrayBar_OnClick(object sender, RoutedEventArgs e)
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DarkMode2", true);
        string state = key.GetValue("TrayBar").ToString();
        if (state == "true")
        {
            TrayBar.IsChecked = false;
            try
            {
                key.SetValue("TrayBar", "false");
                key.Close();
                PlaySound(@"C:\Windows\Media\Windows Notify System Generic.wav", 0, 1);
                OpenSnackbar("下次启动时生效，关闭托盘栏图标后可以使用 Ctrl+Alt+D 来快速打开设置中心");
            }
            catch (Exception ex)
            {
                MessageBox.OpenMessageBox("错误发生", ex.ToString());
                ExceptionContent = ex.ToString();
            }
        }
        else if (state == "false")
        {
            TrayBar.IsChecked = true;
            try
            {
                key.SetValue("TrayBar", "true");
                key.Close();
                PlaySound(@"C:\Windows\Media\Windows Notify System Generic.wav", 0, 1);
                OpenSnackbar("下次启动时生效");
            }
            catch (Exception ex)
            {
                MessageBox.OpenMessageBox("错误发生", ex.ToString());
                ExceptionContent = ex.ToString();
            }
        }
    }

    private void OpenSnackbar(string connect)
    {
        PlaySound(@"C:\Windows\Media\Windows Notify System Generic.wav", 0, 1);
        _snackbarService.Show("提示", connect, SymbolRegular.Alert24);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    IntPtr hwnd = GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
    private void DeveloperMode_Click(object sender, RoutedEventArgs e)
    {
        bool isWindowOpen = false;
        foreach (Window w in Application.Current.Windows)
        {
            if (w is DeveloperModeWindow)
            {
                isWindowOpen = true;
                w.Activate();
            }
        }
        if (!isWindowOpen)
        {
            _testWindowService.Show<DeveloperModeWindow>();
        }
    }

    private void UpdateCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DarkMode2", true);
        if((int)UpdateCombo.SelectedValue == 0)
        {
            key.SetValue("UpdateChannels", "Auto");
        }else if((int)UpdateCombo.SelectedValue == 1)
        {
            key.SetValue("UpdateChannels", "Github");
        }else if((int)UpdateCombo.SelectedValue == 2)
        {
            key.SetValue("UpdateChannels", "Gitee");
        }
        key.Close();
    }

    private void ColorCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DarkMode2", true);
        if ((int)ColorCombo.SelectedValue == 0)
        {
            key.SetValue("ColorMode", "Auto");
            if (DetermineSystemColorMode.GetState() == "light")
            {
                _themeService.SetTheme(ThemeType.Light);
            }
            else if (DetermineSystemColorMode.GetState() == "dark")
            {
                _themeService.SetTheme(ThemeType.Dark);
            }
        }
        else if ((int)ColorCombo.SelectedValue == 1)
        {
            key.SetValue("ColorMode", "Light");
            _themeService.SetTheme(ThemeType.Light);
        }
        else if ((int)ColorCombo.SelectedValue == 2)
        {
            key.SetValue("ColorMode", "Dark");
            _themeService.SetTheme(ThemeType.Dark);
        }
        key.Close();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        //注册表重置
        RegistryInit.RegistryReset();
    }
}
