using Common.Abstract;
using MCTS.Standard.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCTS2016.MCTS
{
    class SP_MCTSAlgorithm
    {
        private ITreeNodeCreator treeCreator;
        private bool search = true;

        private List<IGameMove> bestRollout;
        private double topScore = double.MinValue;
        private bool updateTopScore;

        public SP_MCTSAlgorithm(ITreeNodeCreator treeCreator)
        {
            this.treeCreator = treeCreator;
        }

        public List<IGameMove> Solve(IGameState rootState, int iterations)
        {
            List<IGameMove> moves = new List<IGameMove>();
            while (!rootState.isTerminal())
            {
                IGameMove bestMove = Search(rootState, iterations);
                rootState.DoMove(bestMove);
                moves.Add(bestMove);
            }
            return moves;
        }

        public IGameMove Search(IGameState rootState, int iterations, double maxTimeInMinutes = 5)
        {

            int nodeCount = 0; 
            long startTime = DateTime.Now.Ticks;
            if (!search)
            {
                search = true;
            }

            ITreeNode rootNode = treeCreator.GenRootNode(rootState);

            long beforeMemory = GC.GetTotalMemory(false);
            long afterMemory = GC.GetTotalMemory(false);
            long usedMemory = afterMemory - beforeMemory;
            long averageUsedMemoryPerIteration = 0;
            int i = 0;
            for (i = 0; i < iterations; i++)
            {
                if(DateTime.Now.Ticks > startTime + maxTimeInMinutes * 60 * 1000 * 1000 * 10)
                {
                    break;
                }
                ITreeNode node = rootNode;
                IGameState state = rootState.Clone();

                // Select
                while (!node.HasMovesToTry() && node.HasChildren())
                {
                    node = node.SelectChild();
                    state.DoMove(node.Move);
                }

                // Expand
                if (node.HasMovesToTry())
                {
                    IGameMove move = node.SelectUntriedMove();
                    if (move != -1)
                    {
                        state.DoMove(move);
                    }
                    else
                    {
                        state.Pass();
                    }
                    node = node.AddChild(move, state);
                    nodeCount++;
                }

                List<IGameMove> currentRollout = new List<IGameMove>();
                // Rollout
                while (!state.isTerminal())
                {
                    var move = state.GetSimulationMove();
                    if (move != -1)
                    {
                        //Keep rollout moves
                        currentRollout.Add(move);
                        state.DoMove(move);
                    }
                    else
                    {
                        state.Pass();
                    }
                }

                //Keep topScore and update bestRollout
                if (state.GetScore(node.PlayerWhoJustMoved) > topScore)
                {
                    topScore = state.GetScore(node.PlayerWhoJustMoved);
                    bestRollout = currentRollout ;
                    bestRollout.Reverse();
                    updateTopScore = true;
                }

                // Backpropagate
                while (node != null)
                {
                    if (updateTopScore && node.Move != null)
                    {
                        //Complete bestRollout with moves from nodes already in the tree
                        bestRollout.Add(node.Move);
                    }
                    node.Update(state.GetResult(node.PlayerWhoJustMoved));
                    node = node.Parent;
                    
                }

                if (updateTopScore)
                {
                    //Debug.WriteLine(bestRollout.Count);
                    bestRollout.Reverse();
                    updateTopScore = false;
                }

                if (!search)
                {
                    search = true;
                    return null;
                }

                afterMemory = GC.GetTotalMemory(false);
                usedMemory = afterMemory - beforeMemory;
                averageUsedMemoryPerIteration = usedMemory / (i + 1);

                var outStringToWrite = string.Format(" MCTS search: {0:0.00}% [{1} of {2}] - Total used memory b(mb): {3}({4}) - Average used memory per iteration b(mb): {5}({6:N7})", (float)((i + 1) * 100) / (float)iterations, i + 1, iterations, usedMemory, usedMemory / 1024 / 1024, averageUsedMemoryPerIteration, (float)averageUsedMemoryPerIteration / 1024 / 1024);
#if DEBUG
                if (iterations > 50000)
                {
                    Console.Write(outStringToWrite);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }
#endif

                //Console.WriteLine(rootNode.TreeToString(0));
            }
            //Console.WriteLine();

            //#if DEBUG
            //            Console.WriteLine(rootNode.ChildrenToString());
            //            Console.WriteLine(rootNode.TreeToString(0));
            //#endif
            //Debug.WriteLine("Iterations: " + i);
            //Debug.WriteLine("Node Count: " + nodeCount);
            IGameMove bestMove;
            if (bestRollout != null)
            {
                bestMove = bestRollout.First<IGameMove>();
                bestRollout.RemoveAt(0);
            }
            else
            {
                bestMove = rootNode.GetBestMove();
            }
            //Remove first move from rollout so that if the topScore is not beaten we can just take the next move
            
            
            return bestMove;
        }

        public ITreeNodeCreator TreeCreator
        {
            get { return treeCreator; }
        }

        public void Abort()
        {
            search = false;
        }
    }
}
