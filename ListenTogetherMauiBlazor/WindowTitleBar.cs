﻿using ListenTogether.Pages;


namespace ListenTogetherMauiBlazor;
internal class WindowTitleBar : IWindowTitleBar
{
	public bool IsMaximized =>
  (Application.Current.MainPage.WidthRequest == double.PositiveInfinity) &&
  (Application.Current.MainPage.HeightRequest == double.PositiveInfinity);
	//public bool IsMaximized => FindOverlappedPresenter().State == OverlappedPresenterState.Maximized;

    public WindowTitleBar()
    {

    }

    public void Minimize()
    {
        //MainThread.BeginInvokeOnMainThread(() =>
        //{
        //    FindOverlappedPresenter().Minimize();
        //});
    }

    public void Maximize()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {

            //if (IsMaximized)
            //{
            //    FindOverlappedPresenter().Restore();
            //}
            //else
            //{
            //    FindOverlappedPresenter().Maximize();
            //    var mauiWindow = App.Current.Windows.First();
            //    //var taskBarHandle = PInvoke.FindWindow("Shell_traywnd", "");
            //    //PInvoke.GetWindowRect(taskBarHandle, out var rct);
            //    var shellHeight = (rct.bottom - rct.top) / mauiWindow.DisplayDensity;
            //    mauiWindow.Height =    mauiWindow.Height - shellHeight;
            //}
        });
    }

    public void Close()
    {
        App.Current.Quit();
    }

    //private OverlappedPresenter FindOverlappedPresenter()
    //{
    //    var mauiWindow = App.Current.Windows.First();
    //    var nativeWindow = mauiWindow.Handler.PlatformView;
    //    IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
    //    WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
    //    AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
        
    //    return (OverlappedPresenter)appWindow.Presenter;
    //}

    public void SetTitle(string title)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var mauiWindow = App.Current.Windows.First();
            mauiWindow.Title = title;
        });
    }
}