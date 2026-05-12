using System.Collections.Generic;
using System.Linq;

namespace ShadowMaze.Model
{
    public class MemorySystem
    {
        private List<(int x, int y)> rememberedCells; // список, где последний элемент — самый свежий
        private int capacity;

        public MemorySystem(int capacity)
        {
            this.capacity = capacity;
            rememberedCells = new List<(int x, int y)>();
        }

        // добавляет клетку или обновляет её позицию в памяти
        public void Add(int x, int y)
        {
            (int, int) cell = (x, y);

            // если уже помним — удаляем старую запись
            if (rememberedCells.Contains(cell))
                rememberedCells.Remove(cell);
            else if (rememberedCells.Count >= capacity)
                // удаляем самую старую (первую в списке)
                rememberedCells.RemoveAt(0);

            // добавляем в конец как самую свежую
            rememberedCells.Add(cell);
        }

        // проверяет, находится ли клетка в памяти
        public bool IsRemembered(int x, int y)
        {
            return rememberedCells.Contains((x, y));
        }
        public int Count => rememberedCells.Count;
    }
}