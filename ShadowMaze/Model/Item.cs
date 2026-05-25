namespace ShadowMaze.Model
{
    public enum ItemType
    {
        MemoryBoost,   // увеличивает лимит памяти
        VisionBoost,   // увеличивает радиус обзора
        SlowCrystal    // замедляет врагов на время
    }

    public class Item
    {
        public int X { get; }
        public int Y { get; }
        public ItemType Type { get; }

        public Item(int x, int y, ItemType type)
        {
            X = x;
            Y = y;
            Type = type;
        }
    }
}