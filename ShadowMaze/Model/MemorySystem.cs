using System.Collections.Generic;
using System.Linq;

namespace ShadowMaze.Model
{
    public class MemorySystem
    {
        private List<(int x, int y)> rememberedCells;
        private int capacity;

        public MemorySystem(int capacity)
        {
            this.capacity = capacity;
            rememberedCells = new List<(int x, int y)>();
        }

        public void Add(int x, int y)
        {
            (int, int) cell = (x, y);

            if (rememberedCells.Contains(cell))
                rememberedCells.Remove(cell);
            else if (rememberedCells.Count >= capacity)
                rememberedCells.RemoveAt(0);

            rememberedCells.Add(cell);
        }

        public bool IsRemembered(int x, int y)
        {
            return rememberedCells.Contains((x, y));
        }

        public void SetCapacity(int newCapacity)
        {
            capacity = newCapacity;
            while (rememberedCells.Count > capacity)
                rememberedCells.RemoveAt(0);
        }

        public int Count => rememberedCells.Count;
    }
}