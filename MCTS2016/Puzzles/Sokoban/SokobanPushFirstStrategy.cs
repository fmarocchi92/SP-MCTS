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
    class SokobanPushFirstStrategy : ISPSimulationStrategy
    {
        private MersenneTwister rnd;
        public SokobanPushFirstStrategy(MersenneTwister rng)
        {
            rnd = rng;
        }

        public string getFriendlyName()
        {
            return "Sokoban - PushFirst Strategy";
        }

        public string getTypeName()
        {
            return GetType().Name;
        }

        public IPuzzleMove selectMove(IPuzzleState gameState)
        {
            if(rnd.NextDouble() < 0.2)
            {
                return gameState.GetRandomMove();
            }
            List<IPuzzleMove> moves = gameState.GetMoves();
            double bestScore = gameState.GetResult();
            IPuzzleMove bestMove = null;
            foreach(IPuzzleMove move in moves)
            {
                IPuzzleState state = gameState.Clone();
                state.DoMove(move);
                if(state.GetResult() > bestScore)
                {
                    bestMove = move;
                }
            }
            if(bestScore == gameState.GetResult())
            {
                return gameState.GetRandomMove();
            }
            else
            {
                return bestMove;
            }
        }
    }
}
