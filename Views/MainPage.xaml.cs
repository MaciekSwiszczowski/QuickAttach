using Microsoft.UI.Xaml.Controls;

using QuickAttach.ViewModels;

namespace QuickAttach.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }
}
