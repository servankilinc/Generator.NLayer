using Generator.Domain.Core.Dtos.Relation;
using Generator.Domain.Core.Entities;

namespace Generator.Domain.Core.Dtos.DtoField;

public class DtoFieldUpdateDto
{
    public int Id { get; set; }
 
    public int SourceEntityId { get; set; }
    public int SourceFieldId { get; set; } // SourceEntityId seçilmediyse combobox kapatılmalı ve source field değişince Name ve IsRequired otomatik olarak field.name ve field.isRequired alsın
    public bool IsRequired { get; set; }
    public bool IsList { get; set; }
    public string Name { get; set; } = null!;
     
    public List<DtoFieldRelationsCreateForUpdateModel>? DtoFieldRelations { get; set; }
}



public class DtoFieldRelationsCreateForUpdateModel
{
    //public List<RelationVisualModel>? Relations { get; set; } // state list RelationId için
    public int RelationId { get; set; } // select ile seçilecek
    public int SequenceNo { get; set; } // kullanıcı girecek


    // ************** UI PROPS **************

    // FirstEntityId veya SecondEntityId değişince List<RelationVisualModel>? Relations temizlenmeli, her ikisi de seçiliyse ve aynı entitye ait değillerse yeni liste çekilmeli sadece bir tane ilişki varsa default seçili olmalı
    // if (FirstEntityId == SecondEntityId) return;
    // Relations = new ObservableCollection<RelationVisualModel>(_relationRepository.GetRelationsBehindEntities(SecondEntityId, FirstEntityId).Select(x => new RelationVisualModel
    // {
    //     Id = x.Id,
    //     Name = x.PrimaryField.EntityId != FirstEntityId ? $"(...).{x.ForeignEntityVirPropName}" : $"(...).{x.PrimaryEntityVirPropName}"
    // }));
    // if (Relations != null && Relations.Any()) RelationId = Relations.First().Id;
    public int FirstEntityId { get; set; }

    public int SecondEntityId { get; set; }
}