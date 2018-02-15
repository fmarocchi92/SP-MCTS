using MCTS2016.Common.Abstract;
using MCTS2016.Puzzles.SameGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCTS2016.Puzzles.Sokoban
{
    class SokobanPushMove : IPuzzleMove
    {
        private Position playerPosition;

        private SokobanGameMove pushMove;

        private List<SokobanGameMove> moveList;

        internal List<SokobanGameMove> MoveList { get => moveList; set => moveList = value; }
        internal SokobanGameMove PushMove { get => pushMove; set => pushMove = value; }
        internal Position PlayerPosition { get => playerPosition; set => playerPosition = value; }

        public SokobanPushMove(SokobanGameMove pushMove, List<SokobanGameMove> moves, Position playerPosition)
        {
            PushMove = pushMove;
            MoveList = moves;
            PlayerPosition = playerPosition;
        }

        public override string ToString()
        {
            string s = "";
            foreach(SokobanGameMove m in moveList)
            {
                s += m;
            }
            return playerPosition+":"+ s+PushMove.ToString();
        }
    }
}
