using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShadowMaze.Model
{
    public class Cell
    {
        public bool IsWall { get; set; }
        public bool IsExit { get; set; }

        public Cell(bool isWall = false)
        {
            IsWall = isWall;
            IsExit = false;
        }
    }
}