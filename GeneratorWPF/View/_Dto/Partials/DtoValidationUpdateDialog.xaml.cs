using GeneratorWPF.Services;
using GeneratorWPF.ViewModel._Dto;
using System.Windows;

namespace GeneratorWPF.View._Dto.Partials
{
    /// <summary>
    /// Interaction logic for DtoValidationUpdateDialog.xaml
    /// </summary>
    public partial class DtoValidationUpdateDialog : Window
    {
        public DtoValidationUpdateDialog(INavigationService navigationService)
        {
            InitializeComponent();

            var viewModel = new DtoValidationUpdateVM(navigationService);
            viewModel.CloseDialogAction = () => this.Close(); // Dialog'u kapatma işlemi
            this.DataContext = viewModel;
        }
    }
}
