using System;

namespace ProjectThunderIV.Classes
{
    internal class DelayedCall
    {
        #region Variables
        public Action ActionToExecute;
        public DateTime ExecuteAt;
        public bool CanExecuteWhenUninitializing;
        #endregion

        #region Constructor
        public DelayedCall(DateTime executeAt, Action a, bool canExecuteWhenUninitializing)
        {
            ActionToExecute = a;
            ExecuteAt = executeAt;
            CanExecuteWhenUninitializing = canExecuteWhenUninitializing;
        }
        #endregion
    }
}
