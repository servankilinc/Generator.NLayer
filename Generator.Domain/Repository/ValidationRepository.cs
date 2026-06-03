using Generator.Domain.Context;
using Microsoft.EntityFrameworkCore;
using Generator.Domain.Core.Entities;
using Generator.Domain.Repository.Base;

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

    //public void AddValidationList(List<ValidationCreateDto> list)
    //{
    //    using var _context = new ProjectContext();
    //    using var transaction = _context.Database.BeginTransaction();
    //    try
    //    {
    //        foreach (var createDto in list)
    //        {
    //            var validation = _context.Validations.Add(new Validation
    //            {
    //                DtoFieldId = createDto.DtoFieldId,
    //                ValidatorTypeId = createDto.ValidatorTypeId,
    //                ErrorMessage = createDto.ErrorMessage
    //            }).Entity;
    //            _context.SaveChanges();

    //            // add validation params if exist
    //            if (createDto.ValidationParams != null)
    //            {
    //                foreach (var validationParam in createDto.ValidationParams)
    //                {
    //                    _context.ValidationParams.Add(new ValidationParam
    //                    {
    //                        ValidationId = validation.Id,
    //                        ValidatorTypeParamId = validationParam.ValidatorTypeParamId,
    //                        Value = validationParam.Value
    //                    });
    //                }
    //                _context.SaveChanges();
    //            }
    //        }

    //        transaction.Commit();
    //    }
    //    catch (Exception)
    //    {
    //        transaction.Rollback();
    //        throw;
    //    }
    //}

    //public void AddValidation(ValidationCreateDto createDto)
    //{
    //    using var _context = new ProjectContext();
    //    using var transaction = _context.Database.BeginTransaction();
    //    try
    //    {
    //        var validation = _context.Validations.Add(new Validation
    //        {
    //            DtoFieldId = createDto.DtoFieldId,
    //            ValidatorTypeId = createDto.ValidatorTypeId,
    //            ErrorMessage = createDto.ErrorMessage
    //        }).Entity;
    //        _context.SaveChanges();

    //        // add validation params if exist
    //        if (createDto.ValidationParams != null)
    //        {
    //            foreach (var validationParam in createDto.ValidationParams)
    //            {
    //                _context.ValidationParams.Add(new ValidationParam
    //                {
    //                    ValidationId = validation.Id,
    //                    ValidatorTypeParamId = validationParam.ValidatorTypeParamId,
    //                    Value = validationParam.Value
    //                });
    //            }
    //            _context.SaveChanges();
    //        }

    //        transaction.Commit();
    //    }
    //    catch (Exception)
    //    {
    //        transaction.Rollback();
    //        throw;
    //    }
    //}

    //public void UpdateValidation(ValidationUpdateDto updateDto)
    //{
    //    using var _context = new ProjectContext();
    //    using var transaction = _context.Database.BeginTransaction();
    //    try
    //    {
    //        var validation = _context.Validations.FirstOrDefault(f => f.Id == updateDto.ValidationId);
    //        if (validation == null) throw new Exception("Validation not found");

    //        validation.DtoFieldId = updateDto.DtoFieldId;
    //        validation.ValidatorTypeId = updateDto.ValidatorTypeId;
    //        validation.ErrorMessage = updateDto.ErrorMessage;
    //        _context.SaveChanges();

    //        // delete old validation params
    //        var oldValidationParams = _context.ValidationParams.Where(f => f.ValidationId == updateDto.ValidationId).ToList();
    //        if (oldValidationParams != null && oldValidationParams.Count > 0)
    //        {
    //            _context.ValidationParams.RemoveRange(oldValidationParams);
    //            _context.SaveChanges();
    //        }

    //        // add validation params if exist
    //        if (updateDto.ValidationParams != null)
    //        {
    //            foreach (var validationParam in updateDto.ValidationParams)
    //            {
    //                _context.ValidationParams.Add(new ValidationParam
    //                {
    //                    ValidationId = validation.Id,
    //                    ValidatorTypeParamId = validationParam.ValidatorTypeParamId,
    //                    Value = validationParam.Value
    //                });
    //            }
    //            _context.SaveChanges();
    //        }

    //        transaction.Commit();
    //    }
    //    catch (Exception)
    //    {
    //        transaction.Rollback();
    //        throw;
    //    }
    //}
}
