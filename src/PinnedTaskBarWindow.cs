using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WindowInTaskBarDemo;

public class PinnedTaskBarWindow : Window
{
    //定义窗口的大小
    private const int WindowWidth = 40;
    private const int WindowHeight = 40;

    //当前窗口的句柄
    private HWND _hwd;
    //父窗口的句柄
    private HWND _parentHwnd;

    //任务栏句柄
    private HWND _taskBarHwd;
    //工具栏句柄
    private HWND _toolBarHwd;
    //最小化窗口句柄
    private HWND _minHwd;
    //通知栏句柄
    private HWND _notifyHwd;

    //RECT
    private RECT _taskBarRect;
    private RECT _toolBarRect;
    private RECT _minRect;
    private RECT _notifyRect;

    //当前窗口的RECT
    private RECT _windowRect;

    //timer 刷新窗口位置
    private DispatcherTimer _timer;

    public PinnedTaskBarWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        ShowInTaskbar = false;
        Height = WindowHeight;
        Width = WindowWidth;

        Loaded += PinnedTaskBarWindow_Loaded;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += _timer_Tick; ;
        _timer.Start();
    }

    private void _timer_Tick(object? sender, EventArgs e)
    {
        AdjustWindowPos();
    }

    private void PinnedTaskBarWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _hwd = GetSafeHwnd();
        _windowRect = new();

        GetTaskBarRelatedWindowHandles();
        GetTaskBarRelatedWindowSizes();

        //把程序窗口设置成任务栏的子窗口
        _parentHwnd = PInvoke.SetParent(_hwd, _taskBarHwd);

        AdjustWindowPos();
    }

    private void AdjustWindowPos()
    {
        GetTaskBarRelatedWindowSizes();

        var referenceRect = _notifyRect;
        var space = 50;

        _windowRect.left = referenceRect.left - space - WindowWidth;
        _windowRect.top = referenceRect.top + (referenceRect.Height - WindowHeight) / 2;

        // _windowRect.left = 200;
        // _windowRect.top = 200;

        PInvoke.MoveWindow(
            GetSafeHwnd(),
            _windowRect.X,
            _windowRect.Y,
            WindowWidth,
            WindowHeight,
            false);

        // PInvoke.SetWindowPos(
        //     GetSafeHwnd(),
        //     _parentHwnd,
        //     0,
        //     0,
        //     0,
        //     0,
        //     SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE);

    }

    private HWND GetSafeHwnd()
    {
        return new HWND(new WindowInteropHelper(this).Handle);
    }

    private bool GetTaskBarRelatedWindowHandles()
    {
        // Windows 任务栏句柄
        _taskBarHwd = PInvoke.FindWindow("Shell_TrayWnd", null);

        // 获取任务栏中工具栏句柄
        _toolBarHwd = PInvoke.FindWindowEx(
            _taskBarHwd,
            HWND.Null,
            "ReBarWindow32",
            null);

        // 获取工具栏汇中最小化窗口句柄
        _minHwd = PInvoke.FindWindowEx(
            _toolBarHwd,
            HWND.Null,
            "MSTaskSwWClass",
            null);

        // 获取任务栏中通知栏的句柄
        _notifyHwd = PInvoke.FindWindowEx(
            _taskBarHwd,
            HWND.Null,
            "TrayNotifyWnd",
            null);

        return true;
    }

    private bool GetTaskBarRelatedWindowSizes()
    {
        //获取任务栏矩形区域并返回结果，如果失败则返回false
        if (!PInvoke.GetWindowRect(_taskBarHwd, out _taskBarRect))
        {
            return false;
        }

        //获取工具栏矩形区域并返回结果，如果失败则返回false
        if (!PInvoke.GetWindowRect(_toolBarHwd, out _toolBarRect))
        {
            return false;
        }

        //获取最小化窗口矩形区域并返回结果，如果失败则返回false
        if (!PInvoke.GetWindowRect(_minHwd, out _minRect))
        {
            return false;
        }

        //获取通知栏矩形区域并返回结果，如果失败则返回false
        if (!PInvoke.GetWindowRect(_notifyHwd, out _notifyRect))
        {
            return false;
        }

        return true;
    }
}