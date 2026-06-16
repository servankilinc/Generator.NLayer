using GeneratorWPF.Services;
using GeneratorWPF.ViewModel._Entity;
using System.Windows;

namespace GeneratorWPF.View._Entity.Partials
{
    /// <summary>
    /// Interaction logic for FieldRelationsUpdateDialog.xaml
    /// </summary>
    public partial class FieldRelationsUpdateDialog : Window
    {
        public FieldRelationsUpdateDialog(INavigationService navigationService)
        {
            InitializeComponent();

            var viewModel = new FieldRelationsUpdateVM(navigationService);
            viewModel.CloseDialogAction = () => this.Close(); // Dialog'u kapatma işlemi
            this.DataContext = viewModel;
        }
    }
}
