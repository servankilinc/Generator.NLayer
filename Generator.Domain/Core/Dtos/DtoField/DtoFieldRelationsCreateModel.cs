namespace Generator.Domain.Core.Dtos.DtoField;

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
