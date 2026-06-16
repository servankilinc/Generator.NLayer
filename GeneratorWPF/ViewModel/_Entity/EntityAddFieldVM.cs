using GeneratorWPF.Dtos._Field;
using GeneratorWPF.Models;
using GeneratorWPF.Models.Enums;
using GeneratorWPF.Repository;
using GeneratorWPF.Services;
using GeneratorWPF.Utils;
using GeneratorWPF.ViewModel._Dto;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace GeneratorWPF.ViewModel._Entity
{
    public class EntityAddFieldVM: BaseViewModel
    {
        private INavigationService _navigation;
        private FieldRepository _fieldRepository { get; set; }
        private FieldTypeRepository _fieldTypeRepository { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ObservableCollection<FieldType> FieldTypeList { get; set; }

        private FieldCreateDto _fieldModel;
        public FieldCreateDto FieldModel { get => _fieldModel; set { _fieldModel = value; OnPropertyChanged(nameof(FieldModel)); } }
        public Action? CloseDialogAction { get; set; }

        public EntityAddFieldVM(INavigationService navigation)
        {
            _navigation = navigation;
            _fieldRepository = new FieldRepository();
            _fieldTypeRepository = new FieldTypeRepository();

            FieldTypeList = new ObservableCollection<FieldType>(_fieldTypeRepository.GetAll(filter: f => f.SourceTypeId == (byte)FieldTypeSourceEnums.Base));

            FieldModel = new FieldCreateDto
            {
                EntityId = StateStatics.EntityDetailId,
                IsList = false,
                IsUnique = false,
                IsRequired = false,
                Filterable = false,
                Name = "New Field",
            };

            SaveCommand = new RellayCommand(obj =>
            {
                try
                {
                    if (string.IsNullOrEmpty(FieldModel.Name) || FieldModel.EntityId == default || FieldModel.FieldTypeId == default)
                    {
                        MessageBox.Show("Check The Fields!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _fieldRepository.Add(FieldModel);

                    MessageBox.Show("Field Added Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);

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
            });
        }
    }
}
