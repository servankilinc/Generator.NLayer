using GeneratorWPF.Services;
using GeneratorWPF.ViewModel._Dto;
using GeneratorWPF.ViewModel._Dto.Partial;
using GeneratorWPF.ViewModel._Entity;
using Microsoft.EntityFrameworkCore.Metadata;
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

namespace GeneratorWPF.View._Dto.Partials
{
    /// <summary>
    /// Interaction logic for DtoCreateDialog.xaml
    /// </summary>
    public partial class DtoCreateDialog : Window
    {
        public DtoCreateDialog(INavigationService navigation)
        {
            InitializeComponent();

            var viewModel = new DtoCreateVM(navigation);
            viewModel.CloseDialogAction = () => this.Close(); // Dialog'u kapatma işlemi
            this.DataContext = viewModel;
        }
    }
}
