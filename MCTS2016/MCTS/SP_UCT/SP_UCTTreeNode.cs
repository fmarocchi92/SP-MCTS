using System;
using System.Collections.Generic;
using Common.Abstract;
using MCTS.Standard.Utils;

namespace MCTS2016.MCTS.SP_UCT
{
    public class SP_UCTTreeNode : ITreeNode
    {
        private static Random random = new Random();
        private SP_UCTTreeNode parent;
        private IGameMove move;
        protected List<SP_UCTTreeNode> childNodes;
        protected List<IGameMove> untriedMoves;
        private double wins;
        private double score;
        protected int visits;
        private int playerWhoJustMoved;
        protected double const_C;
        protected double const_D;
        private double squaredReward;
        private double topScore;

        public SP_UCTTreeNode(IGameMove move, SP_UCTTreeNode parent, IGameState state, double const_C = 1, double const_D = 20000, bool generateUntriedMoves = true)
        {
            this.move = move;
            this.parent = parent;
            this.const_C = const_C;
            this.const_D = const_D;
            childNodes = new List<SP_UCTTreeNode>();
            wins = 0;
            visits = 0;
            squaredReward = 0;
            topScore = 0;
            playerWhoJustMoved = state.playerJustMoved;
            if (generateUntriedMoves)
            {
                untriedMoves = state.GetMoves();
            }
        }

        public int PlayerWhoJustMoved
        {
            get { return playerWhoJustMoved; }
        }

        public ITreeNode Parent
        {
            get { return parent; }
        }

        public IGameMove Move
        {
            get { return move; }
        }

        public ITreeNode SelectChild()
        {
            childNodes.Sort((x, y) => -x.SP_UCTValue().CompareTo(y.SP_UCTValue()));
            return childNodes[0];
        }

        private double SP_UCTValue()
        {            
            return wins / visits + const_C * Math.Sqrt(2 * Math.Log(parent.visits) / visits) 
                + Math.Sqrt((squaredReward - visits * Math.Pow(wins / visits, 2) + const_D) / visits);
        }

        public virtual ITreeNode AddChild(IGameMove move, IGameState state)
        {
            SP_UCTTreeNode n = new SP_UCTTreeNode(move, this, state, const_C);
            untriedMoves.Remove(move);
            childNodes.Add(n);
            return n;
        }

        public void Update(double result)
        {
            visits++;
            wins += result;
            score += result;
            squaredReward += result * result;
            topScore = Math.Max(topScore, result);
        }

        public IGameMove SelectUntriedMove()
        {
            return untriedMoves[random.Next(untriedMoves.Count)];
        }

        /// <summary>
        /// Returns the move with the highest final topScore
        /// </summary>
        /// <returns></returns>
        public IGameMove GetBestMove()
        {
            childNodes.Sort((x, y) => -x.topScore.CompareTo(y.topScore));
            return childNodes.Count > 0 ? childNodes[0].move : (IGameMove)(0 - 1);
        }

        public bool HasMovesToTry()
        {
            return untriedMoves.Count != 0;
        }

        public bool HasChildren()
        {
            return childNodes.Count != 0;
        }

        public override string ToString()
        {
            return "[M:" + move + " W/V:" + wins + "/" + visits + "]";
        }

        public string TreeToString(int indent)
        {
            string s = IndentString(indent) + ToString();
            foreach (SP_UCTTreeNode c in childNodes)
            {
                s += c.TreeToString(indent + 1);
            }
            return s;
        }

        public string ChildrenToString()
        {
            string s = "";
            foreach (SP_UCTTreeNode c in childNodes)
            {
                s += c + "\n";
            }
            return s;
        }

        private string IndentString(int indent)
        {
            string s = "\n";
            for (int i = 1; i < indent + 1; i++)
            {
                s += "| ";
            }
            return s;
        }
    }
}
