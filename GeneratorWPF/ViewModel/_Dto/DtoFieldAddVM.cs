using GeneratorWPF.Utils;
using System.Windows.Input;
using GeneratorWPF.Repository;
using System.Windows;
using GeneratorWPF.Services;
using GeneratorWPF.Dtos._DtoField;
using GeneratorWPF.Models;
using System.Collections.ObjectModel;

namespace GeneratorWPF.ViewModel._Dto;

public class DtoFieldAddVM : BaseViewModel
{
    private INavigationService _navigation;
    private FieldRepository _fieldRepository { get; set; }
    private DtoFieldRepository _dtoFieldRepository { get; set; }
    private EntityRepository _entityRepository { get; set; }
    private RelationRepository _relationRepository { get; set; }
    public ICommand SaveCommand { get; set; }
    public ICommand CancelCommand { get; set; }
    public ICommand AddRelationCommand { get; set; }
    public ICommand RemoveRelationCommand { get; set; }

    public ObservableCollection<Entity> EntityList { get; set; }

    private DtoFieldCreateDto _dtoFieldCreateDto;
    public DtoFieldCreateDto DtoFieldCreateDto { get => _dtoFieldCreateDto; set { _dtoFieldCreateDto = value; OnPropertyChanged(nameof(DtoFieldCreateDto)); }}
    public Action? CloseDialogAction { get; set; } // Dialog'u kapatmak için callback

    public DtoFieldAddVM(INavigationService navigation)
    {
        _navigation = navigation;
        _fieldRepository = new FieldRepository();
        _dtoFieldRepository = new DtoFieldRepository();
        _entityRepository = new EntityRepository();
        _relationRepository = new RelationRepository();

        EntityList = new ObservableCollection<Entity>(_entityRepository.GetAll());

        DtoFieldCreateDto = new DtoFieldCreateDto(_fieldRepository, _relationRepository) 
        { 
            DtoId = StateStatics.DtoDetailAddDtoFieldDtoId,
            SourceEntityId = StateStatics.DtoDetailRelatedEntityId,
            IsRequired = true,
            IsList = false,
            DtoRelatedEntityId = StateStatics.DtoDetailRelatedEntityId
        };

        SaveCommand = new RellayCommand(obj =>
        {
            try
            {
                if (string.IsNullOrEmpty(DtoFieldCreateDto.Name) || DtoFieldCreateDto.SourceFieldId == default)
                {
                    MessageBox.Show("Check The Fields!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (DtoFieldCreateDto.SourceEntityId != DtoFieldCreateDto.DtoRelatedEntityId)
                {
                    if (DtoFieldCreateDto.DtoFieldRelations!.Count == 0)
                    {
                        MessageBox.Show("Check The Relations!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                _dtoFieldRepository.Add(DtoFieldCreateDto);

                MessageBox.Show("Field Added Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);

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
                DtoFieldCreateDto.DtoFieldRelations!.Add(new DtoFieldRelationsCreateModel(_relationRepository)
                {
                    FirstEntityId = DtoFieldCreateDto.DtoRelatedEntityId,
                    SecondEntityId = DtoFieldCreateDto.SourceEntityId,
                    SequenceNo = DtoFieldCreateDto.DtoFieldRelations.Count() + 1,
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
                DtoFieldCreateDto.DtoFieldRelations!.Remove((DtoFieldRelationsCreateModel)obj);
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
