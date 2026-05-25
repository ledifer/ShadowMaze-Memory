using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShadowMaze.Controller
{
    public class GameController
    {
        private Model.GameModel model;

        public GameController(Model.GameModel model)
        {
            this.model = model;
        }

        public void HandleInput(Model.Direction direction)
        {
            System.Diagnostics.Debug.WriteLine($"Controller received direction: {direction}");
            model.MovePlayer(direction);
        }
    }
}