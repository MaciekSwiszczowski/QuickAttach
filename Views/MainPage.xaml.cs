using QuickAttach.ViewModels;

namespace QuickAttach.Views;

public sealed partial class MainPage
{
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();

        Loaded += (_, _) => ViewModel.Root = Root.XamlRoot;
    }
}
