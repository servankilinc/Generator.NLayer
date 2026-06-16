using GeneratorWPF.Services;
using GeneratorWPF.ViewModel._Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GeneratorWPF.View._Entity.Partials
{
    /// <summary>
    /// Interaction logic for EntityUpdateDialog.xaml
    /// </summary>
    public partial class EntityUpdateDialog : Window
    {
        public EntityUpdateDialog(INavigationService navigationService)
        {
            InitializeComponent();

            var viewModel = new EntityUpdateVM(navigationService);
            viewModel.CloseDialogAction = () => this.Close(); // Dialog'u kapatma işlemi
            this.DataContext = viewModel;
        }
    }
}
