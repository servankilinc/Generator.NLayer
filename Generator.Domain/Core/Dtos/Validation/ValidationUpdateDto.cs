using Generator.Domain.Repository;
using Generator.Domain.Core.Dtos.ValidationParam;
using System.Collections.ObjectModel;

namespace Generator.Domain.Core.Dtos.Validation;

public class ValidationUpdateDto
{
    private readonly ValidationRepository validationRepository;

    public ValidationUpdateDto(ValidationRepository validationRepository)
    {
        this.validationRepository = validationRepository;
    }

    public int ValidationId { get; set; }
    public int DtoFieldId { get; set; }

    public int OldValidatorTypeId { get; set; }

    private int _validatorTypeId;
    public int ValidatorTypeId
    {
        get => _validatorTypeId;
        set
        {
            if (_validatorTypeId != value)
            {
                _validatorTypeId = value;
                OnPropertyChanged(nameof(ValidatorTypeId));
                // ---- Set Validation Params ----
                if (validationRepository == null) return;

                // 1) İlk olarak validatorType değeri sistemde kayıtlı validatorTypeId ile aynı ise kayıtlı validationParamsları value bilgisi ile al
                if (OldValidatorTypeId == value)
                {
                    var validationParamList = validationRepository.GetValidationParams(this.ValidationId);
                    if (validationParamList != null && validationParamList.Count > 0)
                    {
                        ValidationParams = new ObservableCollection<ValidationParamUpdateDto>(validationParamList.Select(vp =>
                        {
                            this.ErrorMessage = vp.Validation.ErrorMessage;
                            return new ValidationParamUpdateDto
                            {
                                Key = vp.ValidatorTypeParam.Key,
                                ValidatorTypeParamId = vp.ValidatorTypeParamId,
                                Value = vp.Value
                            };
                        }));
                    }
                }
                else
                {
                    // 2) Eğer yoksa validatorTypeId ile kayıtlı validationParamsları al
                    List<ValidatorTypeParam> _validatorTypeParamList = validationRepository.GetValidatorTypeParams(value);

                    if (_validatorTypeParamList != null && _validatorTypeParamList.Count > 0)
                    {
                        ValidationParams = new ObservableCollection<ValidationParamUpdateDto>(_validatorTypeParamList.Select(vp =>
                        {
                            this.ErrorMessage = vp.ValidatorType.Description;
                            return new ValidationParamUpdateDto
                            {
                                Key = vp.Key,
                                ValidatorTypeParamId = vp.Id,
                                Value = string.Empty
                            };
                        }));
                    }
                    else
                    {
                        ValidationParams = new ObservableCollection<ValidationParamUpdateDto>();
                        var _validatorType = validationRepository.GetValidatorType(value);
                        if (_validatorType != null) this.ErrorMessage = _validatorType.Description;
                    }
                }
                // ---- Set Validation Params ----
            }
        }
    }

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set { if (_errorMessage != value) { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); } } }

    private ObservableCollection<ValidationParamUpdateDto>? _validationParams;
    public ObservableCollection<ValidationParamUpdateDto>? ValidationParams
    {
        get => _validationParams;
        set
        {
            if (_validationParams != value)
            {
                _validationParams = value;
                OnPropertyChanged(nameof(ValidationParams));
            }
        }
    }
}
