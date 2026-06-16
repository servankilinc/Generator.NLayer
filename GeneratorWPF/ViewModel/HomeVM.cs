using GeneratorWPF.Models;
using GeneratorWPF.Repository;
using GeneratorWPF.Services;
using GeneratorWPF.Utils;
using GeneratorWPF.View._Generate;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace GeneratorWPF.ViewModel
{
    public class HomeVM : BaseViewModel
    {
        private readonly AppSettingsRepository _appSettingsRepository;
        private readonly EntityRepository _entityRepository;
        private readonly INavigationService _navigationService;
        public ICommand SaveAppSettingsCommand { get; set; }
        public ICommand ShowGenerateCommand { get; set; }
        public ICommand BrowseFileCommand { get; set; }

        public string Title { get; set; } = "Welcome to Genereator Desktop App";

        private ObservableCollection<Entity> _entityList;
        public ObservableCollection<Entity> EntityList
        {
            get => _entityList;
            set
            {
                _entityList = value;
                OnPropertyChanged();
            }
        }
        public AppSetting AppSettingModel { get; set; }

        private string _folderPath;
        public string FolderPath 
        { 
            get => _folderPath; 
            set { 
                _folderPath = value; 
                OnPropertyChanged(nameof(FolderPath));

                AppSettingModel.Path = value;
            } 
        }

        private bool _isThereUser;
        public bool IsThereUser
        {
            get => _isThereUser;
            set
            {
                _isThereUser = value;
                OnPropertyChanged();

                IsUserSelectVisible = value ? Visibility.Visible : Visibility.Hidden;
                AppSettingModel.IsThereUser = value;
            }
        }
        private bool _isThereRole;
        public bool IsThereRole
        {
            get => _isThereRole;
            set
            {
                _isThereRole = value;
                OnPropertyChanged();

                IsRoleSelectVisible = value ? Visibility.Visible : Visibility.Hidden;
                AppSettingModel.IsThereRole = value;
            }
        }

        private Visibility _isUserSelectVisible;
        public Visibility IsUserSelectVisible { get => _isUserSelectVisible; set { _isUserSelectVisible = value; OnPropertyChanged(nameof(IsUserSelectVisible)); } }

        private Visibility _isRoleSelectVisible;
        public Visibility IsRoleSelectVisible { get => _isRoleSelectVisible; set { _isRoleSelectVisible = value; OnPropertyChanged(nameof(IsRoleSelectVisible)); } }


        public HomeVM(INavigationService navigationService) // 
        {
            _navigationService = navigationService;
            _appSettingsRepository = new AppSettingsRepository();
            _entityRepository = new EntityRepository();

            EntityList = new ObservableCollection<Entity>(_entityRepository.GetAll());

            AppSettingModel = _appSettingsRepository.Get(filter: f => f.Id == 1);
            
            if (!string.IsNullOrEmpty(AppSettingModel.Path)) FolderPath = AppSettingModel.Path;
            IsThereUser = AppSettingModel.IsThereUser;
            IsThereRole = AppSettingModel.IsThereRole;

            IsUserSelectVisible = AppSettingModel.IsThereUser ? Visibility.Visible : Visibility.Hidden;
            IsRoleSelectVisible = AppSettingModel.IsThereRole ? Visibility.Visible : Visibility.Hidden;


            SaveAppSettingsCommand = new RellayCommand(obj =>
            {
                try
                {
                    if ((AppSettingModel.IsThereUser && AppSettingModel.UserEntityId == default) || (AppSettingModel.IsThereRole && AppSettingModel.RoleEntityId == default))
                    {
                        MessageBox.Show("Check The Fields!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _appSettingsRepository.Update(AppSettingModel);

                    MessageBox.Show("App Settings Updated Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    _navigationService.NavigateTo<HomeVM>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            ShowGenerateCommand = new RellayCommand(obj =>
            {
                var dialog = new GenerateDialog(_navigationService);

                dialog.ShowDialog();
            });


            BrowseFileCommand = new RellayCommand(obj =>
            {
                var dialogService = new FileDialogService();
                var selectedPath = dialogService.OpenFileDialog();
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    FolderPath = selectedPath;
                }
            });
        }
    }

    public class FileDialogService
    {
        public string OpenFileDialog()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Klasör Seçin";
                dialog.ShowNewFolderButton = true;
                DialogResult result = dialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    return dialog.SelectedPath;
                }

                return null;
            }
        }
    }
}
