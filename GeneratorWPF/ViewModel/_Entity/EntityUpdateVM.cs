using GeneratorWPF.Utils;
using System.Windows.Input;
using GeneratorWPF.Repository;
using System.Windows;
using GeneratorWPF.Dtos._Entity;
using GeneratorWPF.Services;
using System.Collections.ObjectModel;
using GeneratorWPF.Models;
using GeneratorWPF.Models.Enums;

namespace GeneratorWPF.ViewModel._Entity;

public class EntityUpdateVM : BaseViewModel
{
    private INavigationService _navigation;
    private EntityRepository _entityRepository { get; set; } 
    private DtoRepository _dtoRepository { get; set; } 
    public ICommand SaveCommand { get; set; }  
    public ICommand CancelCommand { get; set; }
     
    public ObservableCollection<Dto> ReadDtoList { get; set; } = new ObservableCollection<Dto>();
    public ObservableCollection<Dto> CreateDtoList { get; set; } = new ObservableCollection<Dto>();
    public ObservableCollection<Dto> UpdateDtoList { get; set; } = new ObservableCollection<Dto>();
    public ObservableCollection<Dto> DeleteDtoList { get; set; } = new ObservableCollection<Dto>();

    private EntityUpdateDto _entityUpdateModel = new EntityUpdateDto();
    public EntityUpdateDto EntityUpdateModel { get => _entityUpdateModel; set { _entityUpdateModel = value; OnPropertyChanged(nameof(EntityUpdateModel)); } }
    public Action? CloseDialogAction { get; set; } // Dialog'u kapatmak için callback

    public EntityUpdateVM(INavigationService navigation)
    {
        _navigation = navigation;
        _entityRepository = new EntityRepository();
        _dtoRepository = new DtoRepository();

        ReadDtoList = new ObservableCollection<Dto>(_dtoRepository.GetAll(f => f.RelatedEntityId == StateStatics.EntityUpdateId && f.CrudTypeId == (int)CrudTypeEnums.Read));
        CreateDtoList = new ObservableCollection<Dto>(_dtoRepository.GetAll(f => f.RelatedEntityId == StateStatics.EntityUpdateId && f.CrudTypeId == (int)CrudTypeEnums.Create));
        UpdateDtoList = new ObservableCollection<Dto>(_dtoRepository.GetAll(f => f.RelatedEntityId == StateStatics.EntityUpdateId && f.CrudTypeId == (int)CrudTypeEnums.Update));
        DeleteDtoList = new ObservableCollection<Dto>(_dtoRepository.GetAll(f => f.RelatedEntityId == StateStatics.EntityUpdateId && f.CrudTypeId == (int)CrudTypeEnums.Delete));

        var entity = _entityRepository.Get(f => f.Id == StateStatics.EntityUpdateId);
        EntityUpdateModel = new EntityUpdateDto
        {
            Id = entity.Id,
            Name = entity.Name,
            TableName = entity.TableName,
            SoftDeletable = entity.SoftDeletable,
            Auditable = entity.Auditable,
            Archivable = entity.Archivable,
            CreateDtoId = entity.CreateDtoId,
            UpdateDtoId = entity.UpdateDtoId,
            DeleteDtoId = entity.DeleteDtoId,
            ReportDtoId = entity.ReportDtoId,
            BasicResponseDtoId = entity.BasicResponseDtoId,
            DetailResponseDtoId = entity.DetailResponseDtoId
        };

        SaveCommand = new RellayCommand(obj =>
        {
            try
            {
                if (string.IsNullOrEmpty(EntityUpdateModel.Name) || string.IsNullOrEmpty(EntityUpdateModel.TableName))
                {
                    MessageBox.Show("Check The Fields!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                } 

                _entityRepository.Update(EntityUpdateModel);

                MessageBox.Show("Entity Updated Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                CloseDialogAction?.Invoke(); // Dialog'u kapat
                _navigation.NavigateTo<EntityHomeVM>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });


        CancelCommand = new RellayCommand(obj =>
        {
            CloseDialogAction?.Invoke(); // Dialog'u kapat
        });
    }
}
