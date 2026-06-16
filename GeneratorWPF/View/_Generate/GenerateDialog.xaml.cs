using GeneratorWPF.Services;
using GeneratorWPF.ViewModel._Entity;
using GeneratorWPF.ViewModel._Generate;
using System.Windows;

namespace GeneratorWPF.View._Generate
{
    /// <summary>
    /// Interaction logic for GenerateDialog.xaml
    /// </summary>
    public partial class GenerateDialog : Window
    {
        public GenerateDialog(INavigationService navigationService)
        {
            InitializeComponent();

            var viewModel = new GenerateVM(navigationService);
            viewModel.CloseDialogAction = () => this.Close(); // Dialog'u kapatma işlemi
            this.DataContext = viewModel;
        }
    }
}
