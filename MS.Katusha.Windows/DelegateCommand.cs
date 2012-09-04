using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MS.Katusha.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Input;

    namespace Utilities
    {
        public class DelegateCommand : ICommand
        {
            private readonly Action executeMethod;
            private readonly Func<bool> canExecuteMethod;

            public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod)
            {
                if (executeMethod == null)
                    throw new ArgumentNullException("executeMethod");
                if (canExecuteMethod == null)
                    throw new ArgumentNullException("canExecuteMethod");

                this.executeMethod = executeMethod;
                this.canExecuteMethod = canExecuteMethod;
            }

            /* http://joshsmithonwpf.wordpress.com/2008/06/17/allowing-commandmanager-to-query-your-icommand-objects/
             * Josh smith says to do this:
             * */
            public event EventHandler CanExecuteChanged
            {
                add { } remove { }
                //add { CommandManager.RequerySuggested += value; }
                //remove { CommandManager.RequerySuggested -= value; }
            }
            /* ...instead of this
            public event System.EventHandler CanExecuteChanged;
            */
            public bool CanExecute(object stupid)
            {
                return CanExecute();
            }

            public bool CanExecute()
            {
                return canExecuteMethod();
            }

            public void Execute(object parameter)
            {
                Execute();
            }

            public void Execute()
            {
                executeMethod();
            }

            public void OnCanExecuteChanged()
            {
                //CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
