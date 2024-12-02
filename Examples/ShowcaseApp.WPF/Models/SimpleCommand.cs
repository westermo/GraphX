using System;
using System.Windows.Input;

namespace ShowcaseApp.WPF.Models
{
    public class SimpleCommand(Predicate<object> can, Action<object> ex) : ICommand
    {
       public Predicate<object> CanExecuteDelegate { get; set; } = can;
       public Action<object> ExecuteDelegate { get; set; } = ex;

       #region ICommand Members
    
       public bool CanExecute(object parameter)
       {
           if (CanExecuteDelegate != null)
               return CanExecuteDelegate(parameter);
           return true;// if there is no can execute default to true
       }
    
       public event EventHandler CanExecuteChanged
       {
           add => CommandManager.RequerySuggested += value;
           remove => CommandManager.RequerySuggested -= value;
       }
    
       public void Execute(object parameter)
       {
           ExecuteDelegate?.Invoke(parameter);
       }
    
       #endregion
   }
}
