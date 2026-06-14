using Generator.Domain.Context;
using Generator.Domain.Core.Dtos.Validation;
using Generator.Domain.Core.Dtos.ValidationParam;
using Generator.Domain.Core.Entities;
using Generator.Domain.Repository.Base;
using Microsoft.EntityFrameworkCore;

namespace Generator.Domain.Repository;

public class ValidationRepository : EFRepositoryBase<Validation>
{
    public List<ValidatorType> GetValidatorTypes()
    {
        using var _context = new ProjectContext();
        return _context.Set<ValidatorType>().Include(i => i.ValidatorTypeParams).ToList();
    }

    public ValidatorType GetValidatorType(int validatorTypeId)
    {
        using var _context = new ProjectContext();
        return _context.Set<ValidatorType>().FirstOrDefault(i => i.Id == validatorTypeId);
    }

    public List<ValidatorTypeParam> GetValidatorTypeParams(int validatorTypeId)
    {
        using var _context = new ProjectContext();
        return _context.Set<ValidatorTypeParam>().Where(f => f.ValidatorTypeId == validatorTypeId).Include(i => i.ValidatorType).ToList();
    }

    public List<ValidationParam> GetValidationParams(int validationId)
    {
        using var _context = new ProjectContext();
        return _context.Set<ValidationParam>().Where(f => f.ValidationId == validationId).Include(i => i.Validation).Include(i => i.ValidatorTypeParam).ToList();
    }
    
    public List<ValidationUpdateDto> GetUpdateDtos(int dtoFieldId)
    {
        using var _context = new ProjectContext();
        var data = _context.Validations
                .Include(i => i.DtoField)
                .Include(i => i.ValidationParams)
                    .ThenInclude(ti => ti.ValidatorTypeParam)
                .Where(f => f.DtoFieldId == dtoFieldId)
                .ToList();
        return data.Select(x => new ValidationUpdateDto
        {
            ValidationId = x.Id,
            DtoFieldId = x.DtoFieldId,
            ValidatorTypeId = x.ValidatorTypeId,
            ErrorMessage = x.ErrorMessage,
            ValidationParams = x.ValidationParams.Select(vp => new ValidationParamUpdateDto
            {
                ValidationId = x.Id,
                ValidatorTypeParamId = vp.ValidatorTypeParamId,
                Key = vp.ValidatorTypeParam.Key,
                Value = vp.Value
            }).ToList()
        }).ToList();
    }

    public void SetValidations(List<ValidationUpdateDto> list)
    {
        using var _context = new ProjectContext();
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            var existValidations = _context.Validations.Where(f => f.DtoFieldId == list.First().DtoFieldId);
            
            // Delete DtoFields that are not in the updateDtos list
            foreach (var exValidation in existValidations)
            {
                if (!list.Any(f => f.ValidationId == exValidation.Id))
                {
                    _context.Validations.Remove(exValidation);
                }
            }
            _context.SaveChanges();

            foreach (var updateDto in list)
            {
                var existData = existValidations.FirstOrDefault(f => f.Id == updateDto.ValidationId);

                if (existData == null)
                {
                    var validation = _context.Validations.Add(new Validation
                    {
                        DtoFieldId = updateDto.DtoFieldId,
                        ValidatorTypeId = updateDto.ValidatorTypeId,
                        ErrorMessage = updateDto.ErrorMessage
                    }).Entity;
                    _context.SaveChanges();

                    // add validation params if exist
                    if (updateDto.ValidationParams != null)
                    {
                        foreach (var validationParam in updateDto.ValidationParams)
                        {
                            _context.ValidationParams.Add(new ValidationParam
                            {
                                ValidationId = validation.Id,
                                ValidatorTypeParamId = validationParam.ValidatorTypeParamId,
                                Value = validationParam.Value
                            });
                        }
                        _context.SaveChanges();
                    }
                }
                else
                {
                    existData.DtoFieldId = updateDto.DtoFieldId;
                    existData.ValidatorTypeId = updateDto.ValidatorTypeId;
                    existData.ErrorMessage = updateDto.ErrorMessage;
                    _context.SaveChanges();

                    // delete old validation params
                    var oldValidationParams = _context.ValidationParams.Where(f => f.ValidationId == updateDto.ValidationId).ToList();
                    if (oldValidationParams != null && oldValidationParams.Count > 0)
                    {
                        _context.ValidationParams.RemoveRange(oldValidationParams);
                        _context.SaveChanges();
                    }

                    // add validation params if exist
                    if (updateDto.ValidationParams != null)
                    {
                        foreach (var validationParam in updateDto.ValidationParams)
                        {
                            _context.ValidationParams.Add(new ValidationParam
                            {
                                ValidationId = existData.Id,
                                ValidatorTypeParamId = validationParam.ValidatorTypeParamId,
                                Value = validationParam.Value
                            });
                        }
                        _context.SaveChanges();
                    }

                }
            }

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }
}
