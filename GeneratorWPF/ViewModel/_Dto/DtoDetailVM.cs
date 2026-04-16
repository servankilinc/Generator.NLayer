using GeneratorWPF.Dtos._Dto;
using GeneratorWPF.Dtos._DtoField;
using GeneratorWPF.Dtos._Validation;
using GeneratorWPF.Models;
using GeneratorWPF.Repository;
using GeneratorWPF.Services;
using GeneratorWPF.Utils;
using GeneratorWPF.View._Dto.Partials;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace GeneratorWPF.ViewModel._Dto;

public class DtoDetailVM : BaseViewModel
{
    private INavigationService _navigation;
    private EntityRepository _entityRepository;
    private DtoRepository _dtoRepository;
    private DtoFieldRepository _dtoFieldRepository;
    private ValidationRepository _validationRepository;
    public ICommand ReturnHomeCommand { get; set; }
    public ICommand ShowAddDtoFieldCommand { get; set; }
    public ICommand ShowUpdateDtoFieldCommand { get; set; }
    public ICommand RemoveDtoFieldCommand { get; set; }
    public ICommand ShowCreateValidationCommand { get; set; }
    public ICommand ShowAddValidationCommand { get; set; }
    public ICommand ShowUpdateValidationCommand { get; set; }
    public ICommand RemoveValidationCommand { get; set; }

    private Dto _dto;
    public Dto Dto { get => _dto; set { _dto = value; OnPropertyChanged(nameof(Dto)); } }

    private DtoUpdateDto _dtoUpdateModel;
    public DtoUpdateDto DtoUpdateModel { get => _dtoUpdateModel; set { _dtoUpdateModel = value; OnPropertyChanged(nameof(DtoUpdateModel)); } }

    private ObservableCollection<DtoFieldResponseDto> _dtoFields;
    public ObservableCollection<DtoFieldResponseDto> DtoFields { get => _dtoFields; set { _dtoFields = value; OnPropertyChanged(nameof(DtoFields)); } }

    private ObservableCollection<ValidationResponse> _validations;
    public ObservableCollection<ValidationResponse> Validations { get => _validations; set { _validations = value; OnPropertyChanged(nameof(Validations)); } }
    private Visibility _btnCreateEnable = Visibility.Visible;
    public Visibility BtnCreateEnable { get => _btnCreateEnable; set { _btnCreateEnable = value; OnPropertyChanged(nameof(BtnCreateEnable)); } }
    private Visibility _btnAddValidationEnable = Visibility.Hidden;
    public Visibility BtnAddValidationEnable { get => _btnAddValidationEnable; set { _btnAddValidationEnable = value; OnPropertyChanged(nameof(BtnAddValidationEnable)); } }
    public DtoDetailVM(INavigationService navigation)
    {
        _navigation = navigation;
        _entityRepository = new EntityRepository();
        _dtoRepository = new DtoRepository();
        _dtoFieldRepository = new DtoFieldRepository();
        _validationRepository = new ValidationRepository();

        Dto = _dtoRepository.Get(f => f.Id == StateStatics.DtoDetailId);
        StateStatics.DtoDetailRelatedEntityId = Dto.RelatedEntityId;

        DtoFields = new ObservableCollection<DtoFieldResponseDto>(_dtoFieldRepository.GetDetailList(f => f.DtoId == StateStatics.DtoDetailId));
        Validations = new ObservableCollection<ValidationResponse>(_dtoFieldRepository.GetValidations(StateStatics.DtoDetailId));

        BtnCreateEnable = Validations.Count > 0 ? Visibility.Hidden : Visibility.Visible;
        BtnAddValidationEnable = Validations.Count > 0 ? Visibility.Visible : Visibility.Hidden;


        ShowAddDtoFieldCommand = new RellayCommand(obj =>
        {
            StateStatics.DtoDetailAddDtoFieldDtoId = StateStatics.DtoDetailId;
            var dialog = new DtoFieldAddDialog(_navigation);
            dialog.ShowDialog();
        });

        ShowUpdateDtoFieldCommand = new RellayCommand(obj =>
        {
            StateStatics.DtoDetailUpdateDtoFieldId = (int)obj;
            var dialog = new DtoFieldUpdateDialog(_navigation);
            dialog.ShowDialog();
        });

        RemoveDtoFieldCommand = new RellayCommand(dtoFieldId =>
        {
            try
            {
                _dtoFieldRepository.Delete((int)dtoFieldId);
                DtoFields = new ObservableCollection<DtoFieldResponseDto>(_dtoFieldRepository.GetDetailList(f => f.DtoId == StateStatics.DtoDetailId));

                MessageBox.Show("Field Deleted Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception)
            {
                MessageBox.Show("Field Could not be Deleted", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });


        ShowCreateValidationCommand = new RellayCommand(obj =>
        {
            var dialog = new DtoValidationCreateDialog(_navigation);
            dialog.ShowDialog();
        });


        ShowAddValidationCommand = new RellayCommand(obj =>
        {
            var dialog = new DtoValidationAddDialog(_navigation);
            dialog.ShowDialog();
        });


        ShowUpdateValidationCommand = new RellayCommand(validationId =>
        {
            StateStatics.DtoDetailValidationId = (int)validationId;
            var dialog = new DtoValidationUpdateDialog(_navigation);
            dialog.ShowDialog();
        });


        RemoveValidationCommand = new RellayCommand(validationId =>
        {
            try
            {
                if (validationId != null)
                {
                    var confirmation = MessageBox.Show("Are you sure to remove?", "Successful", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirmation == MessageBoxResult.No) return;

                    _validationRepository.DeleteByFilter(f => f.Id == (int)validationId);
                    DtoFields = new ObservableCollection<DtoFieldResponseDto>(_dtoFieldRepository.GetDetailList(f => f.DtoId == StateStatics.DtoDetailId));

                    MessageBox.Show("Validation Removed Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Validation Could not be Removed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });

        ReturnHomeCommand = new RellayCommand(obj =>
        {
            _navigation.NavigateTo<DtoHomeVM>();
        });
    }
}
