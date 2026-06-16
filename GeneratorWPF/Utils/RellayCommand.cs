using System.Windows.Input;

namespace GeneratorWPF.Utils
{
    public class RellayCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;   
        private readonly Action<object> _execute;
        private readonly Func<object?, bool> _canExecute;

        public RellayCommand(Action<object> execute, Func<object?, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }


        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }
    }
}
