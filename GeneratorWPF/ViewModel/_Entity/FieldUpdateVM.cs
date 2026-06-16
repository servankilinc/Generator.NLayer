using GeneratorWPF.Dtos._Field;
using GeneratorWPF.Models;
using GeneratorWPF.Models.Enums;
using GeneratorWPF.Repository;
using GeneratorWPF.Services;
using GeneratorWPF.Utils;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace GeneratorWPF.ViewModel._Entity
{
    class FieldUpdateVM: BaseViewModel
    {
        private INavigationService _navigation;

        private readonly FieldTypeRepository _fieldTypeRepository;
        private readonly FieldRepository _fieldRepository;
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ObservableCollection<FieldType> FieldTypeList { get; set; } = new ObservableCollection<FieldType>();

        private FieldUpdateDto _fieldUpdateModel;
        public FieldUpdateDto FieldUpdateModel { get => _fieldUpdateModel; set { _fieldUpdateModel = value; OnPropertyChanged(nameof(FieldUpdateModel)); }}
        public Action? CloseDialogAction { get; set; }

        public FieldUpdateVM(INavigationService navigationService)
        {
            _navigation = navigationService;
            _fieldTypeRepository = new FieldTypeRepository();
            _fieldRepository = new FieldRepository();

            FieldTypeList = new ObservableCollection<FieldType>(_fieldTypeRepository.GetAll(f => f.SourceTypeId == (int)FieldTypeSourceEnums.Base));
            
            var field = _fieldRepository.Get(f => f.Id == StateStatics.FieldUpdateId);
            FieldUpdateModel = new FieldUpdateDto
            {
                Id = field.Id,
                FieldTypeId = field.FieldTypeId,
                Name = field.Name,
                IsRequired = field.IsRequired,
                IsUnique = field.IsUnique,
                IsList = field.IsList,
                Filterable = field.Filterable
            };
            
            SaveCommand = new RellayCommand(obj =>
            {
                try
                {
                    if (FieldUpdateModel.Name == default || FieldUpdateModel.FieldTypeId == default)
                    {
                        MessageBox.Show("Check The Fields!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _fieldRepository.Update(FieldUpdateModel);

                    MessageBox.Show("Field Updated Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    CloseDialogAction?.Invoke();
                    _navigation.NavigateTo<EntityDetailVM>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            CancelCommand = new RellayCommand(obj =>
            {
                CloseDialogAction?.Invoke();
                _navigation.NavigateTo<EntityDetailVM>();
            });
        }
    }
}
