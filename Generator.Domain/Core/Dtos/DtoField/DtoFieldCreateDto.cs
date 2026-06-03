using Generator.Domain.Core.Dtos.Relation;
using Generator.Domain.Repository;
using System.Collections.ObjectModel;

namespace Generator.Domain.Core.Dtos.DtoField;

public class DtoFieldCreateDto
{
    public int DtoId { get; set; }

    // 1) SourceFieldId yi doldurduğun selecti yeni entity'e ait fieldlar ile doldur
    // 2) (SourceEntityId != default && DtoRelatedEntityId != null && SourceEntityId != DtoRelatedEntityId ise DtoFieldRelations yenilenmeli
    //DtoFieldRelations.Clear();
    //DtoFieldRelations.Add(new DtoFieldRelationsCreateForUpdateModel(_relationRepository)
    //{
    //    FirstEntityId = DtoRelatedEntityId,
    //    SecondEntityId = SourceEntityId,
    //    SequenceNo = DtoFieldRelations.Count() + 1,
    //});
    public int SourceEntityId { get; set; } 
    public int SourceFieldId { get; set; } // SourceEntityId seçilmediyse combobox kapatılmalı ve source field değişince Name ve IsRequired otomatik olarak field.name ve field.isRequired alsın
    public string Name { get; set; } = null!;
    public bool IsRequired { get; set; }
    public bool IsList { get; set; }

    public List<DtoFieldRelationsCreateModel>? DtoFieldRelations { get; set; } // bu liste 
}


public class DtoFieldRelationsCreateModel
{
    // public List<RelationVisualModel>? Relations // state list RelationId için
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
