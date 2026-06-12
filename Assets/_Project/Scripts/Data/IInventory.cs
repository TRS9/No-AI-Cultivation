namespace CultivationGame.Data
{
    public interface IInventory
    {
        void AddItem(ItemData item);

        /// <summary>Adds multiple items at once, raising a single change notification.</summary>
        void AddItem(ItemData item, int amount);
    }
}
