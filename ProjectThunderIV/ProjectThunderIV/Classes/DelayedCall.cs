using System;

namespace ProjectThunderIV.Classes
{
    internal class DelayedCall
    {
        #region Variables
        public Action ActionToExecute;
        public TimeSpan ExecuteIn;
        public bool CanExecuteWhenUninitializing;
        public string Tag;
        #endregion

        #region Constructor
        public DelayedCall(TimeSpan executeIn, Action a, bool canExecuteWhenUninitializing, string tag)
        {
            ActionToExecute = a;
            ExecuteIn = executeIn;
            CanExecuteWhenUninitializing = canExecuteWhenUninitializing;
            Tag = tag;
        }
        #endregion
    }
}
