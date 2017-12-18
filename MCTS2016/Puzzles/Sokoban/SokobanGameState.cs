using Common;
using Common.Abstract;
using MCTS2016.Puzzles.SameGame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCTS2016.Puzzles.Sokoban
{
    class SokobanGameState : IGameState
    {
        public int playerJustMoved { get; set; } //0

        public int currentPlayer { get; set; } //1

        public int numberOfPlayers { get; set; } //1

        public int size { get; set; }//board width


        private bool stateChanged=false;

        const int EMPTY = 0;
        const int WALL = 1;
        const int GOAL = 2;
        const int BOX = 3;
        const int BOX_ON_GOAL = 4;
        const int PLAYER = 5;
        const int PLAYER_ON_GOAL = 6;

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

        private List<Position> simpleDeadlock;

        private ISimulationStrategy simulationStrategy;

        public SokobanGameState(String level, ISimulationStrategy simulationStrategy = null)
        {
            String[] levelRows = level.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            int maxWidth = 0;
            foreach(string row in levelRows)
            {
                maxWidth = Math.Max(maxWidth, row.Length);
            }
            board = new int[maxWidth,levelRows.Length];
            int x, y = 0;
            foreach (string row in levelRows)
            {
                x = 0;
                foreach(char c in row)
                {
                    board[x, y] = TranslateToInternalRepresentation(c.ToString()) ;
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
        }

        private SokobanGameState()
        {
            board = new int[1, 1];
            this.simulationStrategy = new SokobanRandomStrategy();
        }

        public IGameState Clone()
        {
            return new SokobanGameState()
            {
                playerJustMoved = playerJustMoved,
                currentPlayer = currentPlayer,
                numberOfPlayers = numberOfPlayers,
                size = size,
                board = board.Clone() as int[,],
                playerX = playerX,
                playerY = playerY,
                simulationStrategy = simulationStrategy
            };
        }

        List<Position> FindDeadlockPositions()
        {
            List<Position> deadlockPositions = new List<Position>();
            List<Position> goals = new List<Position>();
            int[,] backupBoard = board.Clone() as int[,];
            for(int x = 0; x < board.GetLength(0); x++)
            {
                for(int y = 0; y < board.GetLength(1); y++)
                {
                    if (board[x, y] == BOX || board[x, y] == PLAYER)
                        board[x,y] = EMPTY;
                    if (board[x, y] == BOX_ON_GOAL || board[x, y] == PLAYER_ON_GOAL)
                        board[x, y] = GOAL;
                    if(board[x, y] == GOAL){
                        goals.Add(new Position(x,y));
                    }
                }
            }
            foreach(Position goal in goals)
            {
                ResetBoard();

            }

            return deadlockPositions;
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

        public void DoMove(IGameMove move)
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
            }
            else if (board[x, y] == GOAL)
            {
                board[x, y] = PLAYER_ON_GOAL;
            }
            if (board[playerX, playerY] == PLAYER)
            {
                board[playerX, playerY] = EMPTY;
            }
            else if (board[playerX, playerY] == PLAYER_ON_GOAL)
            {
                board[playerX, playerY] = GOAL;
            }
            stateChanged = true;
            playerX = x;
            playerY = y;
        }

        private void PushBox(int xDirection, int yDirection)
        {
            if (board[playerX + 2*xDirection, playerY + 2*yDirection] == EMPTY)
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
                }else if (board[playerX + xDirection, playerY + yDirection] == BOX_ON_GOAL)
                {
                    board[playerX + xDirection, playerY + yDirection] = PLAYER_ON_GOAL;
                }
                if (board[playerX, playerY] == PLAYER)
                {
                    board[playerX, playerY] = EMPTY;
                }else if (board[playerX, playerY] == PLAYER_ON_GOAL)
                {
                    board[playerX, playerY] = GOAL;
                }
                playerX += xDirection;
                playerY += yDirection;
            }
        }

        private void PullBox(int xDirection, int yDirection)
        {
            if (board[playerX - xDirection, playerY - yDirection] == EMPTY || board[playerX - xDirection, playerY - yDirection] == GOAL)
            {
                if (board[playerX, playerY] == PLAYER)
                {
                    board[playerX, playerY] = BOX;
                }
                else
                {
                    board[playerX, playerY] = BOX_ON_GOAL;

                }
                stateChanged = true;
            }
            if (stateChanged)
            {
                if (board[playerX + xDirection, playerY + yDirection] == BOX)
                {
                    board[playerX + xDirection, playerY + yDirection] = EMPTY;
                }
                else if (board[playerX + xDirection, playerY + yDirection] == BOX_ON_GOAL)
                {
                    board[playerX + xDirection, playerY + yDirection] = GOAL;
                }
                if (board[playerX - xDirection, playerY - yDirection] == EMPTY)
                {
                    board[playerX - xDirection, playerY - yDirection] = PLAYER;
                }
                else if (board[playerX - xDirection, playerY - yDirection] == GOAL)
                {
                    board[playerX - xDirection, playerY - yDirection] = PLAYER_ON_GOAL;
                }
                playerX -= xDirection;
                playerY -= yDirection;
            }
        }


        public bool EndState()
        {
            bool win = true;
            //check win condition
            for(int y = 0; y < board.GetLength(1); y++)
            {
                for(int x = 0; x < board.GetLength(0); x++)
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
            //TODO check deadlock
            return win;
        }

        public List<int> GetBoard()
        {
            List<int> boardList = new List<int>();
            for (int y = 0; y < board.GetLength(1); y++)
            {
                for (int x = 0; x < board.GetLength(0); x++)
                {
                    boardList.Add(board[x,y]);
                }
            }
            return boardList;
        }

        public int GetBoard(int x, int y)
        {
            return board[x, y];
        }

        public List<IGameMove> GetMoves()
        {
            List<IGameMove> moves = new List<IGameMove>();
            if(playerX < size - 1 &&
                board[playerX + 1, playerY] != WALL)
            {
                if(board[playerX+1,playerY] == BOX || board[playerX + 1, playerY] == BOX_ON_GOAL)
                {
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
                    moves.Add(new SokobanGameMove("L"));
                }
                else
                {
                    moves.Add(new SokobanGameMove("l"));
                }
            }
            if (playerY < size - 1 &&
                board[playerX, playerY + 1] != WALL)
            {
                if (board[playerX, playerY + 1] == BOX || board[playerX, playerY + 1] == BOX_ON_GOAL)
                {
                    moves.Add(new SokobanGameMove("D"));
                }
                else
                {
                    moves.Add(new SokobanGameMove("d"));
                }
            }
            if (playerY > 0 &&
                board[playerX, playerY -1] != WALL)
            {
                if (board[playerX, playerY -1] == BOX || board[playerX, playerY - 1] == BOX_ON_GOAL)
                {
                    moves.Add(new SokobanGameMove("U"));
                }
                else
                {
                    moves.Add(new SokobanGameMove("u"));
                }
            }
            return moves;
        }

        public int GetPositionIndex(int x, int y)
        {
            return y * size + x;
        }

        public IGameMove GetRandomMove()
        {
            List<IGameMove> moves = GetMoves();
            int rndIndex = RNG.Next(moves.Count);
            return moves[rndIndex];
        }

        public double GetResult(int player)
        {
            throw new NotImplementedException();
        }

        public int GetScore(int player)
        {
            throw new NotImplementedException();
        }

        public IGameMove GetSimulationMove()
        {
            return simulationStrategy.selectMove(this);
        }

        public bool isTerminal()
        {
            return GetMoves().Count == 0 || EndState();
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
                    return EMPTY_STR;
            }
        }

        public void Restart(int _player = 1)
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
    }
}
