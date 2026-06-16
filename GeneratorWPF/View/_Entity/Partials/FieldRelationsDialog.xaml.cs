using GeneratorWPF.Services;
using GeneratorWPF.ViewModel._Entity;
using System.Windows;

namespace GeneratorWPF.View._Entity.Partials
{
    /// <summary>
    /// Interaction logic for FieldRelationsDialog.xaml
    /// </summary>
    public partial class FieldRelationsDialog : Window
    {
        public FieldRelationsDialog(INavigationService navigationService)
        {
            InitializeComponent();

            var viewModel = new FieldRelationsVM(navigationService);
            viewModel.CloseDialogAction = () => this.Close(); // Dialog'u kapatma işlemi
            this.DataContext = viewModel;
        }
    }
}
