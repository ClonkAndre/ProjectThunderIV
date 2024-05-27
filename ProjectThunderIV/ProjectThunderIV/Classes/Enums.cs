namespace ProjectThunderIV.Classes
{
    internal enum ThunderstormProgress
    {
        Starting,
        Ongoing,
        Ending
    }

    internal enum TriggerType
    {
        /// <summary>
        /// Player has to look at position for the lightning bolt to appear at the predefined location.
        /// </summary>
        LookAt,

        /// <summary>
        /// Player has to be within the predefined radius at the position inorder for the lightning bolt to appear at the predefined location.
        /// </summary>
        BeAt
    }
}
