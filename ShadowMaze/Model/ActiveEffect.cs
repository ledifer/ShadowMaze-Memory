using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShadowMaze.Model
{
    public class ActiveEffect
    {
        public ItemType Type { get; }
        public int RemainingTicks { get; set; }

        public ActiveEffect(ItemType type, int durationTicks)
        {
            Type = type;
            RemainingTicks = durationTicks;
        }
    }
}
