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
