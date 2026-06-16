using GeneratorWPF.Utils;
using System.Windows.Input;
using GeneratorWPF.Models;
using GeneratorWPF.Repository;
using GeneratorWPF.View._Entity.Partials;
using System.Collections.ObjectModel;
using GeneratorWPF.Services;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using GeneratorWPF.Models.Enums;

namespace GeneratorWPF.ViewModel._Entity;

public class EntityDetailVM : BaseViewModel
{
    private INavigationService _navigation;
    private readonly EntityRepository _entityRepository;
    private readonly RelationRepository _relationRepository;
    private readonly FieldRepository _fieldRepository;
    public ICommand ShowRelationsDialogCommand { get; set; }
    public ICommand ShowRelationsUpdateDialogCommand { get; set; }
    public ICommand ShowFieldUpdateDialogCommand { get; set; }
    public ICommand ShowAddFieldCommand { get; set; }
    public ICommand ReturnEntityHomeCommand { get; set; }
    public ICommand RemoveFieldCommand { get; set; }
    public ICommand RemoveRelationCommand { get; set; }

    private Entity _entity;
    public Entity Entity { get => _entity; set { _entity = value; OnPropertyChanged(nameof(Entity)); } }
    public ObservableCollection<Field> Fields { get; set; } = new ObservableCollection<Field>();
    public ObservableCollection<RelationUIModel> RelationList { get; set; } = new ObservableCollection<RelationUIModel>();


    public EntityDetailVM(INavigationService navigationService)
    {
        _navigation = navigationService;
        _entityRepository = new EntityRepository();
        _relationRepository = new RelationRepository();
        _fieldRepository = new FieldRepository();

        Entity = _entityRepository.Get(filter: f => f.Id == StateStatics.EntityDetailId, include: i => i.Include(e => e.Fields.Where(f => f.FieldType.SourceTypeId == (int)FieldTypeSourceEnums.Base)).ThenInclude(f => f.FieldType));
        Fields = new ObservableCollection<Field>(Entity.Fields);

        var resultRelationList = _relationRepository.GetAll(
            filter: f => f.PrimaryField.EntityId == Entity.Id || f.ForeignField.EntityId == Entity.Id,
            include: i => i.Include(x => x.PrimaryField).ThenInclude(x => x.Entity).Include(x => x.ForeignField).ThenInclude(x => x.Entity).Include(x => x.RelationType).Include(x => x.DeleteBehaviorType));


        RelationList = new ObservableCollection<RelationUIModel>(resultRelationList.Select(x => new RelationUIModel
        {
            Id = x.Id,
            PrimaryRelationName = $"{x.PrimaryField.Entity.Name} => {x.PrimaryField.Name}",
            ForeignRelationName = $"{x.ForeignField.Entity.Name} => {x.ForeignField.Name}",
            RelationTypeName = x.RelationType.Name,
            DeleteBehaviorTypeName = x.DeleteBehaviorType.Name,
            PrimaryEntityVirPropName = x.PrimaryEntityVirPropName,
            ForeignEntityVirPropName= x.ForeignEntityVirPropName,
        }));

        ShowRelationsDialogCommand = new RellayCommand(obj =>
        {
            var dialog = new FieldRelationsDialog(navigationService);
            dialog.ShowDialog();
        });

        ShowRelationsUpdateDialogCommand = new RellayCommand(obj =>
        {
            StateStatics.RelationUpdateId = (int)obj;
            var dialog = new FieldRelationsUpdateDialog(navigationService);
            dialog.ShowDialog();
        });

        ShowFieldUpdateDialogCommand = new RellayCommand(obj =>
        {
            StateStatics.FieldUpdateId = (int)obj;
            var dialog = new FieldUpdateDialog(navigationService);
            dialog.ShowDialog();
        });

        ShowAddFieldCommand = new RellayCommand(obj =>
        {
            var dialog = new EntityAddFieldDialog(_navigation);
            dialog.ShowDialog();
        });

        RemoveFieldCommand = new RellayCommand(obj =>
        {
            if (obj != null)
            {
                var confirmation = MessageBox.Show("Are you sure to remove?", "Successful", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirmation == MessageBoxResult.No) return;

                _fieldRepository.DeleteByFilter(f => f.Id == (int)obj);

                Fields.Remove(Fields.First(f => f.Id == (int)obj));
            }
        });

        RemoveRelationCommand = new RellayCommand(obj =>
        {
            if (obj != null)
            {
                var existData = _relationRepository.Get(f => f.Id == ((RelationUIModel)obj).Id);
                if (existData == null) return;

                _relationRepository.Delete(existData);
                RelationList.Remove((RelationUIModel)obj);
            }
        });

        ReturnEntityHomeCommand = new RellayCommand(obj =>
        {
            _navigation.NavigateTo<EntityHomeVM>();
        });
    }


    public class RelationUIModel
    {
        public int Id { get; set; }
        public string PrimaryRelationName { get; set; }
        public string ForeignRelationName { get; set; }
        public string RelationTypeName { get; set; }
        public string DeleteBehaviorTypeName { get; set; }
        public string PrimaryEntityVirPropName { get; set; }
        public string ForeignEntityVirPropName { get; set; }
    }
}
