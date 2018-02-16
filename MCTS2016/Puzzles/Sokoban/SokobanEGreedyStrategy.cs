using MCTS2016.SP_MCTS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCTS2016.Common.Abstract;
using Common;

namespace MCTS2016.Puzzles.Sokoban
{
    public class SokobanEGreedyStrategy : ISPSimulationStrategy
    {
        double epsilon;
        MersenneTwister rng;

        public SokobanEGreedyStrategy(double epsilon, MersenneTwister rng)
        {
            this.epsilon = epsilon;
            this.rng = rng;
        }

        public string getFriendlyName()
        {
            return "Sokoban Epsilon - Greedy Strategy";
        }

        public string getTypeName()
        {
            return GetType().Name;
        }

        public IPuzzleMove selectMove(IPuzzleState gameState)
        {
            List<IPuzzleMove> moves = gameState.GetMoves();
            IPuzzleMove bestMove = null;

            if (rng.NextDouble() > epsilon)
            {
                IPuzzleState clone = gameState.Clone();
                double maxReward = double.MinValue;

                foreach (IPuzzleMove move in clone.GetMoves())
                {
                    clone.DoMove(move);
                    if (clone.GetResult() > maxReward)
                    {
                        maxReward = clone.GetResult();
                        bestMove = move;
                    }
                    clone = gameState.Clone();
                }
            }
            else
            {
                bestMove = gameState.GetRandomMove();
            }
            return bestMove;
        }
    }
}
