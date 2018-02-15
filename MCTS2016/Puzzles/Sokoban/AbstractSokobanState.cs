using Common;
using MCTS2016.BFS;
using MCTS2016.Common.Abstract;
using MCTS2016.Puzzles.SameGame;
using MCTS2016.SP_MCTS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCTS2016.Puzzles.Sokoban
{
    class AbstractSokobanState:IPuzzleState
    {
        private SokobanGameState state;
        private List<IPuzzleMove> availableMoves;
        private Position normalizedPlayerPosition;
        private ISPSimulationStrategy simulationStrategy;
        private RewardType rewardType;


        public AbstractSokobanState(SokobanGameState state, RewardType rewardType, ISPSimulationStrategy simulationStrategy = null)
        {
            Init(state, rewardType, simulationStrategy);
        }   

        public AbstractSokobanState(String level, RewardType rewardType, ISPSimulationStrategy simulationStrategy = null)
        {
            Init(new SokobanGameState(level, rewardType, simulationStrategy), rewardType, simulationStrategy);
        }

        private void Init (SokobanGameState state, RewardType rewardType, ISPSimulationStrategy simulationStrategy)
        {
            this.state = (SokobanGameState)state;
            if (simulationStrategy == null)
            {
                simulationStrategy = new SokobanRandomStrategy();
            }
            this.rewardType = rewardType;
            this.simulationStrategy = simulationStrategy;
            normalizedPlayerPosition = new Position(int.MaxValue, int.MaxValue);
            availableMoves = null;
        }

        public int size => ((IPuzzleState)state).size;

        public IPuzzleState Clone()
        {
            IPuzzleState clone = new AbstractSokobanState((SokobanGameState)state.Clone(), rewardType, simulationStrategy);
            return clone;
        }

        public void DoMove(IPuzzleMove move)
        {
            DoAbstractMove(move);
            availableMoves = null;
            availableMoves = GetMoves();
        }

        public bool EndState()
        {
            return ((IPuzzleState)state).EndState();
        }

        public override bool Equals(object obj)
        {

            return obj!= null && obj.GetType().Equals(GetType()) && this.GetHashCode()==obj.GetHashCode();
        }

        public List<int> GetBoard()
        {
            return ((IPuzzleState)state).GetBoard();
        }

        public int GetBoard(int x, int y)
        {
            return ((IPuzzleState)state).GetBoard(x, y);
        }

        public override int GetHashCode()
        {
            int hc = state.GetEmptyBoardHash();
            if (normalizedPlayerPosition.X == int.MaxValue)
            {
                GetMoves();//this is because normalizedPlayer position is updated during the move search
            }
            hc = (13 * hc) + normalizedPlayerPosition.GetHashCode();
            return hc;
            //return state.GetHashCode();
        }

        public List<IPuzzleMove> GetMoves()
        {
            if (availableMoves != null)
            {
                return availableMoves;
            }
            return GetAvailablePushes();
        }

        public int GetPositionIndex(int x, int y)
        {
            return ((IPuzzleState)state).GetPositionIndex(x, y);
        }

        public IPuzzleMove GetRandomMove()
        {
            List<IPuzzleMove> moves = GetMoves();
            int rndIndex = RNG.Next(moves.Count);//TODO if multithread use own rng
            return moves[rndIndex];
        }

        public double GetResult()
        {
            return ((IPuzzleState)state).GetResult();
        }

        public int GetScore()
        {
            return ((IPuzzleState)state).GetScore();
        }

        public IPuzzleMove GetSimulationMove()
        {
            return simulationStrategy.selectMove(this);
        }

        public bool isTerminal()
        {
            if(GetMoves().Count() == 0)
            {
                return true;
            }
            return ((IPuzzleState)state).isTerminal();
        }

        public void Pass()
        {
            ((IPuzzleState)state).Pass();
        }

        public string PrettyPrint()
        {
            string s = ((IPuzzleState)state).PrettyPrint();
            s += normalizedPlayerPosition;
            s += "\n";
            foreach (IPuzzleMove m in GetMoves())
            {
                s += m+" - ";
            }
            return s;
        }

        public void Restart()
        {
            ((IPuzzleState)state).Restart();
        }

        public bool StateChanged()
        {
            return ((IPuzzleState)state).StateChanged();
        }

        public override string ToString()
        {
            return PrettyPrint();
        }

        public int Winner()
        {
            return ((IPuzzleState)state).Winner();
        }


        private void DoAbstractMove(IPuzzleMove move)
        {
            SokobanPushMove pushMove = (SokobanPushMove)move;
            foreach(SokobanGameMove m in pushMove.MoveList)
            {
                state.DoMove(m);
            }
            state.DoMove(pushMove.PushMove);
        }

        private List<IPuzzleMove> GetAvailablePushes()
        {
            Position normalizedPosition = new Position(int.MaxValue, int.MaxValue);
            HashSet<SokobanGameState> visitedStates = new HashSet<SokobanGameState>();
            List<IPuzzleMove> pushes = new List<IPuzzleMove>();
            List<BFSNodeState> frontier = new List<BFSNodeState>();
            frontier.Add(new BFSNodeState(state, null, null));
            while (frontier.Count() > 0)
            {
                BFSNodeState currentNode = frontier[0];
                SokobanGameState s = (SokobanGameState)currentNode.state;
                if (s.PlayerY < normalizedPosition.Y || s.PlayerY == normalizedPosition.Y && s.PlayerX < normalizedPosition.X)
                {
                    normalizedPosition.X = s.PlayerX;
                    normalizedPosition.Y = s.PlayerY;
                }
                frontier.RemoveAt(0);
                visitedStates.Add(s);

                foreach (SokobanGameMove move in s.GetMoves())
                {
                    SokobanGameState sCopy = (SokobanGameState)s.Clone();
                    if (move.move < 4)//Movement move
                    {
                        sCopy.DoMove(move);
                        if (!visitedStates.Contains(sCopy))//only expand on unvisited states
                        {
                            visitedStates.Add(sCopy);
                            frontier.Add(new BFSNodeState(sCopy, move, currentNode));
                        }
                    }
                    else//Push move
                    {
                        List<SokobanGameMove> movesToPush = new List<SokobanGameMove>() { };
                        BFSNodeState node = currentNode;
                        while (node.parent != null)//build move sequence that lead to push move
                        {
                            movesToPush.Add((SokobanGameMove)node.move);
                            node = node.parent;
                        }
                        movesToPush.Reverse();
                        pushes.Add(new SokobanPushMove(move, movesToPush, new Position(sCopy.PlayerX, sCopy.PlayerY))); //add push move to available pushes
                    }
                }
            }
            normalizedPlayerPosition = normalizedPosition;
            return pushes;
        }
    }
}
