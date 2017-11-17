using Common;
using Common.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCTS2016.Puzzles.SameGame
{
    class SamegameTabuColorRandomStrategy : ISimulationStrategy
    {
        private int selectedColor;

        public SamegameTabuColorRandomStrategy(SamegameGameState initialState)
        {
            int[] counters = new int[initialState.GetBoard().Count];
            foreach(int value in initialState.GetBoard())
            {
                counters[value]++;
            }
            int max=0;
            int selectedValue=0;
            for(int i = 0; i < counters.Length; i++)
            {
                if (counters[i] > max)
                {
                    max = counters[i];
                    selectedValue = i;
                }
            }
            selectedColor = selectedValue;
        }

        public SamegameTabuColorRandomStrategy(int[][] level)
        {
            int[] counters = new int[level.Length];
            foreach (int[] row in level)
            {
                foreach (int value in row)
                {
                    counters[value]++;
                }
            }
            int max = 0;
            int selectedValue = 0;
            for (int i = 0; i < counters.Length; i++)
            {
                if (counters[i] > max)
                {
                    max = counters[i];
                    selectedValue = i;
                }
            }
            selectedColor = selectedValue;
        }

        public string getFriendlyName()
        {
            return "TabuColorRandom";
        }

        public string getTypeName()
        {
            return GetType().Name;
        }

        public IGameMove selectMove(IGameState gameState)
        {
            List<IGameMove> moves = gameState.GetMoves();
            
            if (RNG.NextDouble() <= 0.00007) //epsilon greedy
            {
                return moves[RNG.Next(moves.Count)];
            }
            moves.RemoveAll(item => gameState.GetBoard(SamegameGameMove.GetX(item), SamegameGameMove.GetY(item))==selectedColor);
            if (moves.Count == 0)
            {
                moves = gameState.GetMoves();
            }
            IGameMove selectedMove = moves[RNG.Next(moves.Count)];
            return selectedMove;
        }
    }
}
