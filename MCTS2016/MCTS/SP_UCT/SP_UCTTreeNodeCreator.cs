using Common;
using Common.Abstract;
using MCTS.Standard.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCTS2016.MCTS.SP_UCT
{
    public class SP_UCTTreeNodeCreator : ITreeNodeCreator
    {
        private double const_C;
        private double const_D;
        private MersenneTwister rnd;
        public SP_UCTTreeNodeCreator(double constant, double const_D, MersenneTwister rng)
        {
            rnd = rng;
            this.const_C = constant;
            this.const_D = const_D;
        }

        public ITreeNode GenRootNode(IGameState rootState)
        {
            return new SP_UCTTreeNode(null, null, rootState, rnd,const_C,const_D);
        }

        public override string ToString()
        {
            return "SP_UCT-C" + const_C+"-D"+const_D;
        }
    }
}
