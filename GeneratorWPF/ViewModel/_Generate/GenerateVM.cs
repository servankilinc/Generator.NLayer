using GeneratorWPF.CodeGenerators.NLayer;
using GeneratorWPF.Services;
using GeneratorWPF.Utils;
using System.Windows;
using System.Windows.Input;

namespace GeneratorWPF.ViewModel._Generate
{
    public class GenerateVM : BaseViewModel
    {
        private readonly NLayerGeneratorService _layerGeneratorService;
        private readonly INavigationService _navigationService;

        public ICommand CancelCommand { get; set; }
        public ICommand StartGenerationCommand { get; set; }

        private string _completeStatus = string.Empty;
        public string CompleteStatus { get => _completeStatus; set { _completeStatus = value; OnPropertyChanged(nameof(CompleteStatus)); } }

        private string _results = string.Empty;
        public string Results { get => _results; set { _results = value; OnPropertyChanged(nameof(Results)); } }

        private Visibility _isCancelVisible = Visibility.Visible;
        public Visibility IsCancelVisible { get => _isCancelVisible; set { _isCancelVisible = value; OnPropertyChanged(nameof(IsCancelVisible)); } }
        public Action? CloseDialogAction { get; set; }

        public GenerateVM(INavigationService navigationService)
        {
            _navigationService = navigationService;
            _layerGeneratorService = new NLayerGeneratorService();

            CancelCommand = new RellayCommand(obj =>
            {
                CloseDialogAction?.Invoke();
            });


            StartGenerationCommand = new RellayCommand(async obj =>
            {
                Results = "Generation Started.\n";
                IsCancelVisible = Visibility.Hidden;

                bool result = await Task.Run(() =>
                {
                    bool stepControl = true;
                    stepControl = _layerGeneratorService.GenerateSolution(AppendToResults);
                    if (!stepControl) return false;

                    stepControl = _layerGeneratorService.GenerateCoreLayer(AppendToResults);
                    if (!stepControl) return false;

                    stepControl = _layerGeneratorService.GenerateModelLayer(AppendToResults);
                    if (!stepControl) return false;

                    //stepControl = _layerGeneratorService.GenerateDataAccessLayer(AppendToResults);
                    //if (!stepControl) return false;

                    //stepControl = _layerGeneratorService.GenerateBusinessLayer(AppendToResults);
                    //if (!stepControl) return false;

                    //stepControl = _layerGeneratorService.GenerateAPILayer(AppendToResults);
                    //if (!stepControl) return false;

                    //stepControl = _layerGeneratorService.GenerateWebUIILayer(AppendToResults);
                    //if (!stepControl) return false;

                    return true;
                });

                if (!result)
                {
                    CompleteStatus = "Project Could Not Be Generated.";
                }
                else
                {
                    CompleteStatus = "Project Generated Successfully.";
                }

                IsCancelVisible = Visibility.Visible;
            });  
        }

        public void AppendToResults(string message)
        {
            Results += message + Environment.NewLine;
            Thread.Sleep(500);
        }
    }
}
