namespace Game.Weapons
{
    /// <summary>
    /// Contract for world targets that can be gathered by the collect weapon.
    /// </summary>
    public interface ICollectible
    {
        bool CanCollect { get; }
        bool TryCollect();
    }
}
