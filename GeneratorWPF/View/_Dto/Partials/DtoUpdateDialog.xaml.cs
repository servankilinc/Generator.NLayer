using GeneratorWPF.Services;
using GeneratorWPF.ViewModel._Dto;
using System.Windows;

namespace GeneratorWPF.View._Dto.Partials
{
    /// <summary>
    /// Interaction logic for DtoUpdateDialog.xaml
    /// </summary>
    public partial class DtoUpdateDialog : Window
    {
        public DtoUpdateDialog(INavigationService navigationService)
        {
            InitializeComponent();

            var viewModel = new DtoUpdateVM(navigationService);
            viewModel.CloseDialogAction = () => this.Close(); // Dialog'u kapatma işlemi
            this.DataContext = viewModel;
        }
    }
}
