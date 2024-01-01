using Windows.UI.ViewManagement;
using QuickAttach.Helpers;

namespace QuickAttach;

public sealed partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _settings = new UISettings();
        _settings.ColorValuesChanged += Settings_ColorValuesChanged;
    }

    private void Settings_ColorValuesChanged(UISettings sender, object args) =>
        _dispatcherQueue.TryEnqueue(TitleBarHelper.ApplySystemThemeToCaptionButtons);

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly UISettings _settings;
}