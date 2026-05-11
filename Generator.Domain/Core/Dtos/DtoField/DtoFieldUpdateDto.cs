using Generator.Domain.Core.Dtos.Relation;
using System.Collections.ObjectModel;

namespace Generator.Domain.Core.Dtos.DtoField;

public class DtoFieldRelationsCreateForUpdateModel
{
    private readonly RelationRepository _relationRepository;
    public DtoFieldRelationsCreateForUpdateModel(RelationRepository relationRepository)
    {
        _relationRepository = relationRepository;
    }


    //public int DtoFieldId { get; set; } // dto field insert sonrası eklenecek
    public int RelationId { get; set; } // select ile seçilecek
    public int SequenceNo { get; set; } // kullanıcı girecek
    public virtual DtoField DtoField { get; set; } = null!;




    // ************** UI PROPS **************
    private int _firstEntityId;
    public int FirstEntityId
    {
        get => _firstEntityId;
        set
        {
            if (_firstEntityId != value)
            {
                _firstEntityId = value;
                OnPropertyChanged(nameof(FirstEntityId));

                if (_secondEntityId == default)
                {
                    if (Relations != null) Relations.Clear();
                    else Relations = new ObservableCollection<RelationVisualModel>();
                }
                else
                {
                    if (_secondEntityId == default) return;
                    // 
                }
            }
        }
    }

    private int _secondEntityId;
    public int SecondEntityId
    {
        get => _secondEntityId;
        set
        {
            if (_secondEntityId != value)
            {
                _secondEntityId = value;
                OnPropertyChanged(nameof(SecondEntityId));

                if (_firstEntityId == default)
                {
                    if (Relations != null) Relations.Clear();
                    else Relations = new ObservableCollection<RelationVisualModel>();
                }
                else
                {
                    if (_secondEntityId == default) return;

                    if (FirstEntityId == SecondEntityId) return;

                    Relations = new ObservableCollection<RelationVisualModel>(_relationRepository.GetRelationsBehindEntities(SecondEntityId, FirstEntityId).Select(x => new RelationVisualModel
                    {
                        Id = x.Id,
                        Name = x.PrimaryField.EntityId != FirstEntityId ? $"(...).{x.ForeignEntityVirPropName}" : $"(...).{x.PrimaryEntityVirPropName}"
                    }));

                    if (Relations != null && Relations.Any()) RelationId = Relations.First().Id;
                }
            }
        }
    }

    private ObservableCollection<RelationVisualModel>? _relations;
    public ObservableCollection<RelationVisualModel>? Relations
    {
        get => _relations;
        set
        {
            if (_relations != value)
            {
                _relations = value;
                OnPropertyChanged(nameof(Relations));
            }
        }
    }
}

public class DtoFieldUpdateDto : ObversableObject
{
    private readonly FieldRepository _fieldRepository;
    private readonly RelationRepository _relationRepository;

    public DtoFieldUpdateDto(FieldRepository fieldRepository, RelationRepository relationRepository)
    {
        this._fieldRepository = fieldRepository;
        this._relationRepository = relationRepository;
    }

    public int Id { get; set; }
    public bool IsRequired { get; set; }
    public bool IsList { get; set; }


    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    private int _sourceFieldId;
    public int SourceFieldId
    {
        get => _sourceFieldId;
        set
        {
            if (_sourceFieldId != value)
            {
                _sourceFieldId = value;
                OnPropertyChanged(nameof(SourceFieldId));

                if (this.Name == default && this._fieldRepository != null)
                {
                    var fieldData = this._fieldRepository.Get(f => f.Id == value);
                    this.Name = fieldData.Name;
                }
            }
        }
    }

    private int _sourceEntityId;
    public int SourceEntityId
    {
        get => _sourceEntityId;
        set
        {
            if (_sourceEntityId != value)
            {
                _sourceEntityId = value;
                OnPropertyChanged(nameof(SourceEntityId));

                if (_fieldRepository != null)
                {
                    FieldList = new ObservableCollection<Field>(
                        _fieldRepository.GetAll(filter: f => f.EntityId == value)
                    );
                }

                if (_sourceEntityId != default && DtoRelatedEntityId != default)
                {
                    SourceFromAnotherEntity = _sourceEntityId != DtoRelatedEntityId ? Visibility.Visible : Visibility.Hidden;

                    if (DtoFieldRelations == null) return;

                    if (DtoFieldRelations.Any(f => f.SecondEntityId == _sourceEntityId)) return;

                    DtoFieldRelations.Clear();
                    
                    DtoFieldRelations.Add(new DtoFieldRelationsCreateForUpdateModel(_relationRepository)
                    {
                        FirstEntityId = DtoRelatedEntityId,
                        SecondEntityId = SourceEntityId,
                        SequenceNo = DtoFieldRelations.Count() + 1,
                    });
                }
            }
        }
    }


    private ObservableCollection<Field>? _fieldList;
    public ObservableCollection<Field>? FieldList
    {
        get => _fieldList;
        set
        {
            if (_fieldList != value)
            {
                _fieldList = value;
                OnPropertyChanged(nameof(FieldList));
            }
        }
    }


    private ObservableCollection<DtoFieldRelationsCreateForUpdateModel>? _dtoFieldRelations = new ObservableCollection<DtoFieldRelationsCreateForUpdateModel>();
    public ObservableCollection<DtoFieldRelationsCreateForUpdateModel>? DtoFieldRelations
    {
        get => _dtoFieldRelations;
        set
        {
            if (_dtoFieldRelations != value)
            {
                _dtoFieldRelations = value;
                OnPropertyChanged(nameof(DtoFieldRelations));
            }
        }
    }

    public Visibility _sourceFromAnotherEntity { get; set; } = Visibility.Hidden;
    public Visibility SourceFromAnotherEntity
    {
        get => _sourceFromAnotherEntity;
        set
        {
            if (_sourceFromAnotherEntity != value)
            {
                _sourceFromAnotherEntity = value;
                OnPropertyChanged(nameof(SourceFromAnotherEntity));

                if (value == Visibility.Visible)
                {
                    //FieldList = new ObservableCollection<Field>(
                    //    _fieldRepository.GetAll(filter: f => f.EntityId == value)
                    //);
                }
            }
        }
    }
    public int DtoRelatedEntityId { get; internal set; }
}
