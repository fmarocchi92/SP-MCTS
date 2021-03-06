﻿using Common;
using MCTS2016.BFS;
using MCTS2016.Common.Abstract;
using MCTS2016.Puzzles.SameGame;
using MCTS2016.SP_MCTS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private bool useNormalizedPosition;
        private MersenneTwister rng;

        public AbstractSokobanState(SokobanGameState state, RewardType rewardType, bool useNormalizedPosition, ISPSimulationStrategy simulationStrategy = null, MersenneTwister rng = null)
        {
            Init(state, rewardType, useNormalizedPosition, simulationStrategy, rng);
        }   

        public AbstractSokobanState(String level, RewardType rewardType, bool useNormalizedPosition, ISPSimulationStrategy simulationStrategy = null, MersenneTwister rng = null)
        {
            Init(new SokobanGameState(level, rewardType, simulationStrategy), rewardType, useNormalizedPosition, simulationStrategy, rng);
        }

        private void Init (SokobanGameState state, RewardType rewardType, bool useNormalizedPosition, ISPSimulationStrategy simulationStrategy, MersenneTwister rng = null)
        {
            this.useNormalizedPosition = useNormalizedPosition;
            this.state = (SokobanGameState)state;
            if (simulationStrategy == null)
            {
                simulationStrategy = new SokobanRandomStrategy();
            }
            if (rng == null)
            {
                rng = new MersenneTwister();
            }
            this.rng = rng;
            this.rewardType = rewardType;
            this.simulationStrategy = simulationStrategy;
            normalizedPlayerPosition = new Position(int.MaxValue, int.MaxValue);
            availableMoves = null;
        }

        public int size => ((IPuzzleState)state).size;

        public IPuzzleState Clone()
        {
            IPuzzleState clone = new AbstractSokobanState((SokobanGameState)state.Clone(), rewardType, useNormalizedPosition, simulationStrategy, rng);
            return clone;
        }

        public void DoMove(IPuzzleMove move)
        {
            DoAbstractMove(move);
            availableMoves = null;
            //availableMoves = GetMoves();
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
            if (useNormalizedPosition)
            {
                int hc = state.GetEmptyBoardHash();
                if (normalizedPlayerPosition.X == int.MaxValue)
                {
                    GetMoves();//this is necessary because normalizedPlayer position is updated during the move search (to ensure that the value is assigned)
                }
                hc = (13 * hc) + normalizedPlayerPosition.GetHashCode();
                return hc;
            }
            else
            {
                return state.GetHashCode();
            }
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
            int rndIndex = rng.Next(moves.Count);//TODO if multithread use own rng
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
                s += m;
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
                        movesToPush.Add(move);
                        List<SokobanGameMove> tunnel = GetTunnelMacro((SokobanGameState)s, move);
                        if(tunnel.Count > 0)
                        {
                            movesToPush.AddRange(tunnel);
                        }
                        pushes.Add(new SokobanPushMove(movesToPush, new Position(sCopy.PlayerX, sCopy.PlayerY))); //add push move to available pushes
                    }
                }
            }
            normalizedPlayerPosition = normalizedPosition;
            return pushes;
        }

        List<SokobanGameMove> GetTunnelMacro(SokobanGameState state, IPuzzleMove push)
        {
            //Debug.WriteLine(state);
            List<SokobanGameMove> macro = new List<SokobanGameMove>();
            Position boxToPush = null;
            Position pushTarget = null;
            
            switch (push.ToString())
            {
                case "U":
                    boxToPush = new Position(state.PlayerX, state.PlayerY - 1);
                    pushTarget = new Position(state.PlayerX, state.PlayerY - 2);
                    break;
                case "D":
                    boxToPush = new Position(state.PlayerX, state.PlayerY + 1);
                    pushTarget = new Position(state.PlayerX, state.PlayerY + 2);
                    break;
                case "R":
                    boxToPush = new Position(state.PlayerX + 1, state.PlayerY);
                    pushTarget = new Position(state.PlayerX + 2, state.PlayerY);
                    break;
                case "L":
                    boxToPush = new Position(state.PlayerX - 1, state.PlayerY);
                    pushTarget = new Position(state.PlayerX - 2, state.PlayerY);
                    break;
            }
            if(boxToPush.X == pushTarget.X &&
                state.Board[boxToPush.X + 1, boxToPush.Y] == SokobanGameState.WALL && state.Board[boxToPush.X -1, boxToPush.Y] == SokobanGameState.WALL) //Vertical push
            {
                while((state.Board[pushTarget.X, pushTarget.Y] == SokobanGameState.EMPTY || state.Board[pushTarget.X, pushTarget.Y] == SokobanGameState.GOAL) &&
                    state.Board[pushTarget.X + 1, pushTarget.Y] == SokobanGameState.WALL && state.Board[pushTarget.X -1, pushTarget.Y] == SokobanGameState.WALL)
                {
                    SokobanGameMove move;
                    if (pushTarget.Y > boxToPush.Y)
                    {
                        move = new SokobanGameMove("D");
                        pushTarget.Y++;
                    }
                    else
                    {
                        move = new SokobanGameMove("U");
                        pushTarget.Y--;
                    }
                    macro.Add(move);
                }
            }
            else if (state.Board[boxToPush.X, boxToPush.Y + 1] == SokobanGameState.WALL && state.Board[boxToPush.X, boxToPush.Y - 1] == SokobanGameState.WALL)//horizontal push
            {
                while ((state.Board[pushTarget.X, pushTarget.Y] == SokobanGameState.EMPTY || state.Board[pushTarget.X, pushTarget.Y] == SokobanGameState.GOAL) &&
                    state.Board[pushTarget.X, pushTarget.Y + 1] == SokobanGameState.WALL && state.Board[pushTarget.X, pushTarget.Y - 1] == SokobanGameState.WALL)
                {
                    SokobanGameMove move;
                    if (pushTarget.X > boxToPush.X)
                    {
                        move = new SokobanGameMove("R");
                        pushTarget.X++;
                    }
                    else
                    {
                        move = new SokobanGameMove("L");
                        pushTarget.X--;
                    }
                    macro.Add(move);
                }

            }
            return macro;
        }
    }
}
