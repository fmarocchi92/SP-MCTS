﻿using Common;
using Common.Abstract;
using GraphAlgorithms;
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
    class SokobanGameState : IPuzzleState
    {
        public int size { get; set; }//board width
        public int PlayerY { get => playerY;}
        public int PlayerX { get => playerX;}
        public int[,] Board { get => board;}

        private bool stateChanged = false;

        private bool isDeadlock = false;

        public const int EMPTY = 0;
        public const int WALL = 1;
        public const int GOAL = 2;
        public const int BOX = 3;
        public const int BOX_ON_GOAL = 4;
        public const int PLAYER = 5;
        public const int PLAYER_ON_GOAL = 6;
        const int VISITED = 7;

        const string EMPTY_STR = " ";
        const string WALL_STR = "#";
        const string GOAL_STR = ".";
        const string BOX_STR = "$";
        const string BOX_ON_GOAL_STR = "*";
        const string PLAYER_STR = "@";
        const string PLAYER_ON_GOAL_STR = "+";
        //0 → empty
        //1 → wall
        //2 → empty goal
        //3 → box not on goal
        //4 → box on target
        //5 → player
        //6 → player on goal
        private int[,] board;
        private int[,] distanceBoard;

        private int playerX;
        private int playerY;

        private int score;
        private bool win;
        public HashSet<Position> simpleDeadlock;

        private Dictionary<Position, int> distancesFromClosestGoal = new Dictionary<Position, int>();
        private Dictionary<PositionGoalPair, int> distancesFromAllGoals = new Dictionary<PositionGoalPair, int>();
        private List<Position> goals = new List<Position>();
        private RewardType rewardType;
        private ISPSimulationStrategy simulationStrategy;

        public SokobanGameState(String level, RewardType rewardType, ISPSimulationStrategy simulationStrategy = null)
        {
            this.rewardType = rewardType;
            String[] levelRows = level.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            int maxWidth = 0;
            foreach (string row in levelRows)
            {
                maxWidth = Math.Max(maxWidth, row.Length);
            }
            board = new int[maxWidth, levelRows.Length];
            int x, y = 0;
            foreach (string row in levelRows)
            {
                x = 0;
                foreach (char c in row)
                {
                    board[x, y] = TranslateToInternalRepresentation(c.ToString());
                    if (board[x, y] == PLAYER || board[x, y] == PLAYER_ON_GOAL)
                    {
                        playerX = x;
                        playerY = y;
                    }
                    if(board[x,y] == GOAL || board[x, y] == PLAYER_ON_GOAL || board[x, y] == BOX_ON_GOAL)
                    {
                        goals.Add(new Position(x, y));
                    }
                    x++;
                }
                y++;
            }
            size = maxWidth;
            if (simulationStrategy != null)
            {
                this.simulationStrategy = simulationStrategy;
            }
            else
            {
                this.simulationStrategy = new SokobanRandomStrategy();
            }

            simpleDeadlock = FindDeadlockPositions();
        }

        private SokobanGameState()
        {
            board = new int[1, 1];
            this.simulationStrategy = new SokobanRandomStrategy();
        }

        public IPuzzleState Clone()
        {
            return new SokobanGameState()
            {
                size = size,
                board = board.Clone() as int[,],
                playerX = playerX,
                playerY = playerY,
                simulationStrategy = simulationStrategy,
                score = score,
                simpleDeadlock = simpleDeadlock,
                isDeadlock = isDeadlock,
                win = win,
                distancesFromClosestGoal = distancesFromClosestGoal,
                distancesFromAllGoals = distancesFromAllGoals,
                goals = goals,
                rewardType = rewardType
            };
        }
                
        HashSet<Position> FindDeadlockPositions()
        {
            HashSet<Position> deadlockPositions = new HashSet<Position>();
            Position backupPlayer = new Position(playerX, playerY);
            int[,] backupBoard = board.Clone() as int[,];
            distanceBoard = board.Clone() as int[,];
            ResetBoard(true);
            foreach (Position goal in goals)
            {
                ResetBoard(false);
                playerX = goal.X;
                playerY = goal.Y;
                CalculateDistances(goal);
                ResetBoard(false);
                playerX = goal.X;
                playerY = goal.Y;
                Explore(goal.X, goal.Y, 0);
            }
            for (int x = 0; x < board.GetLength(0); x++)
            {
                for (int y = 0; y < board.GetLength(1); y++)
                {
                    if (board[x, y] != VISITED && board[x, y] != WALL && board[x, y] != GOAL)
                    {
                        deadlockPositions.Add(new Position(x, y));
                    }
                }
            }
            //Debug.WriteLine(PrettyPrint());
            //Debug.WriteLine(PrettyPrintDistance());
            board = backupBoard;
            playerX = backupPlayer.X;
            playerY = backupPlayer.Y;
            return deadlockPositions;
        }
        void CalculateDistances(Position goal)
        {
            HashSet<Position> visited = new HashSet<Position>();
            List<PositionCost> frontier = new List<PositionCost>() { new PositionCost(goal,0) };
            PositionCost current;
            board[goal.X, goal.Y] = VISITED;
            while (frontier.Count() != 0)
            {
                //Debug.WriteLine(PrettyPrint());
                current = frontier[0];
                //Debug.WriteLine(current.position);
                frontier.RemoveAt(0);
                if (!distancesFromClosestGoal.TryGetValue(current.position, out int oldValue))
                {
                    distancesFromClosestGoal.Add(current.position, current.cost);
                    distanceBoard[current.position.X, current.position.Y] = current.cost;
                }
                else if (current.cost < oldValue)
                {
                    distancesFromClosestGoal.Remove(current.position);
                    distancesFromClosestGoal.Add(current.position, current.cost);
                    distanceBoard[current.position.X, current.position.Y] = current.cost;
                }
                PositionGoalPair currentPair = new PositionGoalPair(current.position, goal);
                if (!distancesFromAllGoals.TryGetValue(currentPair, out int oldPairValue))
                {
                    distancesFromAllGoals.Add(currentPair, current.cost);

                }
                else if (current.cost < oldPairValue)
                {
                    distancesFromAllGoals.Remove(currentPair);
                    distancesFromAllGoals.Add(currentPair, current.cost);
                }
                playerX = current.position.X + 1;
                playerY = current.position.Y;
                Position child = new Position(playerX, playerY);
                if (!visited.Contains(child) && board[playerX, playerY] != WALL)
                {
                    frontier.Add(new PositionCost(child, current.cost + 1));
                    visited.Add(child);
                }
                playerX = current.position.X - 1;
                playerY = current.position.Y;
                child = new Position(playerX, playerY);
                if (!visited.Contains(child) && board[playerX, playerY] != WALL)
                {
                    frontier.Add(new PositionCost(child, current.cost + 1));
                    visited.Add(child);
                }
                playerX = current.position.X;
                playerY = current.position.Y + 1;
                child = new Position(playerX, playerY);
                if (!visited.Contains(child) && board[playerX, playerY] != WALL)
                {
                    frontier.Add(new PositionCost(child, current.cost + 1));
                    visited.Add(child);
                }
                playerX = current.position.X;
                playerY = current.position.Y - 1;
                child = new Position(playerX, playerY);
                if (!visited.Contains(child) && board[playerX, playerY] != WALL)
                {
                    frontier.Add(new PositionCost(child, current.cost + 1));
                    visited.Add(child);
                }
            }
        }



        void Explore(int x, int y, int depth)
        {
            Position currentPosition = new Position(x, y);
            
            if (board[x + 1, y] != WALL)
            {
                playerX = x + 1;
                playerY = y;
                bool pulled = PullBox(1, 0);
                if (pulled)
                {
                    Explore(x + 1, y, depth + 1);
                }
            }
            if (board[x, y + 1] != WALL)
            {
                playerX = x;
                playerY = y + 1;
                bool pulled = PullBox(0, 1);
                if (pulled)
                {
                    Explore(x, y + 1, depth + 1);
                }
            }
            if (board[x - 1, y] != WALL)
            {
                playerX = x - 1;
                playerY = y;
                bool pulled = PullBox(-1, 0);
                if (pulled)
                {
                    Explore(x - 1, y, depth + 1);
                }
            }
            if (board[x, y - 1] != WALL)
            {
                playerX = x;
                playerY = y - 1;
                bool pulled = PullBox(0, -1);
                if (pulled)
                {
                    Explore(x, y - 1, depth + 1);
                }
            }
        }

        void ResetBoard(bool fullReset=false)
        {
            for (int x = 0; x < board.GetLength(0); x++)
            {
                for (int y = 0; y < board.GetLength(1); y++)
                {
                    if (board[x, y] == BOX || board[x, y] == PLAYER)
                        board[x, y] = EMPTY;
                    if (board[x, y] == BOX_ON_GOAL || board[x, y] == PLAYER_ON_GOAL)
                        board[x, y] = GOAL;
                    if (fullReset && board[x, y] == VISITED)
                        board[x, y] = EMPTY;
                }
            }
        }

        public void DoMove(IPuzzleMove move)
        {
            DoBasicMove(move);
        }

        private void DoBasicMove(IPuzzleMove move)
        {
            stateChanged = false;
            switch (move.ToString())
            {
                case "D":
                    PushBox(0, 1);
                    break;
                case "R":
                    PushBox(1, 0);
                    break;
                case "U":
                    PushBox(0, -1);
                    break;
                case "L":
                    PushBox(-1, 0);
                    break;
                case "d":
                    MovePlayer(playerX, playerY + 1);
                    break;
                case "r":
                    MovePlayer(playerX + 1, playerY);
                    break;
                case "u":
                    MovePlayer(playerX, playerY - 1);
                    break;
                case "l":
                    MovePlayer(playerX - 1, playerY);
                    break;
                default:
                    break;
            }
        }

        private void MovePlayer(int x, int y)
        {
            if (board[x, y] == EMPTY)
            {
                board[x, y] = PLAYER;
                stateChanged = true;
            }
            else if (board[x, y] == GOAL)
            {
                board[x, y] = PLAYER_ON_GOAL;
                stateChanged = true;
            }
            if (stateChanged && board[playerX, playerY] == PLAYER)
            {
                board[playerX, playerY] = EMPTY;
            }
            else if (stateChanged && board[playerX, playerY] == PLAYER_ON_GOAL)
            {
                board[playerX, playerY] = GOAL;
            }
            if (stateChanged)
            {
                playerX = x;
                playerY = y;
            }
        }

        private void PushBox(int xDirection, int yDirection)
        {
            if (board[playerX + 2 * xDirection, playerY + 2 * yDirection] == EMPTY)
            {
                board[playerX + 2 * xDirection, playerY + 2 * yDirection] = BOX;
                stateChanged = true;
            }
            else if (board[playerX + 2 * xDirection, playerY + 2 * yDirection] == GOAL)
            {
                stateChanged = true;
                board[playerX + 2 * xDirection, playerY + 2 * yDirection] = BOX_ON_GOAL;
            }
            if (stateChanged)
            {
                if (board[playerX + xDirection, playerY + yDirection] == BOX)
                {
                    board[playerX + xDirection, playerY + yDirection] = PLAYER;
                }
                else if (board[playerX + xDirection, playerY + yDirection] == BOX_ON_GOAL)
                {
                    board[playerX + xDirection, playerY + yDirection] = PLAYER_ON_GOAL;
                }
                if (board[playerX, playerY] == PLAYER)
                {
                    board[playerX, playerY] = EMPTY;
                }
                else if (board[playerX, playerY] == PLAYER_ON_GOAL)
                {
                    board[playerX, playerY] = GOAL;
                }
                playerX += xDirection;
                playerY += yDirection;
                isDeadlock = CheckDeadlock(playerX + xDirection, playerY + yDirection, new HashSet<Position>(), new HashSet<Position>());
            }
        }

        bool CheckDeadlock(int x, int y, HashSet<Position> checkedBoxes, HashSet<Position> frozenBoxes)
        {
            if (simpleDeadlock.Contains(new Position(x, y)))
            {
                return true;
            }
            bool horizontalLock = false;
            bool verticalLock = false;
            checkedBoxes.Add(new Position(x, y));
            frozenBoxes.Add(new Position(x, y));
            horizontalLock = CheckHorizontalLock(x, y, checkedBoxes, frozenBoxes);
            verticalLock = CheckVerticalLock(x, y, checkedBoxes, frozenBoxes);
            if (verticalLock && horizontalLock)
            {
                frozenBoxes.Add(new Position(x, y));
                foreach(Position p in frozenBoxes)
                {
                    if (board[p.X, p.Y] != BOX_ON_GOAL)
                        return true;
                }
            }
            else
            {
                frozenBoxes.Remove(new Position(x, y));
            }
            return false;
        }

        bool CheckHorizontalLock(int x, int y, HashSet<Position> checkedBoxes, HashSet<Position> frozenBoxes)
        {
            Position right = new Position(x + 1, y);
            Position left = new Position(x - 1, y);
            bool horizontalLock = false;
            if (board[x + 1, y] == WALL || board[x - 1, y] == WALL || simpleDeadlock.Contains(right) && simpleDeadlock.Contains(left) || checkedBoxes.Contains(right) || checkedBoxes.Contains(left))
            {
                horizontalLock = true;
            }
            if (board[x + 1, y] == BOX || board[x + 1, y] == BOX_ON_GOAL)
            {
                if (!checkedBoxes.Contains(right))
                {
                    if (CheckDeadlock(x + 1, y, checkedBoxes, frozenBoxes))
                        horizontalLock = true;
                }
                else
                {
                    if (frozenBoxes.Contains(right))
                        horizontalLock = true;
                }
            }
            if (board[x - 1, y] == BOX || board[x - 1, y] == BOX_ON_GOAL)
            {
                if (!checkedBoxes.Contains(left))
                {
                    if (CheckDeadlock(x - 1, y, checkedBoxes, frozenBoxes))
                        horizontalLock = true;
                }
                else
                {
                    if (frozenBoxes.Contains(left))
                        horizontalLock = true;
                }
            }
            return horizontalLock;
        }

        bool CheckVerticalLock(int x, int y, HashSet<Position> checkedBoxes, HashSet<Position> frozenBoxes)
        {
            bool verticalLock = false;
            Position up = new Position(x, y + 1);
            Position down = new Position(x, y - 1);

            if (board[x, y + 1] == WALL || board[x, y - 1] == WALL || simpleDeadlock.Contains(up) && simpleDeadlock.Contains(down) || checkedBoxes.Contains(up) || checkedBoxes.Contains(down))
            {
                verticalLock = true;
            }

            if (board[x, y + 1] == BOX || board[x, y + 1] == BOX_ON_GOAL)
            {
                if (!checkedBoxes.Contains(up))
                {
                    if (CheckDeadlock(x, y + 1, checkedBoxes, frozenBoxes))
                        verticalLock = true;
                }
                else
                {
                    if (frozenBoxes.Contains(up))
                        verticalLock = true;
                }
            }
            if (board[x, y - 1] == BOX || board[x, y - 1] == BOX_ON_GOAL)
            {
                if (!checkedBoxes.Contains(down))
                {
                    if (CheckDeadlock(x, y - 1, checkedBoxes, frozenBoxes))
                        verticalLock = true;
                }
                else
                {
                    if (frozenBoxes.Contains(down))
                        verticalLock = true;
                }
            }
            return verticalLock;
        }

            /// <summary>
            /// Input: Direction towards which I pull
            /// Returns whether or not it could pull towards the new position
            /// </summary>
            /// <param name="xDirection"></param>
            /// <param name="yDirection"></param>
            /// <returns>Whether or not it could pull towards the new position</returns>
        private bool PullBox(int xDirection, int yDirection)
        {
            if (board[playerX, playerY] != VISITED &&
                (board[playerX + xDirection, playerY + yDirection] == EMPTY || board[playerX + xDirection, playerY + yDirection] == GOAL || board[playerX + xDirection, playerY + yDirection] == VISITED))
            {
                board[playerX, playerY] = VISITED;
                playerX += xDirection;
                playerY += yDirection;
                return true;
            }
            return false;
        }


        public bool EndState()
        {
            CheckWinCondition();
            return win;
        }

        public bool CheckWinCondition()
        {
            bool win = true;
            //check win condition
            for (int y = 0; y < board.GetLength(1); y++)
            {
                for (int x = 0; x < board.GetLength(0); x++)
                {
                    if (board[x, y] == BOX) //box not on goal
                    {
                        win = false;
                        break;
                    }
                }
                if (!win)
                    break;
            }
            this.win = win;
            return win;
        }

        public List<int> GetBoard()
        {
            List<int> boardList = new List<int>();
            for (int y = 0; y < board.GetLength(1); y++)
            {
                for (int x = 0; x < board.GetLength(0); x++)
                {
                    boardList.Add(board[x, y]);
                }
            }
            return boardList;
        }

        public int GetBoard(int x, int y)
        {
            return board[x, y];
        }

        public List<IPuzzleMove> GetMoves()
        {
            if(isDeadlock || win)
            {
                return new List<IPuzzleMove>();
            }
            return GetBasicMoves();
        }

        public int GetPositionIndex(int x, int y)
        {
            return y * size + x;
        }

        public IPuzzleMove GetRandomMove()
        {
            List<IPuzzleMove> moves = GetMoves();
            int rndIndex = RNG.Next(moves.Count);
            return moves[rndIndex];
        }

        public double GetResult()
        {
            double reward = 0;
            switch (rewardType)
            {
                case RewardType.R0:
                    
                    if (CheckWinCondition())
                    {
                        reward = 1;
                    }
                    else
                    {
                        reward = 0;
                    }
                    break;
                case RewardType.InverseBM:
                    int totalDistance = HungarianDistance();
                    if (totalDistance == 0)
                    {
                        totalDistance = 1;
                    }
                    else if(totalDistance == 1)
                    {
                        totalDistance = 2;
                    }
                    if (isDeadlock)
                    {
                        totalDistance += 1*size;
                    }
                    reward = (1.0 / Math.Sqrt(totalDistance));
                    break;
                case RewardType.NegativeBM:
                    reward = -HungarianDistance();
                    if (isDeadlock)
                    {
                        reward -= 1 * size;
                    }
                    break;
                case RewardType.LogBM:
                    totalDistance = HungarianDistance();
                    if (isDeadlock)
                    {
                        totalDistance += 1 * size;
                    }
                    if (totalDistance == 0)
                    {
                        reward = 1;
                    }
                    else
                    {
                        reward = -Math.Log(totalDistance);
                    }
                    break;
                case RewardType.PositiveBM:
                    reward = HungarianDistance();
                    break;
            }
            return reward;
           
        }

        private int ManhattanDistance()
        {
            int totalDistance = 0;
            for (int x = 0; x < board.GetLength(0); x++)
            {
                for (int y = 0; y < board.GetLength(1); y++)
                {
                    if (board[x, y] == BOX)
                    {
                        distancesFromClosestGoal.TryGetValue(new Position(x, y), out int minDistance);
                        totalDistance += minDistance;
                    }
                }
            }
            return totalDistance;
        }

        private int HungarianDistance()
        {

            int[][] distances = new int[goals.Count()][];
            //for (int i = 0; i < distances.Length; i++)
            //{
            //    distances[i] = new int[distances.Length];
            //}
            int boxCounter = 0;
            for (int x = 0; x < board.GetLength(0); x++)
            {
                for (int y = 0; y < board.GetLength(1); y++)
                {
                    if (board[x, y] == BOX || board[x, y] == BOX_ON_GOAL)
                    {
                        PositionGoalPair pair;
                        List<int> currentBoxCosts = new List<int>();
                        foreach (Position goal in goals)
                        {
                            pair = new PositionGoalPair(new Position(x, y), goal);
                            int distance = int.MaxValue;
                            if (distancesFromAllGoals.TryGetValue(pair, out distance))
                            {
                                currentBoxCosts.Add(distance);
                            }
                            else
                            {
                                distance = 1000;
                                currentBoxCosts.Add(distance);
                            }
                        }
                        distances[boxCounter] = currentBoxCosts.ToArray();
                        boxCounter++;
                    }
                }
            }
            int[][] costs = new int[goals.Count()][];
            for(int i = 0; i < costs.Length; i++)
            {
                costs[i] = (int[])distances[i].Clone();
            }

            int[] pairings = new HungarianAlgorithm(costs).Run();
            int totalcost = 0;
            for (int i = 0; i < pairings.Length; i++)
            {
                if (pairings[i] < 0)
                {
                    totalcost = 1000;
                    break;
                }
                totalcost += costs[i][pairings[i]];
            }
            //Debug.WriteLine(PrettyPrint());
            //Debug.WriteLine(PrettyPrintDistance());
            //Debug.WriteLine(totalcost);

            return totalcost;
        }

        public int GetScore()
        {
            score = 1;
            if (isDeadlock)
            {
                score = 0;
            }
            if (CheckWinCondition())
            {
                score = 1;
            }
            return score;
        }

        public IPuzzleMove GetSimulationMove()
        {
            return simulationStrategy.selectMove(this);
        }

        public bool isTerminal()
        {
            if (isDeadlock)
            {
                return true;
            }
            if (GetMoves().Count == 0)
                return true;
            for (int y = 0; y < board.GetLength(1); y++)
            {
                for (int x = 0; x < board.GetLength(0); x++)
                {
                    if (board[x, y] == BOX && simpleDeadlock.Contains(new Position(x, y)))
                    {
                        return true;
                    }
                }
            }
            return EndState();
        }

        public void Pass()
        {
            return;
        }

        public string PrettyPrintDistance()
        {
            string s = "";
            for (int y = 0; y < distanceBoard.GetLength(1); y++)
            {
                for (int x = 0; x < distanceBoard.GetLength(0); x++)
                {
                    if (board[x, y] == WALL)
                    {
                        s += WALL_STR;
                    }
                    else
                    {
                        s += distanceBoard[x, y];
                    }

                }
                s += "\n";
            }
            return s;
        }

        public string PrettyPrint()
        {
            string s = "";
            for (int y = 0; y < board.GetLength(1); y++)
            {
                for (int x = 0; x < board.GetLength(0); x++)
                {
                    s += TranslateToExternalRepresentation(board[x, y]);
                }
                s += "\n";
            }
            return s;
        }

        public override string ToString()
        {
            return PrettyPrint();
        }

        private int TranslateToInternalRepresentation(string c)
        {
            switch (c)
            {
                case EMPTY_STR:
                    return EMPTY;
                case WALL_STR:
                    return WALL;
                case GOAL_STR:
                    return GOAL;
                case BOX_STR:
                    return BOX;
                case BOX_ON_GOAL_STR:
                    return BOX_ON_GOAL;
                case PLAYER_STR:
                    return PLAYER;
                case PLAYER_ON_GOAL_STR:
                    return PLAYER_ON_GOAL;
                default:
                    Debug.WriteLine("ERROR");
                    return EMPTY;
            }
        }

        private string TranslateToExternalRepresentation(int v)
        {
            switch (v)
            {
                case EMPTY:
                    return EMPTY_STR;
                case WALL:
                    return WALL_STR;
                case GOAL:
                    return GOAL_STR;
                case BOX:
                    return BOX_STR;
                case BOX_ON_GOAL:
                    return BOX_ON_GOAL_STR;
                case PLAYER:
                    return PLAYER_STR;
                case PLAYER_ON_GOAL:
                    return PLAYER_ON_GOAL_STR;
                default:
                    return v.ToString();
            }
        }

        public void Restart()
        {
            throw new NotImplementedException();
        }

        public bool StateChanged()
        {
            return stateChanged;
        }

        public int Winner()
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            int hc = 27;
            if (board != null)
            {
                foreach (var p in board)
                {
                    hc = (13 * hc) + p.GetHashCode();
                }
            }
            return hc;
        }

        public int GetEmptyBoardHash()
        {
            int hc = 27;
            if (board != null)
            {
                foreach (var p in board)
                {
                    if (p == PLAYER)
                    {
                        hc = (13 * hc) + EMPTY.GetHashCode();
                    }
                    else if (p == PLAYER_ON_GOAL)
                    {
                        hc = (13 * hc) + GOAL.GetHashCode();
                    }
                    else
                    {
                        hc = (13 * hc) + p.GetHashCode();
                    }
                }
            }
            return hc;
        }

        public override bool Equals(object obj)
        {
            return GetHashCode()==obj.GetHashCode();
        }

        private List<IPuzzleMove> GetBasicMoves()
        {
            List<IPuzzleMove> moves = new List<IPuzzleMove>();
            if (playerX < board.GetLength(0) - 1 &&
                board[playerX + 1, playerY] != WALL)
            {
                if (board[playerX + 1, playerY] == BOX || board[playerX + 1, playerY] == BOX_ON_GOAL)
                {
                    if (board[playerX + 2, playerY] == EMPTY || board[playerX + 2, playerY] == GOAL)
                        moves.Add(new SokobanGameMove("R"));
                }
                else
                {
                    moves.Add(new SokobanGameMove("r"));
                }
            }
            if (playerX > 0 &&
                board[playerX - 1, playerY] != WALL)
            {
                if (board[playerX - 1, playerY] == BOX || board[playerX - 1, playerY] == BOX_ON_GOAL)
                {
                    if (board[playerX - 2, playerY] == EMPTY || board[playerX - 2, playerY] == GOAL)
                        moves.Add(new SokobanGameMove("L"));
                }
                else
                {
                    moves.Add(new SokobanGameMove("l"));
                }
            }
            if (playerY < board.GetLength(1) - 1 &&
                board[playerX, playerY + 1] != WALL)
            {
                if (board[playerX, playerY + 1] == BOX || board[playerX, playerY + 1] == BOX_ON_GOAL)
                {
                    if (board[playerX, playerY + 2] == EMPTY || board[playerX, playerY + 2] == GOAL)
                        moves.Add(new SokobanGameMove("D"));
                }
                else
                {
                    moves.Add(new SokobanGameMove("d"));
                }
            }
            if (playerY > 0 &&
                board[playerX, playerY - 1] != WALL)
            {
                if (board[playerX, playerY - 1] == BOX || board[playerX, playerY - 1] == BOX_ON_GOAL)
                {
                    if (board[playerX, playerY - 2] == EMPTY || board[playerX, playerY - 2] == GOAL)
                        moves.Add(new SokobanGameMove("U"));
                }
                else
                {
                    moves.Add(new SokobanGameMove("u"));
                }
            }
            CheckWinCondition();
            return moves;
        }
    }

    class PositionGoalPair
    {
        public Position position;
        public Position goal;

        public PositionGoalPair(Position position, Position goal)
        {
            this.position = position;
            this.goal = goal;
        }

        public override bool Equals(object obj)
        {
            var pair = obj as PositionGoalPair;
            return pair != null &&
                   position.Equals(pair.position) &&goal.Equals(pair.goal);
        }

        public override int GetHashCode()
        {
            var hashCode = 1583233552;
            hashCode = hashCode * -1521134295 + position.GetHashCode();
            hashCode = hashCode * -1521134295 + goal.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return "Pos:"+position+" - Goal:"+goal;
        }
    }

    class PositionCost
    {
        public Position position;
        public int cost;

        public PositionCost(Position position, int cost)
        {
            this.position = position;
            this.cost = cost;
        }
    }
}
