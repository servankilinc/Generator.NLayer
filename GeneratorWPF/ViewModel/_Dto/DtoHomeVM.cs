using GeneratorWPF.Dtos._Dto;
using GeneratorWPF.Dtos._DtoField;
using GeneratorWPF.Models;
using GeneratorWPF.Repository;
using GeneratorWPF.Services;
using GeneratorWPF.Utils;
using GeneratorWPF.View._Dto.Partials;
using GeneratorWPF.ViewModel._Entity;
using System.Windows;
using System.Windows.Input;

namespace GeneratorWPF.ViewModel._Dto
{
    public class DtoHomeVM : BaseViewModel
    {
        private DtoRepository _dtoRepository { get; set; }
        private INavigationService _navigation { get; set; }
        public ICommand ShowCreateCommand { get; set; }
        public ICommand ShowDetailCommand { get; set; }
        public ICommand ShowUpdateCommand { get; set; }
        public ICommand RemoveCommand { get; set; }
        public List<Entity> EntityList { get; set; }

        private List<DtoDetailResponseDto> _DtoList = new List<DtoDetailResponseDto>();
        public List<DtoDetailResponseDto> DtoList { get { return _DtoList; } set { _DtoList = value; OnPropertyChanged(nameof(DtoList)); } }

        private int _entiyIdForFilter = default;
        public int EntiyIdForFilter { 
            get { return _entiyIdForFilter; } 
            set { 
                _entiyIdForFilter = value; 
                OnPropertyChanged(nameof(EntiyIdForFilter));

                DtoList = _dtoRepository.GetDetailList(f => f.RelatedEntityId == value);
            }
        } 

        public DtoHomeVM(INavigationService navigation)
        {
            _navigation = navigation;
            var entityRepository = new EntityRepository();
            _dtoRepository = new DtoRepository();

            EntityList = entityRepository.GetAll(enableTracking: false);
            DtoList = _dtoRepository.GetDetailList(f => true);

            ShowCreateCommand = new RellayCommand(RellayCommand =>
            {
                var dialog = new DtoCreateDialog(_navigation);
                dialog.ShowDialog();
            });

            ShowDetailCommand = new RellayCommand(dtoId =>
            {
                StateStatics.DtoDetailId = (int)dtoId;
                _navigation.NavigateTo<DtoDetailVM>();
            });

            ShowUpdateCommand = new RellayCommand(dtoId =>
            {
                StateStatics.DtoUpdateId = (int)dtoId;
                var dialog = new DtoUpdateDialog(_navigation);
                dialog.ShowDialog();
            });

            RemoveCommand = new RellayCommand(dtoId =>
            {
                try
                {
                    _dtoRepository.Delete((int)dtoId);
                    MessageBox.Show("Dto removed successfully ", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    DtoList = _dtoRepository.GetDetailList(f => EntiyIdForFilter != default ? f.Id == EntiyIdForFilter : true);
                }
                catch (Exception)
                {
                    MessageBox.Show("Dto could'nt remove", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw;
                }
            });
        }
    }
}
