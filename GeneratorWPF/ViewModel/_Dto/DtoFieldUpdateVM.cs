using GeneratorWPF.Dtos._DtoField;
using GeneratorWPF.Dtos._Relation;
using GeneratorWPF.Models;
using GeneratorWPF.Repository;
using GeneratorWPF.Services;
using GeneratorWPF.Utils;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace GeneratorWPF.ViewModel._Dto;

public class DtoFieldUpdateVM : BaseViewModel
{
    private INavigationService _navigation;
    private FieldRepository _fieldRepository { get; set; }
    private DtoFieldRepository _dtoFieldRepository { get; set; }
    private EntityRepository _entityRepository { get; set; }
    private RelationRepository _relationRepository { get; set; }
    
    public ICommand SaveCommand { get; set; }
    public ICommand AddRelationCommand { get; set; }
    public ICommand RemoveRelationCommand { get; set; }
    public ICommand CancelCommand { get; set; }

    public ObservableCollection<Entity> EntityList { get; set; }

    private DtoFieldUpdateDto _dtoFieldUpdateDto;
    public DtoFieldUpdateDto DtoFieldUpdateDto { get => _dtoFieldUpdateDto; set { _dtoFieldUpdateDto = value; OnPropertyChanged(nameof(DtoFieldUpdateDto)); } }
    public Action? CloseDialogAction { get; set; } // Dialog'u kapatmak için callback

    public DtoFieldUpdateVM(INavigationService navigation)
    {
        _navigation = navigation;
        _fieldRepository = new FieldRepository();
        _dtoFieldRepository = new DtoFieldRepository();
        _entityRepository = new EntityRepository();
        _relationRepository = new RelationRepository();

        EntityList = new ObservableCollection<Entity>(_entityRepository.GetAll());

        var dtoField = _dtoFieldRepository.Get(f => f.Id == StateStatics.DtoDetailUpdateDtoFieldId, include: i => i.Include(x => x.SourceField).Include(x => x.Dto));
        var dtoFiedRelations = _dtoFieldRepository.GetDtoFieldRelations(StateStatics.DtoDetailUpdateDtoFieldId);

        DtoFieldUpdateDto = new DtoFieldUpdateDto(_fieldRepository, _relationRepository)
        {
            Id = dtoField.Id,
            DtoRelatedEntityId = StateStatics.DtoDetailRelatedEntityId,
            SourceEntityId = dtoField.SourceField.EntityId,
            SourceFromAnotherEntity = StateStatics.DtoDetailRelatedEntityId != dtoField.SourceField.EntityId ? Visibility.Visible : Visibility.Hidden,
            Name = dtoField.Name,
            SourceFieldId = dtoField.SourceFieldId,
            IsRequired = dtoField.IsRequired,
            IsList = dtoField.IsList,
        };
        if(dtoFiedRelations != null)
        { 

            DtoFieldUpdateDto.DtoFieldRelations = new ObservableCollection<DtoFieldRelationsCreateForUpdateModel>(dtoFiedRelations.Select(x => new DtoFieldRelationsCreateForUpdateModel(_relationRepository)
            {
                SequenceNo = x.SequenceNo,
                FirstEntityId = dtoField.Dto.RelatedEntityId,
                SecondEntityId = x.DtoField.SourceField.EntityId,
                RelationId = x.RelationId,
                Relations = new ObservableCollection<RelationVisualModel>(_relationRepository.GetRelationsBehindEntities(dtoField.SourceField.EntityId, dtoField.Dto.RelatedEntityId).Select(x => new RelationVisualModel
                {
                    Id = x.Id,
                    Name = x.PrimaryField.EntityId != dtoField.Dto.RelatedEntityId ? $"(...).{x.ForeignEntityVirPropName}" : $"(...).{x.PrimaryEntityVirPropName}"
                }))
            }));
        }

        SaveCommand = new RellayCommand(obj =>
        {
            try
            {
                if (string.IsNullOrEmpty(DtoFieldUpdateDto.Name) || DtoFieldUpdateDto.SourceFieldId == default)
                {
                    MessageBox.Show("Check The Fields!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _dtoFieldRepository.Update(DtoFieldUpdateDto);

                MessageBox.Show("Field Updated Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                CloseDialogAction?.Invoke();
                _navigation.NavigateTo<DtoDetailVM>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });

        AddRelationCommand = new RellayCommand(obj =>
        {
            try
            {
                DtoFieldUpdateDto.DtoFieldRelations!.Add(new DtoFieldRelationsCreateForUpdateModel(_relationRepository)
                {
                    FirstEntityId = DtoFieldUpdateDto.DtoRelatedEntityId,
                    SecondEntityId = DtoFieldUpdateDto.SourceEntityId,
                    SequenceNo = DtoFieldUpdateDto.DtoFieldRelations.Count() + 1,
                    Relations = new ObservableCollection<RelationVisualModel>(_relationRepository.GetRelationsBehindEntities(dtoField.SourceField.EntityId, dtoField.Dto.RelatedEntityId).Select(x => new RelationVisualModel
                    {
                        Id = x.Id,
                        Name = x.PrimaryField.EntityId != DtoFieldUpdateDto.DtoRelatedEntityId ? $"(...).{x.ForeignEntityVirPropName}" : $"(...).{x.PrimaryEntityVirPropName}"
                    }))
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });

        RemoveRelationCommand = new RellayCommand(obj =>
        {
            try
            {
                DtoFieldUpdateDto.DtoFieldRelations!.Remove((DtoFieldRelationsCreateForUpdateModel)obj);
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
