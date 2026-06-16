using GeneratorWPF.Services;
using GeneratorWPF.ViewModel._Entity;
using System.Windows;

namespace GeneratorWPF.View._Entity.Partials
{
    /// <summary>
    /// Interaction logic for EntityCreateDialog.xaml
    /// </summary>
    public partial class EntityCreateDialog : Window
    {
        public EntityCreateDialog(INavigationService navigationService)
        {
            InitializeComponent();

            var viewModel = new EntityCreateVM(navigationService);
            viewModel.CloseDialogAction = () => this.Close(); // Dialog'u kapatma işlemi
            this.DataContext = viewModel;
        }
    }
}
