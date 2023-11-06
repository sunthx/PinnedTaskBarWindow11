using System;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WindowInTaskBarDemo;

public class PinnedTaskBarWindow : Window
{
    //定义窗口的大小
    private const int WindowWidth = 80;
    private const int WindowHeight = 40;

    //窗口置顶
    public const int WS_EX_TOOLWINDOW = 0x00000080;

    //主Timer的句柄
    private uint _mainTimerId;

    //当前窗口的句柄
    private HWND _hwnd;

    private bool _insertToTaskBar;

    //最小化窗口句柄
    private HWND _minHwd;

    private RECT _minRect;

    //通知栏句柄
    private HWND _notifyHwd;

    private RECT _notifyRect;

    //父窗口的句柄
    private HWND _parentHwnd;

    //任务栏句柄
    private HWND _taskBarHwd;

    //RECT
    private RECT _taskBarRect;

    //timer 刷新窗口位置
    private readonly System.Timers.Timer _timer;

    //工具栏句柄
    private HWND _toolBarHwd;
    private RECT _toolBarRect;

    //当前窗口的RECT
    private RECT _windowRect;

    public PinnedTaskBarWindow()
    {
        AllowsTransparency = true;
        WindowStyle = WindowStyle.None;
        Height = WindowHeight;
        Width = WindowWidth;
        ShowInTaskbar = false;

        _timer = new System.Timers.Timer(100);
        _timer.Elapsed += _main_timer;

        Closing += PinnedTaskBarWindow_Closing;
        SourceInitialized += PinnedTaskBarWindow_SourceInitialized;
    }

    private void PinnedTaskBarWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _timer.Stop();
    }

    private void _main_timer(object? sender, ElapsedEventArgs e)
    {
        AdjustWindowPos();
    }

    private void PinnedTaskBarWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _windowRect = new RECT();
        _hwnd = GetSafeHwnd();

        GetTaskBarRelatedWindowHandles();
        GetTaskBarRelatedWindowSizes();

        var existStyle = PInvoke.GetWindowLong(
            GetSafeHwnd(),
            WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

        // 设置窗口的样式
        PInvoke.SetWindowLong(GetSafeHwnd(),
            WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE,
            existStyle | WS_EX_TOOLWINDOW);

        AdjustWindowPos();
        _timer.Start();
    }

    private void AdjustWindowPos()
    {
        if (_hwnd == HWND.Null || !PInvoke.IsWindow(_hwnd))
        {
            return;
        }

        GetTaskBarRelatedWindowSizes();

        var referenceRect = _notifyRect;
        var space = 50;

        _windowRect.left = referenceRect.left - space - WindowWidth;
        _windowRect.top = _insertToTaskBar ?
            (referenceRect.Height - WindowHeight) / 2 :
            referenceRect.top + (referenceRect.Height - WindowHeight) / 2;

        PInvoke.MoveWindow(
            _hwnd,
            _windowRect.left,
            _windowRect.top,
            WindowWidth,
            WindowHeight,
            true);

        PInvoke.SetWindowPos(
            _hwnd,
            new HWND(new IntPtr(-1)),
            0,
            0,
            0,
            0,
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE);
    }

    private HWND GetSafeHwnd()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {

        }

        return new HWND(hwnd);
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
        if (!PInvoke.GetWindowRect(_taskBarHwd, out _taskBarRect)) return false;

        //获取工具栏矩形区域并返回结果，如果失败则返回false
        if (!PInvoke.GetWindowRect(_toolBarHwd, out _toolBarRect)) return false;

        //获取最小化窗口矩形区域并返回结果，如果失败则返回false
        if (!PInvoke.GetWindowRect(_minHwd, out _minRect)) return false;

        //获取通知栏矩形区域并返回结果，如果失败则返回false
        if (!PInvoke.GetWindowRect(_notifyHwd, out _notifyRect)) return false;

        return true;
    }
}