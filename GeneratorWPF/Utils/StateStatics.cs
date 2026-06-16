using GeneratorWPF.Models.LocalModels;

namespace GeneratorWPF.Utils
{
    public static class StateStatics
    {
        public static Project? CurrentProject { get; set; } = default;

        public static int EntityDetailId { get; set; }
        public static int EntityUpdateId { get; set; }
        public static int FieldUpdateId { get; set; }
        public static int RelationUpdateId { get; set; }
        public static int DtoDetailId { get; set; }
        public static int DtoUpdateId { get; set; }
        public static int DtoDetailRelatedEntityId { get; set; }
        public static int DtoDetailUpdateDtoFieldId { get; set; }
        public static int DtoDetailValidationId { get; set; }
        public static int DtoDetailAddDtoFieldDtoId { get; set; }
    }
}
