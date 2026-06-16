using GeneratorWPF.Utils;
using System.Collections.ObjectModel;
using System.Windows.Input;
using GeneratorWPF.Models;
using GeneratorWPF.Repository;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using GeneratorWPF.Dtos._Relation;
using GeneratorWPF.Services;
using GeneratorWPF.Models.Enums;

namespace GeneratorWPF.ViewModel._Entity;

public class FieldRelationsVM : BaseViewModel
{
    private INavigationService _navigation;
    private readonly EntityRepository _entityRepository;
    private readonly RelationRepository _relationRepository;
    private readonly DeleteBehaviorTypeRepository _deleteBehaviorTypeRepository;
    public ICommand SaveCommand { get; set; }
    public ICommand CancelCommand { get; set; }
    public Action? CloseDialogAction { get; set; } // Dialog'u kapatmak için callback

    public RelationCreateDto RelationCreateModel { get; set; } = new RelationCreateDto();
    public ObservableCollection<Entity> EntityList { get; set; } = new ObservableCollection<Entity>();
    public ObservableCollection<RelationType> RelationTypeList { get; set; } = new ObservableCollection<RelationType>();
    public ObservableCollection<DeleteBehaviorType> DeleteBehaviorTypeList { get; set; } = new ObservableCollection<DeleteBehaviorType>();

    public DeleteBehaviorType? SetNullBehavior { get; set; }
    
    private int _foreignFieldId;
    public int ForeignFieldId
    {
        get => _foreignFieldId;
        set
        {
            _foreignFieldId = value;
            RelationCreateModel.ForeignFieldId = value;

            //RelationCreateModel.ForeignEntityVirPropName = ForeignEntity.Fields.First(f => f.Id == value).Entity?.Name;

            if (DeleteBehaviorTypeList == null) return;

            // Check to foreign field if required set deisable SetNull delete behavior
            if(ForeignEntity.Fields != null && ForeignEntity.Fields.First(f => f.Id == value).IsRequired)
            {
                if (DeleteBehaviorTypeList.Any(f => f.Id == (int)DeleteBehaviorTypeEnums.SetNull))
                {
                    SetNullBehavior = DeleteBehaviorTypeList.First(f => f.Id == (int)DeleteBehaviorTypeEnums.SetNull);
                    DeleteBehaviorTypeList.Remove(SetNullBehavior);
                }
            }
            else 
            {
                if (!DeleteBehaviorTypeList.Any(f => f.Id == (int)DeleteBehaviorTypeEnums.SetNull) && SetNullBehavior != null)
                {
                    DeleteBehaviorTypeList.Add(SetNullBehavior);
                }
            }
        }
    }

    private Entity _primaryEntity;
    public Entity PrimaryEntity { get => _primaryEntity; set { _primaryEntity = value; OnPropertyChanged(nameof(PrimaryEntity)); } }

    private Entity _foreignEntity;
    public Entity ForeignEntity { get => _foreignEntity; set { _foreignEntity = value; OnPropertyChanged(nameof(ForeignEntity)); } }


    public FieldRelationsVM(INavigationService navigationService)
    {
        _navigation = navigationService;
        _entityRepository = new EntityRepository();
        _relationRepository = new RelationRepository();
        _deleteBehaviorTypeRepository = new DeleteBehaviorTypeRepository();

        EntityList = new ObservableCollection<Entity>(_entityRepository.GetAll(include: i => i.Include(x => x.Fields.Where(f => f.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Base)).ThenInclude(ti => ti.FieldType)));
        RelationTypeList = new ObservableCollection<RelationType>(_relationRepository.GetRelationTypes());
        DeleteBehaviorTypeList = new ObservableCollection<DeleteBehaviorType>(_deleteBehaviorTypeRepository.GetAll());

        ForeignEntity = EntityList.First(f => f.Id == StateStatics.EntityDetailId);

        SaveCommand = new RellayCommand(obj =>
        {
            try
            {
                if (RelationCreateModel.PrimaryFieldId == default || RelationCreateModel.ForeignFieldId == default || RelationCreateModel.RelationTypeId == default || RelationCreateModel.DeleteBehaviorTypeId == default || string.IsNullOrEmpty(RelationCreateModel.PrimaryEntityVirPropName) || string.IsNullOrEmpty(RelationCreateModel.ForeignEntityVirPropName))
                {
                    MessageBox.Show("Check The Fields!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _relationRepository.AddRelation(RelationCreateModel);

                MessageBox.Show("Relation Created Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                CloseDialogAction?.Invoke(); // Dialog'u kapat
                _navigation.NavigateTo<EntityDetailVM>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });

        CancelCommand = new RellayCommand(obj =>
        {
            CloseDialogAction?.Invoke(); // Dialog'u kapat
            _navigation.NavigateTo<EntityDetailVM>();
        });
    }
}
