using GeneratorWPF.Services;
using GeneratorWPF.ViewModel._Dto;
using System.Windows;

namespace GeneratorWPF.View._Dto.Partials
{
    /// <summary>
    /// Interaction logic for DtoValidationAddDialog.xaml
    /// </summary>
    public partial class DtoValidationAddDialog : Window
    {
        public DtoValidationAddDialog(INavigationService navigationService)
        {
            InitializeComponent();

            var viewModel = new DtoValidationAddVM(navigationService);
            viewModel.CloseDialogAction = () => this.Close(); // Dialog'u kapatma işlemi
            this.DataContext = viewModel;
        }
    }
}
