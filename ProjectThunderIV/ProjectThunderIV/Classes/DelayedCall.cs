using System;

namespace ProjectThunderIV.Classes
{
    internal class DelayedCall
    {
        #region Variables
        public Action ActionToExecute;
        public DateTime ExecuteAt;
        #endregion

        #region Constructor
        public DelayedCall(DateTime executeAt, Action a)
        {
            ActionToExecute = a;
            ExecuteAt = executeAt;
        }
        #endregion
    }
}
