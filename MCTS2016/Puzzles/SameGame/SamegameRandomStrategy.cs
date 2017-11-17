using Common.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCTS2016.Puzzles.SameGame
{
    class SamegameRandomStrategy : ISimulationStrategy
    {
        public string getFriendlyName()
        {
            return "Samegame Random";
        }

        public string getTypeName()
        {
            return GetType().Name;
        }

        public IGameMove selectMove(IGameState gameState)
        {
            return gameState.GetRandomMove();
        }
    }
}
