using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShadowMaze.Model
{
    public class Player
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int MemoriesLeft { get; set; }  
        public int VisionRadius { get; set; }

        public Player(int startX, int startY, int memoryCapacity = 50)
        {
            X = startX;
            Y = startY;
            MemoriesLeft = memoryCapacity;
            VisionRadius = 2;
        }
    }
}