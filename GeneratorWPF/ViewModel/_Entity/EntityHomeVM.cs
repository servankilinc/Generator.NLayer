using GeneratorWPF.Utils;
using System.Windows.Input;
using GeneratorWPF.Models;
using GeneratorWPF.Repository;
using GeneratorWPF.View._Entity.Partials;
using GeneratorWPF.Services;

namespace GeneratorWPF.ViewModel._Entity;

public class EntityHomeVM : BaseViewModel
{
    private INavigationService _navigation;
    private EntityRepository _entityRepository { get; set; }
    public ICommand ShowCreateDialogCommand { get; set; }
    public ICommand ShowUpdateDialogCommand { get; set; }
    public ICommand ShowDetailCommand { get; set; }
    public ICommand DeleteCommand { get; set; }

    
    private List<Entity> _EntityList = new List<Entity>();
    public List<Entity> EntityList { get { return _EntityList; } set { _EntityList = value; OnPropertyChanged(nameof(EntityList)); } }


    public EntityHomeVM(INavigationService navigation)
    {
        _navigation = navigation;

        _entityRepository = new EntityRepository();
        EntityList = _entityRepository.GetListBasic();

        ShowCreateDialogCommand = new RellayCommand(obj =>
        {
            var dialog = new EntityCreateDialog(navigation);
            dialog.ShowDialog();
        });

        ShowUpdateDialogCommand = new RellayCommand(obj =>
        {
            StateStatics.EntityUpdateId = (int)obj;
            var dialog = new EntityUpdateDialog(navigation);
            dialog.ShowDialog();
        });

        ShowDetailCommand = new RellayCommand(obj =>
        {
            StateStatics.EntityDetailId = (int)obj;
            _navigation.NavigateTo<EntityDetailVM>();
        });

        DeleteCommand = new RellayCommand(obj =>
        {
            _entityRepository.Delete((int)obj);

            EntityList = _entityRepository.GetListBasic();
        });
    }
}
