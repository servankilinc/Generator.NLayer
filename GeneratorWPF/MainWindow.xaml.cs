using GeneratorWPF.ViewModel;
using System.Windows;

namespace GeneratorWPF
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowVM mainWindowVM)
        {
            InitializeComponent();
            DataContext = mainWindowVM;
        }
    }
}