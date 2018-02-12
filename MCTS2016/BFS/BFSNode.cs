using MCTS2016.Common.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCTS2016.BFS
{
    public class BFSNode
    {
        public IPuzzleState state;
        public IPuzzleMove move;
        public BFSNode parent;

        public BFSNode(IPuzzleState state, IPuzzleMove move, BFSNode parent)
        {
            this.state = state;
            this.move = move;
            this.parent = parent;
        }

        internal BFSNode Clone()
        {
            return new BFSNode(state, move, parent);
        }
    }
}
