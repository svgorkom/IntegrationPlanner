using System.Windows;
using IntegrationPlanner.UI.ViewModels;

namespace IntegrationPlanner.UI;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
    }
}
