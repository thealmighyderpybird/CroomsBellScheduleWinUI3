using CroomsBellScheduleCS.Utils;
using CroomsBellScheduleCS.Views;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.UI.Popups;
using WinRT;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CroomsBellScheduleCS.Windows;

public sealed partial class MainWindow
{
    public static MainWindow Instance = null!;
    public static MainView ViewInstance = null!;
    WindowsSystemDispatcherQueueHelper? m_wsdqHelper; // See below for implementation.
    MicaController? m_backdropController;
    SystemBackdropConfiguration? m_configurationSource;
    public MainWindow()
    {
        InitializeComponent();

        Instance = this;
        ViewInstance = mainView;

        Application.Current.UnhandledException += Current_UnhandledException;
        TrySetSystemBackdrop();
        mainView.PositionWindow();
    }

    public void RemoveMica()
    {
        // Make sure any Mica/Acrylic controller is disposed
        // so it doesn't try to use this closed window.
        if (m_backdropController != null)
        {
            m_backdropController.Dispose();
            m_backdropController = null;
        }
        this.Activated -= Window_Activated;
        m_configurationSource = null;
    }

    public bool TrySetSystemBackdrop()
    {
        if (MicaController.IsSupported())
        {
            m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            // Create the policy object.
            m_configurationSource = new SystemBackdropConfiguration();
            this.Activated += Window_Activated;
            this.Closed += Window_Closed;
            ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

            // Initial configuration state.
            m_configurationSource.IsInputActive = true;
            SetConfigurationSourceTheme();

            m_backdropController = new MicaController();

            // Enable the system backdrop.
            // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
            m_backdropController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
            m_backdropController.SetSystemBackdropConfiguration(m_configurationSource);
            return true; // succeeded
        }

        return false; // Mica is not supported on this system
    }

    private void Window_ThemeChanged(FrameworkElement sender, object args)
    {
        if (m_configurationSource != null)
        {
            SetConfigurationSourceTheme();
        }
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        // Make sure any Mica/Acrylic controller is disposed
        // so it doesn't try to use this closed window.
        if (m_backdropController != null)
        {
            m_backdropController.Dispose();
            m_backdropController = null;
        }
        this.Activated -= Window_Activated;
        m_configurationSource = null;
    }

    private void SetConfigurationSourceTheme()
    {
        //UpdateTheme(((FrameworkElement)this.Content).ActualTheme);
        UpdateTheme(SettingsManager.Settings.Theme);
    }
    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (m_configurationSource == null) return;
        m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
    }

    internal void UpdateTheme(ElementTheme theme)
    {
        if (m_configurationSource == null) return;
        switch (theme)
        {
            case ElementTheme.Dark: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark; break;
            case ElementTheme.Light: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light; break;
            case ElementTheme.Default: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default; break;
        }
    }

    private async void Current_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        MessageDialog dlg = new MessageDialog($"{e.Exception.ToString()}")
        {
            Title = "Unhandled runtime error"
        };
        InitializeWithWindow.Initialize(dlg, WindowNative.GetWindowHandle(this));
        await dlg.ShowAsync();
    }
    class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        object? m_dispatcherQueueController = null;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (global::Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options = new();
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                if (CreateDispatcherQueueController(options, ref m_dispatcherQueueController) != 0)
                {
                    // TODO: show error
                }
            }
        }
    }
}