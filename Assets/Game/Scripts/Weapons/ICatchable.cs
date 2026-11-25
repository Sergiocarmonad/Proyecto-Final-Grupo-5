namespace Game.Weapons
{
    /// <summary>
    /// Contract for world targets that can be caught by the hunting weapon.
    /// </summary>
    public interface ICatchable
    {
        bool CanCatch { get; }
        bool TryCatch();
    }
}
