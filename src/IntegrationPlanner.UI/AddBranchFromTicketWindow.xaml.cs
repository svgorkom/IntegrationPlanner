using System.Windows;
using IntegrationPlanner.UI.ViewModels;

namespace IntegrationPlanner.UI;

public partial class AddBranchFromTicketWindow : Window
{
    public AddBranchFromTicketWindow(AddBranchFromTicketViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
        viewModel.CloseRequested += (_, accepted) => DialogResult = accepted;
    }

    public AddBranchFromTicketViewModel ViewModel { get; }
}
