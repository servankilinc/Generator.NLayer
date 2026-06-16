using Generator.Domain.Core;
using Generator.Domain.Core.Entities;
using Generator.Domain.Repository;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Generator.NTier.CodeGenerators.NLayer.Business.Helpers;

public static class MappingProfilesHelper
{
    public static string[] GetUsings(EntityRepository entityRepository, DtoRepository dtoRepository, AppSetting appSettings)
    {
        var isEntityExist = entityRepository.IsExist(f => true);
        var dtos = dtoRepository.GetAll(f => true, include: i => i.Include(x => x.RelatedEntity), enableTracking: false);

        var usings = new List<string> { "AutoMapper" };
        if (isEntityExist)
            usings.Add($"{appSettings.ModelLayerProjectName}.Entities");
        if (appSettings.IsThereIdentity)
            usings.Add($"{appSettings.ModelLayerProjectName}.Auth.SignUp");
        foreach (var group in dtos.GroupBy(x => x.RelatedEntity.Name))
        {
            if (group.Any(x => x.CrudTypeId == (int)Enums.CrudTypeEnums.Read))
                usings.Add($"{appSettings.ModelLayerProjectName}.Dtos.{group.Key}.Queries");
            if (group.Any(x => x.CrudTypeId != (int)Enums.CrudTypeEnums.Read))
                usings.Add($"{appSettings.ModelLayerProjectName}.Dtos.{group.Key}.Commands");
        }

        return [.. usings];
    }

    public static BlockSyntax GetRules(EntityRepository entityRepository, DtoRepository dtoRepository, DtoFieldRepository dtoFieldRepository, AppSetting appSettings)
    {
        var entities = entityRepository.GetAll(f => true, include: i => i.Include(x => x.Fields).ThenInclude(ti => ti.FieldType), enableTracking: false);

        var mappings = new List<StatementSyntax>
        {
            SyntaxFactory.ParseStatement("// CreateMap<source, dest>")
        };
        foreach (var entity in entities)
        {
            var dtoList = dtoRepository.GetAll(f => f.RelatedEntityId == entity.Id, enableTracking: false);

            string entityToEntityRule = $"CreateMap<{entity.Name}, {entity.Name}>().ForAllMembers(opt => opt.Condition((src, dest, srcMember, destMember) => !Equals(srcMember, destMember)));";
            mappings.Add(SyntaxFactory.ParseStatement(entityToEntityRule));

            if (appSettings.IsThereIdentity && appSettings.UserEntityId == entity.Id)
            {
                mappings.Add(MapperCommandSignupDto(appSettings, entity));
            }
            // Query Dtos Mapping
            foreach (var dto in dtoList.Where(f => f.CrudTypeId == (int)Enums.CrudTypeEnums.Read))
                mappings.Add(MapperQueryDto(entity, dto, dtoFieldRepository));

            // Command Dtos Mapping
            foreach (var dto in dtoList.Where(f => f.CrudTypeId != (int)Enums.CrudTypeEnums.Read))
                mappings.Add(MapperCommandDto(entity, dto, dtoFieldRepository));
        }

        return SyntaxFactory.Block(mappings);
    }


    #region Dto Mappers
    private static StatementSyntax MapperCommandSignupDto(AppSetting appSettings, Entity entity)
    {
        StringBuilder sb = new();
        sb.Append($"CreateMap<SignUpRequest, {entity.Name}>()");
        foreach (var field_user in entity.Fields.Where(f => !f.IsUnique && f.IsRequired && f.FieldType.SourceTypeId == (int)Enums.FieldTypeSourceEnums.Base))
        {
            sb.Append($".ForMember(dest => dest.{field_user.Name}, opt => opt.MapFrom(src => src.{field_user.Name}))");
        }

        // Todo: Add UserName integration
        //signupMapping.Append($".ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))");
        sb.Append($".ReverseMap();");

        return SyntaxFactory.ParseStatement(sb.ToString());
    }

    private static StatementSyntax MapperCommandDto(Entity entity, Dto dto, DtoFieldRepository dtoFieldRepository)
    {
        var dtoFields = dtoFieldRepository.GetAll(
            filter: f => f.DtoId == dto.Id,
            include: i => i.Include(x => x.SourceField).ThenInclude(x => x.FieldType),
            enableTracking: false
        );

        StringBuilder sb = new();
        sb.Append($"CreateMap<{dto.Name}, {entity.Name}>()");
        foreach (var dtoField in dtoFields)
        {
            if (dtoField.SourceField.FieldType.SourceTypeId == (int)Enums.FieldTypeSourceEnums.Base)
                sb.Append($".ForMember(dest => dest.{dtoField.Name}, opt => opt.MapFrom(src => src.{dtoField.SourceField.Name}))");
            else if (dtoField.SourceField.FieldType.SourceTypeId == (int)Enums.FieldTypeSourceEnums.Dto)
                sb.Append($".ForMember(dest => dest.{dtoField.Name}, opt => opt.MapFrom(src => src))");
        }
        sb.Append(".ReverseMap();");

        return SyntaxFactory.ParseStatement(sb.ToString());
    }

    private static StatementSyntax MapperQueryDto(Entity entity, Dto dto, DtoFieldRepository dtoFieldRepository)
    {
        var dtoFields = dtoFieldRepository.GetAll(
            filter: f => f.DtoId == dto.Id,
            include: i => i
                .Include(x => x.SourceField).ThenInclude(x => x.Entity)
                .Include(x => x.SourceField).ThenInclude(x => x.FieldType),
            enableTracking: false
        );

        StringBuilder sb = new();
        sb.Append($"CreateMap<{entity.Name}, {dto.Name}>()");

        var processedFields = new HashSet<string>();

        foreach (var df in dtoFields)
        {
            if (!processedFields.Add(df.Name))
                continue;

            var source = df.SourceField;
            if (source == null || source.FieldType == null)
                continue;

            if (source.EntityId == entity.Id)
            {
                if (source.FieldType.SourceTypeId == (int)Enums.FieldTypeSourceEnums.Base)
                    sb.Append($".ForMember(dest => dest.{df.Name}, opt => opt.MapFrom(src => src.{source.Name}))");
                else
                    sb.Append($".ForMember(dest => dest.{df.Name}, opt => opt.MapFrom(src => src))");
            }
            else
            {
                var relations = dtoFieldRepository.GetDtoFieldRelations(df.Id);

                if (relations == null || !relations.Any())
                    throw new Exception($"Cannot find relations between entities ({entity.Name}, {source.Entity?.Name})");

                var first = relations.First();
                bool firstIsPrimary = first.Relation.PrimaryField.EntityId == entity.Id;

                string firstNavProp = firstIsPrimary
                    ? first.Relation.PrimaryEntityVirPropName
                    : first.Relation.ForeignEntityVirPropName;

                int lastEntityId = firstIsPrimary
                    ? first.Relation.ForeignField.EntityId
                    : first.Relation.PrimaryField.EntityId;

                bool isInList = first.Relation.RelationTypeId == (int)Enums.RelationTypeEnums.OneToMany && firstIsPrimary;

                string chain = firstNavProp;

                for (int i = 1; i < relations.Count; i++)
                {
                    var rel = relations[i];
                    bool isPrimary = rel.Relation.PrimaryField.EntityId == lastEntityId;

                    string navProp = isPrimary
                        ? rel.Relation.PrimaryEntityVirPropName
                        : rel.Relation.ForeignEntityVirPropName;

                    bool nextIsList = rel.Relation.RelationTypeId == (int)Enums.RelationTypeEnums.OneToMany && isPrimary;

                    chain = isInList
                        ? (nextIsList
                            ? $"{chain}.SelectMany(x => x.{navProp})"
                            : $"{chain}.Select(x => x.{navProp})")
                        : $"{chain}.{navProp}";

                    lastEntityId = isPrimary
                        ? rel.Relation.ForeignField.EntityId
                        : rel.Relation.PrimaryField.EntityId;

                    if (!isInList) isInList = nextIsList;
                }

                if (source.FieldType.SourceTypeId == (int)Enums.FieldTypeSourceEnums.Base)
                    chain = isInList
                        ? $"{chain}.Select(x => x.{source.Name})"
                        : $"{chain}.{source.Name}";

                sb.Append($".ForMember(dest => dest.{df.Name}, opt => opt.MapFrom(src => src.{firstNavProp} != default ? src.{chain} : default))");
            }
        }

        if (entity.ReportDtoId == dto.Id)
        {
            if (entity.Auditable)
            {
                sb.Append($".ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))");
                sb.Append($".ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))");
                sb.Append($".ForMember(dest => dest.CreateDateUtc, opt => opt.MapFrom(src => src.CreateDateUtc))");
                sb.Append($".ForMember(dest => dest.UpdateDateUtc, opt => opt.MapFrom(src => src.UpdateDateUtc))");
            }
            if (entity.SoftDeletable)
            {
                sb.Append($".ForMember(dest => dest.DeletedBy, opt => opt.MapFrom(src => src.DeletedBy))");
                sb.Append($".ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted))");
                sb.Append($".ForMember(dest => dest.DeletedDateUtc, opt => opt.MapFrom(src => src.DeletedDateUtc))");
            }
        }

        sb.Append(".ForAllMembers(opt => opt.Condition((src, dest, srcMember, destMember) => !Equals(srcMember, destMember)));");

        return SyntaxFactory.ParseStatement(sb.ToString());
    }
    #endregion
}