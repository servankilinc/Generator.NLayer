using Generator.Domain.Core.Dtos.ValidationParam;
using Generator.Domain.Repository;
using System.Collections.ObjectModel;

namespace Generator.Domain.Core.Dtos.Validation;

public class ValidationCreateDto
{
    private readonly ValidationRepository validationRepository;
    public ValidationCreateDto()
    {
        this.validationRepository = null;
        this.ValidationParams = new ObservableCollection<ValidationParamCreateDto>();
    }

    public ValidationCreateDto(ValidationRepository validationRepository)
    {
        this.validationRepository = validationRepository;
        this.ValidationParams = new ObservableCollection<ValidationParamCreateDto>();
    }

    public int DtoFieldId { get; set; }
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
                if (validationRepository != null)
                {
                    var _validatorTypeParamList = validationRepository.GetValidatorTypeParams(value);
                    if (_validatorTypeParamList != null && _validatorTypeParamList.Count > 0)
                    {
                        ValidationParams = new ObservableCollection<ValidationParamCreateDto>(_validatorTypeParamList.Select(vp =>
                        {
                            this.ErrorMessage = vp.ValidatorType.Description;
                            return new ValidationParamCreateDto
                            {
                                Key = vp.Key,
                                ValidatorTypeParamId = vp.Id,
                            };
                        }));
                    }
                    else
                    {
                        ValidationParams = new ObservableCollection<ValidationParamCreateDto>();
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

    private ObservableCollection<ValidationParamCreateDto>? _validationParams;
    public ObservableCollection<ValidationParamCreateDto>? ValidationParams
    {
        get => _validationParams;
        set
        {
            if (_validationParams != value)
            {
                _validationParams = value;
                OnPropertyChanged(nameof(ValidationParams)); // <-- Bu çok önemli
            }
        }
    }
}
/*
# ValidationCreateDto 
{
    *** Validation *** 
    public int DtoFieldId { get; set; } // UserCreateDto.UserName
    public int ValidatorTypeId { get; set; } // Range
    public string? ErrorMessage { get; set; } // Lütfen 6 ile 18 karakter arasında bir değer giriniz.
    *** ValidationParams (List) ***
    {
        public int ValidatorTypeParamId { get; set; } // Min
        *** ValidationParams ***
        public string Value { get; set; } // 6
    }
    {
        public int Id { get; set; } // Max
        *** ValidationParams ***
        public string Value { get; set; } // 18
    }
}
*/
