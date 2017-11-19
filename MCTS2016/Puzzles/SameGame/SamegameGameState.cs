using Common;
using Common.Abstract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCTS2016.Puzzles.SameGame
{
    class SamegameGameState : IGameState
    {
        public int playerJustMoved => 1;

        public int currentPlayer => 1;

        public int numberOfPlayers => 1;

        public int size {get;set;}

        private List<List<int>> board;

        private int score;

        private bool stateChanged = false;

        private ISimulationStrategy simulationStrategy;

        private SamegameGameState()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="levelBoardToTranspose">An array containing the rows of the level</param>
        /// <param name="sim"></param>
        public SamegameGameState(int[][] levelBoardToTranspose, ISimulationStrategy sim = null)
        {
            //Transform arrays into lists 
            List<List<int>> levelBoard = new List<List<int>>();
            for (int i = 0; i < levelBoardToTranspose.Length; i++)
            {
                List<int> newList = new List<int>();
                for (int j = 0; j < levelBoardToTranspose[i].Length; j++)
                {
                    newList.Add(levelBoardToTranspose[j][i]);
                }
                levelBoard.Add(newList);
            }

            foreach (List<int> column in levelBoard)
            {
                column.Reverse();
            }
            InitState(levelBoard, sim);
            
        }
        public SamegameGameState(List<List<int>> levelBoard, ISimulationStrategy sim = null)
        {
            InitState(levelBoard, sim);
        }

        private void InitState(List<List<int>> levelBoard, ISimulationStrategy sim)
        {
            board = levelBoard;
            size = board.Count;
            if (sim == null)
            {
                simulationStrategy = new SamegameRandomStrategy();
            }
            else
            {
                simulationStrategy = sim;
            }
        }
        
        
        public SamegameGameState(string level, ISimulationStrategy sim = null)
        {
            String[] levelRows = level.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            board = new List<List<int>>();
            foreach (string row in levelRows)
            {
                board.Add(new List<int>());
            }
            int x;
            int val;
            foreach (string row in levelRows)
            {
                x = 0;
                foreach (char c in row)
                {
                    int.TryParse(c.ToString(), out val);
                    board[x].Add(val);
                    x++;
                }
            }
            foreach(List<int> column in board)
            {
                column.Reverse();
            }
            size = levelRows.Length;

            if (sim == null)
            {
                simulationStrategy = new SamegameRandomStrategy();
            }
            else
            {
                simulationStrategy = sim;
            }
        }

        public IGameState Clone()
        {
            List<List<int>> boardCopy = new List<List<int>>(); ;
            foreach(List<int> column in board)
            {
                List<int> newColumn = new List<int>();
                foreach(int value in column)
                {
                    newColumn.Add(value);
                }
                boardCopy.Add(newColumn);
            }
            return new SamegameGameState()
            {
                board = boardCopy,
                simulationStrategy = this.simulationStrategy,
                score = this.score,
                size = this.size,
            };
        }

        public void DoMove(IGameMove move)
        {
            stateChanged = false;
            SamegameGameMove sgmove = move as SamegameGameMove;
            int value = board[sgmove.x][sgmove.y];
            List<Tuple<int, int>> toRemove = new List<Tuple<int, int>>();
            CheckAdjacentBlocks(sgmove.x, sgmove.y, value, toRemove); //remove adjacent blocks
            if(toRemove.Count > 0)
            {
                board[sgmove.x][sgmove.y] = 1000;
                score += (int) Math.Pow((toRemove.Count() - 2) ,2);
                stateChanged = true;
            }
            foreach (Tuple<int, int> block in toRemove)
            {
                board[block.Item1][block.Item2] = 1000;
            }
            foreach (List<int> column in board)
            {
                column.RemoveAll(v => v == 1000);
            }
            board.RemoveAll(column => column.Count == 0); //remove empty columns
        }
        /// <summary>
        /// Recursively find adjacent blocks with the same value and put them into toRemove
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        /// <param name="toRemove"></param>
        private void CheckAdjacentBlocks(int x, int y, int value, List<Tuple<int,int>> toRemove) //should I use "out" for toRemove?
        {
            if(x > 0 && board[x - 1].Count > y)
            {
                if(board[x-1][y] == value && !toRemove.Contains(Tuple.Create<int, int>(x - 1, y)))
                {
                    toRemove.Add(Tuple.Create<int, int>(x - 1, y));
                    CheckAdjacentBlocks(x - 1, y, value, toRemove);
                }
            }
            if(x < board.Count -1 && board[x + 1].Count > y)
            {
                if (board[x + 1][y] == value && !toRemove.Contains(Tuple.Create<int, int>(x + 1, y)))
                {
                    toRemove.Add(Tuple.Create<int, int>(x + 1, y));
                    CheckAdjacentBlocks(x + 1, y, value, toRemove);
                }
            }
            if (y > 0)
            {
                if (board[x][y-1] == value && !toRemove.Contains(Tuple.Create<int, int>(x, y - 1)))
                {
                    toRemove.Add(Tuple.Create<int, int>(x, y - 1));
                    CheckAdjacentBlocks(x, y - 1, value, toRemove);
                }
            }
            if (y < board[x].Count -1)
            {
                if (board[x][y + 1] == value && !toRemove.Contains(Tuple.Create<int, int>(x, y + 1)))
                {
                    toRemove.Add(Tuple.Create<int, int>(x, y + 1));
                    CheckAdjacentBlocks(x, y + 1, value, toRemove);
                }
            }
        }

        public bool EndState()
        {
            return (board.Count == 0);
        }

        public List<int> GetBoard()
        {
            List<int> boardList = new List<int>();
            foreach(List<int> column in board)
            {
                boardList.AddRange(column);
            }
            return boardList;
        }

        public int GetBoard(int x, int y)
        {
            return board[x][y];
        }

        public List<IGameMove> GetMoves()
        {
            List<IGameMove> moves  = new List<IGameMove>();
            int x = 0;
            int y = 0;
            List<Tuple<int, int>> alreadyChecked = new List<Tuple<int, int>>();
            foreach (List<int> column in board)
            {
                y = 0;
                foreach(int value in column)
                {
                    if(!alreadyChecked.Contains(Tuple.Create<int, int>(x, y))) //only check for unchecked blocks
                    {
                        List<Tuple<int, int>> group = new List<Tuple<int, int>>();
                        CheckAdjacentBlocks(x, y, value, group); //group adjacent blocks together to have a single action for all of them
                        if (group.Count>0)
                        {
                            moves.Add(new SamegameGameMove(x, y));
                            alreadyChecked.AddRange(group);
                        }
                    }
                    y++;
                }
                x++;
            }
            return moves;
        }
        

        public int GetPositionIndex(int x, int y)
        {
            int i = 0;
            for(int j = 0; j < x; j++)
            {
                i += board[j].Count;
            }
            i += y;
            return i;
        }

        public IGameMove GetRandomMove()
        {
            List<IGameMove> moves = GetMoves();
            int rndIndex = RNG.Next(moves.Count);
            return moves[rndIndex];
        }

        public double GetResult(int player)
        {
            return GetScore(1);
        }

        public int GetScore(int player)
        {
            int finalScore = score;
            if(board.Count == 0) //bonus of 1000 if the board is empty 
            {
                finalScore += 1000;
                //Debug.WriteLine("Emptied board: score = "+finalScore);
            }
            if (isTerminal() && board.Count > 0) //penalty of (number of blocks left -2)^2 if at the end the board is not empty
            {
                int remainingBlocks = 0;
                foreach (List<int> column in board)
                {
                    remainingBlocks += column.Count;
                }
                if (remainingBlocks > 2)
                {
                    finalScore -= (remainingBlocks - 2) * (remainingBlocks - 2);
                }
            }
            return finalScore;
        }

        public IGameMove GetSimulationMove()
        {
            return simulationStrategy.selectMove(this);
        }

        public bool isTerminal()
        {
            return EndState() || GetMoves().Count == 0;
        }

        public void Pass()
        {
            throw new NotImplementedException();
        }

        public string PrettyPrint()//prints the board rotated 90° clockwise
        {
            string s = "";
            foreach(List<int> column in board)
            {
                foreach(int value in column)
                {
                    s += value.ToString();
                }
                if (board.Last<List<int>>() != column)
                {
                    s += "\n";
                }
            }
            return s;
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
