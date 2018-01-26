using Common;
using Common.Abstract;
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


        private bool stateChanged = false;

        private bool isDeadlock = false;

        const int EMPTY = 0;
        const int WALL = 1;
        const int GOAL = 2;
        const int BOX = 3;
        const int BOX_ON_GOAL = 4;
        const int PLAYER = 5;
        const int PLAYER_ON_GOAL = 6;
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

        private int playerX;
        private int playerY;

        private int score;
        private bool win;
        public HashSet<Position> simpleDeadlock;
        private HashSet<IPuzzleState> visitedStates=new HashSet<IPuzzleState>();

        private Dictionary<Position, int> distancesFromGoals = new Dictionary<Position, int>();

        private ISPSimulationStrategy simulationStrategy;

        public SokobanGameState(String level, ISPSimulationStrategy simulationStrategy = null)
        {
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
                    if (board[x, y] == PLAYER)
                    {
                        playerX = x;
                        playerY = y;
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
            visitedStates.Add(this.Clone());
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
                visitedStates = new HashSet<IPuzzleState>(visitedStates),//TODO visited states: check if this need its own clone
                distancesFromGoals = distancesFromGoals
            };
        }
                
        HashSet<Position> FindDeadlockPositions()
        {
            HashSet<Position> deadlockPositions = new HashSet<Position>();
            List<Position> goals = new List<Position>();
            Position backupPlayer = new Position(playerX, playerY);
            int[,] backupBoard = board.Clone() as int[,];
            for (int x = 0; x < board.GetLength(0); x++)
            {
                for (int y = 0; y < board.GetLength(1); y++)
                {
                    if (board[x, y] == BOX || board[x, y] == PLAYER)
                        board[x, y] = EMPTY;
                    if (board[x, y] == BOX_ON_GOAL || board[x, y] == PLAYER_ON_GOAL)
                        board[x, y] = GOAL;
                    if (board[x, y] == GOAL)
                    {
                        goals.Add(new Position(x, y));
                    }
                }
            }
            foreach (Position goal in goals)
            {
                ResetBoard();
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
            board = backupBoard;
            playerX = backupPlayer.X;
            playerY = backupPlayer.Y;
            return deadlockPositions;
        }

        void Explore(int x, int y, int depth)
        {
            Position currentPosition = new Position(x, y);
            if (!distancesFromGoals.TryGetValue(currentPosition, out int oldValue))
            {
                distancesFromGoals.Add(currentPosition,depth);
            }else if(depth < oldValue)
            {
                distancesFromGoals.Remove(currentPosition);
                distancesFromGoals.Add(currentPosition, depth);
            }
            
            if (board[x + 1, y] != WALL)
            {
                playerX = x + 1;
                playerY = y;
                bool pulled = PullBox(1, 0);
                if (pulled)
                {
                    Explore(x + 1, y, depth+1);
                }
            }
            if (board[x, y + 1] != WALL)
            {
                playerX = x;
                playerY = y + 1;
                bool pulled = PullBox(0, 1);
                if (pulled)
                {
                    Explore(x, y + 1, depth+1);
                }
            }
            if (board[x - 1, y] != WALL)
            {
                playerX = x - 1;
                playerY = y;
                bool pulled = PullBox(-1, 0);
                if (pulled)
                {
                    Explore(x - 1, y, depth+1);
                }
            }
            if (board[x, y - 1] != WALL)
            {
                playerX = x;
                playerY = y - 1;
                bool pulled = PullBox(0, -1);
                if (pulled)
                {
                    Explore(x, y - 1, depth+1);
                }
            }
        }

        void ResetBoard()
        {
            for (int x = 0; x < board.GetLength(0); x++)
            {
                for (int y = 0; y < board.GetLength(1); y++)
                {
                    if (board[x, y] == BOX || board[x, y] == PLAYER)
                        board[x, y] = EMPTY;
                    if (board[x, y] == BOX_ON_GOAL || board[x, y] == PLAYER_ON_GOAL)
                        board[x, y] = GOAL;
                }
            }
        }

        public void DoMove(IPuzzleMove move)
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
            visitedStates.Add(this.Clone());
        }

        private void MovePlayer(int x, int y)
        {
            //score--;
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
            //score++;
            if (board[playerX + 2 * xDirection, playerY + 2 * yDirection] == EMPTY)
            {
                board[playerX + 2 * xDirection, playerY + 2 * yDirection] = BOX;
                stateChanged = true;
            }
            else if (board[playerX + 2 * xDirection, playerY + 2 * yDirection] == GOAL)
            {
                stateChanged = true;
                board[playerX + 2 * xDirection, playerY + 2 * yDirection] = BOX_ON_GOAL;
                //score += 100;
            }
            if (stateChanged)
            {
                if (board[playerX + xDirection, playerY + yDirection] == BOX)
                {
                    board[playerX + xDirection, playerY + yDirection] = PLAYER;
                }
                else if (board[playerX + xDirection, playerY + yDirection] == BOX_ON_GOAL)
                {
                    //score-=100;
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
                isDeadlock = CheckFreezeDeadlock(playerX + xDirection, playerY + yDirection, new HashSet<Position>(), new HashSet<Position>());
            }
        }

        bool CheckFreezeDeadlock(int x, int y, HashSet<Position> checkedBoxes, HashSet<Position> frozenBoxes)
        {
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
                //if (board[x, y] != BOX_ON_GOAL)
                //    return true;
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
                    if (CheckFreezeDeadlock(x + 1, y, checkedBoxes, frozenBoxes))
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
                    if (CheckFreezeDeadlock(x - 1, y, checkedBoxes, frozenBoxes))
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
                    if (CheckFreezeDeadlock(x, y + 1, checkedBoxes, frozenBoxes))
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
                    if (CheckFreezeDeadlock(x, y - 1, checkedBoxes, frozenBoxes))
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
            List<IPuzzleMove> moves = new List<IPuzzleMove>();
            if(isDeadlock || win)
            {
                return moves;
            }
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
            //////////////////////////////////// Avoid repeating states
            List<IPuzzleMove> toRemove = new List<IPuzzleMove>();
            //Debug.WriteLine(PrettyPrint());
            //foreach(IPuzzleState s in visitedStates)
            //{
            //    Debug.WriteLine(s.PrettyPrint());
            //    Debug.WriteLine(s.GetHashCode());
            //}
            foreach (IPuzzleMove m in moves)
            {
                //Debug.WriteLine(m);
                IPuzzleState s = this.Clone();
                s.DoMove(m);
                //Debug.WriteLine(s.PrettyPrint());
                //Debug.WriteLine(s.GetHashCode());
                if (visitedStates.Contains(s))
                {
                    toRemove.Add(m);
                }
            }
            ////////////////////////////////////
            foreach(IPuzzleMove m in toRemove)
            {
                moves.Remove(m);
            }
            return moves;
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
            return GetScore();
            int totalDistance = 0;
            for(int x = 0; x < board.GetLength(0); x++)
            {
                for(int y = 0; y < board.GetLength(1); y++)
                {
                    if (board[x, y] == BOX)
                    {
                        distancesFromGoals.TryGetValue(new Position(x, y), out int minDistance);
                        totalDistance += minDistance;
                    }
                }
            }
            if (totalDistance == 0)
            {
                totalDistance = 1;
            }
            if (isDeadlock)
            {
                return 0;
            }
            if (CheckWinCondition())
            {
                return 1;
            }
            return (1 / totalDistance);

        }

        public int GetScore()
        {
            score = 1;
            //score = 0;
            if (isDeadlock)
            {
                score = -1;
            }
            if (CheckWinCondition())
            {
                score = 10000;
                //score = 1;
            }
            //foreach(int v in board)
            //{
            //    if (v == BOX_ON_GOAL)
            //    {
            //        score += 1;
            //    }
            //}
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
                    return "0";
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
                foreach (var p in board)
                    hc = (13 * hc) + p.GetHashCode();
            return hc;
        }

        public override bool Equals(object obj)
        {
            return GetHashCode()==obj.GetHashCode();
        }
    }
}
