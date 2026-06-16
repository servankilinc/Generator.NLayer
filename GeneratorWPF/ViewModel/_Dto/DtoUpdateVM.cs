using GeneratorWPF.Dtos._Dto;
using GeneratorWPF.Models;
using GeneratorWPF.Repository;
using GeneratorWPF.Services;
using GeneratorWPF.Utils;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace GeneratorWPF.ViewModel._Dto;

public class DtoUpdateVM : BaseViewModel
{
    private readonly INavigationService _navigation;
    private EntityRepository _entityRepository { get; set; }
    private DtoRepository _dtoRepository { get; set; }
    private CrudTypeRepository _crudTypeRepository { get; set; }

    public ICommand SaveCommand { get; set; }
    public ICommand CancelCommand { get; set; }
    public Action? CloseDialogAction { get; set; }
    
    private ObservableCollection<Entity> _entityList;
    public ObservableCollection<Entity> EntityList { get => _entityList; set { _entityList = value; OnPropertyChanged(nameof(EntityList)); } }

    private ObservableCollection<CrudType> _crudTypes;
    public ObservableCollection<CrudType> CrudTypes { get => _crudTypes; set { _crudTypes = value; OnPropertyChanged(nameof(CrudTypes)); } }

    public DtoUpdateDto DtoUpdateModel { get; set; } = new DtoUpdateDto();

    public DtoUpdateVM(INavigationService navigationService)
    {
        _navigation = navigationService;
        _entityRepository = new EntityRepository();
        _dtoRepository = new DtoRepository();
        _crudTypeRepository = new CrudTypeRepository();

        EntityList = new ObservableCollection<Entity>(_entityRepository.GetAll());
        CrudTypes = new ObservableCollection<CrudType>(_crudTypeRepository.GetAll());

        var dto = _dtoRepository.Get(f => f.Id == StateStatics.DtoUpdateId);

        DtoUpdateModel = new DtoUpdateDto
        {
            Id = dto.Id,
            Name = dto.Name,
            RelatedEntityId = dto.RelatedEntityId,
            CrudTypeId = dto.CrudTypeId,
        };

        SaveCommand = new RellayCommand(obj =>
        {
            try
            {
                if (string.IsNullOrEmpty(DtoUpdateModel.Name) || DtoUpdateModel.RelatedEntityId == default || DtoUpdateModel.CrudTypeId == default || DtoUpdateModel.Id == default)
                {
                    MessageBox.Show("Check The Fields!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                 
                _dtoRepository.Update(DtoUpdateModel);

                MessageBox.Show("Dto Updated Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                CloseDialogAction?.Invoke(); // Dialog'u kapat

                _navigation.NavigateTo<DtoHomeVM>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });


        CancelCommand = new RellayCommand(field =>
        {
            CloseDialogAction?.Invoke();
        });
    }
}
