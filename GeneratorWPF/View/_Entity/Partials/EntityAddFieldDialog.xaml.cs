using GeneratorWPF.Services;
using GeneratorWPF.ViewModel._Entity;
using System.Windows;

namespace GeneratorWPF.View._Entity.Partials
{
    /// <summary>
    /// Interaction logic for EntityAddFieldDialog.xaml
    /// </summary>
    public partial class EntityAddFieldDialog : Window
    {
        public EntityAddFieldDialog(INavigationService navigationService)
        {
            InitializeComponent();

            var viewModel = new EntityAddFieldVM(navigationService);
            viewModel.CloseDialogAction = () => this.Close(); // Dialog'u kapatma işlemi
            this.DataContext = viewModel;
        }
    }
}
