﻿using Common.Abstract;
using MCTS.Standard.Utils;
using MCTS.Standard.Utils.UCT;
using MCTS2016.MCTS;
using MCTS2016.MCTS.SP_UCT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCTS2016.Puzzles.SameGame
{
    class SamegameMCTSStrategy : IMCTSSimulationStrategy
    {
        private SP_MCTSAlgorithm mcts;

        private double maxTimeInMinutes;

        public int iterations { get; set; }

        public SamegameMCTSStrategy(int iterations = 1000, double maxTimeInMinutes = 5, SP_MCTSAlgorithm mcts = null)
        {
            if (mcts == null)
            {
                mcts = new SP_MCTSAlgorithm(new SP_UCTTreeNodeCreator());
            }
            this.mcts = mcts;
            this.iterations = iterations;
            this.maxTimeInMinutes = maxTimeInMinutes;
        }

        public string getFriendlyName()
        {
            return string.Format("MCTS[{0}]", this.iterations);
        }

        public string getTypeName()
        {
            return GetType().Name;
        }

        public IGameMove selectMove(IGameState gameState)
        {
            return mcts.Search(gameState, iterations, maxTimeInMinutes);
        }
    }
}
