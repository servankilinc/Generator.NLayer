using GeneratorWPF.Context;
using GeneratorWPF.Models.LocalModels;
using GeneratorWPF.Services;
using GeneratorWPF.Utils;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace GeneratorWPF.ViewModel
{
    public class EntranceVM : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigationService;
        private readonly LocalContext localContext;
        public ICommand ContinueCommand { get; set; }
        public ICommand AddNewProjectCommand { get; set; }
        public ICommand DeleteProjectCommand { get; set; }

        private int _selectedProjectId = StateStatics.CurrentProject != null ? StateStatics.CurrentProject.Id : default;
        public int SelectedProjectId
        {
            get => _selectedProjectId;
            set
            {
                _selectedProjectId = value;
                OnPropertyChanged();
            }
        }

        private Project _projectToCreate = new Project();
        public Project ProjectToCreate
        {
            get => _projectToCreate;
            set
            {
                _projectToCreate = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Project> _projects;
        public ObservableCollection<Project> Projects
        {
            get => _projects;
            set
            {
                _projects = value;
                OnPropertyChanged();
            }
        }

        public EntranceVM(INavigationService navigationService)
        {
            _navigationService = navigationService;

            localContext = new LocalContext();
            Projects = new ObservableCollection<Project>(localContext.Projects);

            ContinueCommand = new RellayCommand(obj =>
            {
                try
                {
                    if (SelectedProjectId == default)
                    {
                        MessageBox.Show("Please Select a Project to Contiune!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    Project? selectedProject = localContext.Projects.FirstOrDefault(f => f.Id == SelectedProjectId);
                    if (selectedProject == default)
                    {
                        MessageBox.Show("There is an error, please select a project and try again!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    StateStatics.CurrentProject = selectedProject;

                    _navigationService.NavigateTo<HomeVM>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            AddNewProjectCommand = new RellayCommand(obj =>
            {
                try
                {
                    if (string.IsNullOrEmpty(ProjectToCreate.ProjectName))
                    {
                        MessageBox.Show("Check the fields!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    localContext.Projects.Add(new Project
                    {
                        ProjectName = ProjectToCreate.ProjectName,
                        CreateDate = DateTime.Now
                    });
                    localContext.SaveChanges();

                    MessageBox.Show("New Project Added Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    Projects = new ObservableCollection<Project>(localContext.Projects);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            DeleteProjectCommand = new RellayCommand(obj =>
            {
                try
                {
                    if (SelectedProjectId == default)
                    {
                        MessageBox.Show("You Did not Select Any Project!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var confirmation = MessageBox.Show("Are you sure to delete?", "Successful", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirmation == MessageBoxResult.No) return;

                    Project? selectedProject = localContext.Projects.FirstOrDefault(f => f.Id == SelectedProjectId);
                    if (selectedProject == default)
                    {
                        MessageBox.Show("There is an error, please select a project and try again!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    localContext.Projects.Remove(selectedProject);
                    localContext.SaveChanges();

                    MessageBox.Show("New Project Deleted Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    Projects = new ObservableCollection<Project>(localContext.Projects);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        public void Dispose()
        {
            if (localContext != null)
            {
                localContext.Dispose();
            }
        }
    }
}
