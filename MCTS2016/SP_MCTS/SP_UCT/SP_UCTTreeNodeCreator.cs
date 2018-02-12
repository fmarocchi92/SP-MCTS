using Common;
using Common.Abstract;
using MCTS.Standard.Utils;
using MCTS2016.Common.Abstract;
using MCTS2016.SP_MCTS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCTS2016.SP_MCTS.SP_UCT
{
    public class SP_UCTTreeNodeCreator : ISPTreeNodeCreator
    {
        private double const_C;
        private double const_D;
        private MersenneTwister rnd;
        private bool graphMode;
        public SP_UCTTreeNodeCreator(double constant, double const_D, MersenneTwister rng, bool graphMode = true)
        {
            rnd = rng;
            this.const_C = constant;
            this.const_D = const_D;
            this.graphMode = graphMode;
        }

        public ISPTreeNode GenRootNode(IPuzzleState rootState)
        {
            return new SP_UCTTreeNode(null, null, rootState, rnd,const_C,const_D,true,graphMode,new HashSet<IPuzzleState>() { rootState });
        }

        public override string ToString()
        {
            return "SP_UCT-C" + const_C+"-D"+const_D;
        }
    }
}
