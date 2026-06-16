using GeneratorWPF.Dtos._Validation;
using GeneratorWPF.Models;
using GeneratorWPF.Repository;
using GeneratorWPF.Services;
using GeneratorWPF.Utils;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace GeneratorWPF.ViewModel._Dto
{
    public class DtoValidationUpdateVM : BaseViewModel
    {
        private INavigationService _navigation;
        public Action? CloseDialogAction { get; set; }
        private ValidationRepository _validationRepository { get; set; }
        private DtoFieldRepository _dtoFieldRepository { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ObservableCollection<ValidatorType> ValidatorTypeList { get; set; }
        public ObservableCollection<DtoField> DtoFields { get; set; }

        private ValidationUpdateDto _validationModel;
        public ValidationUpdateDto ValidationModel { get => _validationModel; set { _validationModel = value; OnPropertyChanged(nameof(ValidationModel)); } }

        public DtoValidationUpdateVM(INavigationService navigation)
        {
            _navigation = navigation;
            _validationRepository = new ValidationRepository();
            _dtoFieldRepository = new DtoFieldRepository();

            ValidatorTypeList = new ObservableCollection<ValidatorType>(_validationRepository.GetValidatorTypes());
            DtoFields = new ObservableCollection<DtoField>(_dtoFieldRepository.GetBySourceField(f => f.DtoId == StateStatics.DtoDetailId));
            
            var existData = _validationRepository.Get(
                f => f.Id == StateStatics.DtoDetailValidationId,
                include: i => i.Include(x => x.DtoField));

            ValidationModel = new ValidationUpdateDto(_validationRepository)
            {
                ValidationId = existData.Id,
                DtoFieldId = existData.DtoFieldId,
                OldValidatorTypeId = existData.ValidatorTypeId,
                ValidatorTypeId = existData.ValidatorTypeId,
                ErrorMessage = existData.ErrorMessage,
            };

            SaveCommand = new RellayCommand(obj =>
            {
                try
                {
                    if (
                        ValidationModel.ValidatorTypeId == default ||
                        ValidationModel.DtoFieldId == default
                    )
                    {
                        MessageBox.Show("Check The Fields!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _validationRepository.UpdateValidation(ValidationModel);

                    MessageBox.Show("Validation Created Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    CloseDialogAction?.Invoke();
                    _navigation.NavigateTo<DtoDetailVM>();
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
