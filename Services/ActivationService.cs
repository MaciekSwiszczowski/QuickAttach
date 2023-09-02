using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using QuickAttach.Activation;
using QuickAttach.Contracts.Services;
using QuickAttach.ViewModels;

namespace QuickAttach.Services;

public class ActivationService : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly UIElement? _shell = null;

    public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, IEnumerable<IActivationHandler> activationHandlers)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;

        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        WeakReferenceMessenger.Default.Register<UpdateWindowSizeMessage>(this,
            (_, _) => dispatcherQueue.TryEnqueue(async () => UpdateWindowSize2()));
    }

    public async Task ActivateAsync(object activationArgs)
    {
        // Execute tasks before activation.
        await InitializeAsync();

        // Set the MainWindow Content.
        if (App.MainWindow.Content == null)
        {
            App.MainWindow.Content = _shell ?? new Frame();
        }

        var frame = App.MainWindow.Content as Frame;

        if (frame != null)
        {
            frame.SizeChanged += OnFrameSizeChanged;
        }

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);

        // Activate the MainWindow.
        App.MainWindow.Activate();

        // Execute tasks after activation.
        await StartupAsync();
    }

    private void OnFrameSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateWindowSize(App.MainWindow.Content);
        var frame = App.MainWindow.Content as Frame;

        if (frame != null)
        {
            frame.SizeChanged -= OnFrameSizeChanged;
        }
    }

    private async Task UpdateWindowSize2()
    {
        var dialog = new ContentDialog()
        {
            XamlRoot = (App.MainWindow.Content as Frame)?.XamlRoot,
            Title = "Warning",
            Content = $"Build failed. Project",
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            CloseButtonText = "OK"
            
        };

        App.MainWindow.Height = 300;

        await dialog.ShowAsync(ContentDialogPlacement.Popup);


        UpdateWindowSize(App.MainWindow.Content);
        
    }


    private void UpdateWindowSize(UIElement uiElement)
    {
        uiElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        uiElement.Arrange(new Rect(0, 0, uiElement.DesiredSize.Width, uiElement.DesiredSize.Height));

        App.MainWindow.Width = uiElement.DesiredSize.Width + 20;
        App.MainWindow.Height = uiElement.DesiredSize.Height + 35;

        //App.MainWindow.MinWidth = frame.DesiredSize.Width + 20;
        //App.MainWindow.MaxWidth = frame.DesiredSize.Width + 20;
        //App.MainWindow.MaxHeight = frame.DesiredSize.Height + 35;
        //App.MainWindow.MinHeight = frame.DesiredSize.Height + 35;
        ;
        
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }

        if (_defaultHandler.CanHandle(activationArgs))
        {
            await _defaultHandler.HandleAsync(activationArgs);
        }
    }

    private async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    private async Task StartupAsync()
    {
        await Task.CompletedTask;
    }
}
