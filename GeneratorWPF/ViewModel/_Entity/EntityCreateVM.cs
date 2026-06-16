using GeneratorWPF.Utils;
using System.Collections.ObjectModel;
using System.Windows.Input;
using GeneratorWPF.Models;
using GeneratorWPF.Repository;
using GeneratorWPF.Dtos._Entity;
using GeneratorWPF.Dtos._Field;
using System.Windows;
using GeneratorWPF.Models.Enums;
using Microsoft.EntityFrameworkCore;
using GeneratorWPF.Services;

namespace GeneratorWPF.ViewModel._Entity;

public class EntityCreateVM : BaseViewModel
{
    private EntityRepository _entityRepository { get; set; }
    private FieldTypeRepository _fieldTypeRepository { get; set; }
    public ICommand SaveCommand { get; set; }
    public ICommand AddFieldCommand { get; set; }
    public ICommand RemoveFieldCommand { get; set; }
    public ICommand CancelCommand { get; set; }
    public ObservableCollection<FieldType> FieldTypeList { get; set; } 
    public EntityCreateDto EntityToCreate { get; set; } = new EntityCreateDto();
    private ObservableCollection<FieldCreateDto> _FieldsToEntity = new ObservableCollection<FieldCreateDto>();
    public ObservableCollection<FieldCreateDto> FieldsToEntity { get => _FieldsToEntity; set { _FieldsToEntity = value; OnPropertyChanged(nameof(FieldsToEntity)); }}
    public Action? CloseDialogAction { get; set; }

    public EntityCreateVM(INavigationService navigation)
    {
        _entityRepository = new EntityRepository();
        _fieldTypeRepository = new FieldTypeRepository();

        FieldTypeList = new ObservableCollection<FieldType>(_fieldTypeRepository.GetAll(filter: f => f.SourceTypeId == (byte)FieldTypeSourceEnums.Base));


        AddFieldCommand = new RellayCommand(obj =>
        {
            FieldsToEntity.Add(new FieldCreateDto
            {
                Name = "New Field",
                FieldTypeId = 1,
                IsRequired = true,
                IsUnique = false,
                IsList = false,
                Filterable = false
            });
        });

        RemoveFieldCommand = new RellayCommand(field =>
        {
            if (field != null && FieldsToEntity.Contains(field))
            {
                FieldsToEntity.Remove((FieldCreateDto)field);
            }
        });

        SaveCommand = new RellayCommand(obj =>
        {
            try
            {
                if (string.IsNullOrEmpty(EntityToCreate.Name) || string.IsNullOrEmpty(EntityToCreate.TableName) || FieldsToEntity.Count == 0)
                {
                    MessageBox.Show("Check The Fields!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                EntityToCreate.Fields = FieldsToEntity.ToList();

                _entityRepository.Create(EntityToCreate);

                MessageBox.Show("Entity Created Successfully", "Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                
                navigation.NavigateTo<EntityHomeVM>();

                CloseDialogAction?.Invoke();
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
