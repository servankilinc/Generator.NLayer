using GeneratorWPF.Services;
using GeneratorWPF.Utils;
using GeneratorWPF.ViewModel._Dto;
using GeneratorWPF.ViewModel._Entity;
using System.Windows;
using System.Windows.Input;

namespace GeneratorWPF.ViewModel
{
    public class MainWindowVM : BaseViewModel
    {
        private INavigationService _navigation;
        public INavigationService Navigation { get { return _navigation; } set { _navigation = value; OnPropertyChanged(); } }

        public ICommand ToHomeCommand { get; set; }
        public ICommand ToEntityListCommand { get; set; }
        public ICommand ToDtoListCommand { get; set; }
        public ICommand ToProjectsCommand { get; set; }

        public MainWindowVM(INavigationService navigation)
        {
            Navigation = navigation;

            ToHomeCommand = new RellayCommand(obj =>
            {
                if (StateStatics.CurrentProject == default)
                {
                    MessageBox.Show("Please Firstly Select a Project!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Navigation.NavigateTo<HomeVM>();
            });

            ToEntityListCommand = new RellayCommand(obj =>
            {
                if (StateStatics.CurrentProject == default)
                {
                    MessageBox.Show("Please Firstly Select a Project!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Navigation.NavigateTo<EntityHomeVM>();
            });

            ToDtoListCommand = new RellayCommand(obj =>
            {
                if (StateStatics.CurrentProject == default)
                {
                    MessageBox.Show("Please Firstly Select a Project!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Navigation.NavigateTo<DtoHomeVM>();
            });

            ToProjectsCommand = new RellayCommand(obj =>
            {
                Navigation.NavigateTo<EntranceVM>();
            });

            Navigation.NavigateTo<EntranceVM>();
        }
    }
}
