namespace Generator.Domain.Core.Dtos.DtoField;

public class DtoFieldResponseDto
{
	public int Id { get; set; }
	public string Name { get; set; } = null!;
	public int DtoId { get; set; }
	public string SourceFieldName { get; set; } = null!;
	public string EntityName { get; set; } = null!;
	public string FieldTypeName { get; set; } = null!;
	public bool IsRequired { get; set; }
	public bool IsList { get; set; }

	public bool IsSourceFromForeignEntity { get; set; }
	public bool IsThereRelations { get; set; }
	public string DtoFieldRelationsPath { get; set; } = null!;
}
