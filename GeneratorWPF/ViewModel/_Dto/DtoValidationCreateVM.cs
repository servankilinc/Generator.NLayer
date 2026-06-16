using GeneratorWPF.Utils;
using System.Windows.Input;
using GeneratorWPF.Repository;
using System.Windows;
using GeneratorWPF.Services;
using GeneratorWPF.Models;
using System.Collections.ObjectModel;
using GeneratorWPF.Dtos._Validation;
using GeneratorWPF.Dtos._DtoField;
using Microsoft.IdentityModel.Tokens;

namespace GeneratorWPF.ViewModel._Dto;

public class DtoValidationCreateVM : BaseViewModel
{
    private INavigationService _navigation;
    public Action? CloseDialogAction { get; set; }
    private ValidationRepository _validationRepository { get; set; }
    private DtoFieldRepository _dtoFieldRepository { get; set; }
    public ICommand AddNewValidationCommand { get; set; }
    public ICommand SaveCommand { get; set; }
    public ICommand RemoveValidationCommand { get; set; } 
    public ICommand CancelCommand { get; set; }
    public ObservableCollection<ValidatorType> ValidatorTypeList { get; set; }
    public ObservableCollection<DtoField> DtoFields { get; set; }
    
    private ObservableCollection<ValidationCreateDto> _validationModels = new ObservableCollection<ValidationCreateDto>();
    public ObservableCollection<ValidationCreateDto> ValidationModels { get => _validationModels; set { _validationModels = value; OnPropertyChanged(nameof(ValidationModels)); }}

    public DtoValidationCreateVM(INavigationService navigation)
    {
        _navigation = navigation;
        _validationRepository = new ValidationRepository();
        _dtoFieldRepository = new DtoFieldRepository();

        ValidatorTypeList= new ObservableCollection<ValidatorType>(_validationRepository.GetValidatorTypes());
        DtoFields = new ObservableCollection<DtoField>(_dtoFieldRepository.GetBySourceField(f => f.DtoId == StateStatics.DtoDetailId));

    
        AddNewValidationCommand = new RellayCommand(obj =>
        {
            ValidationModels.Add(new ValidationCreateDto(_validationRepository));
        });


        SaveCommand = new RellayCommand(obj =>
        {
            try
            {
                if (
                    ValidationModels.Any(f => f.ValidatorTypeId == default) || 
                    ValidationModels.Any(f => f.DtoFieldId == default) ||
                    ValidationModels.Any(f => f.ValidationParams == null || f.ValidationParams.Any(fi => string.IsNullOrEmpty(fi.Value)))
                ) {
                    MessageBox.Show("Check The Fields!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _validationRepository.AddValidationList(ValidationModels.ToList());

                MessageBox.Show("Validations Created Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                CloseDialogAction?.Invoke();
                _navigation.NavigateTo<DtoDetailVM>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });


        RemoveValidationCommand = new RellayCommand(validationCreateModel =>
        {
            try
            {
                ValidationModels.Remove((ValidationCreateDto)validationCreateModel);
                DtoFields = new ObservableCollection<DtoField>(_dtoFieldRepository.GetBySourceField(f => f.DtoId == StateStatics.DtoDetailId));

                MessageBox.Show("Validation removed Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception)
            {
                MessageBox.Show("Validation Could not be Removed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }); 


        CancelCommand = new RellayCommand(obj =>
        {
            CloseDialogAction?.Invoke();
        });
    }
}