using Common.Abstract;
using MCTS.Standard.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCTS2016.SP_MCTS.SP_UCT;
using MCTS2016.Common.Abstract;

namespace MCTS2016.SP_MCTS
{
    class SP_MCTSAlgorithm
    {
        private ISPTreeNodeCreator treeCreator;
        private bool search = true;

        private List<IPuzzleMove> bestRollout;
        private double topScore = double.MinValue;
        private bool updateTopScore;

        public SP_MCTSAlgorithm(ISPTreeNodeCreator treeCreator)
        {
            this.treeCreator = treeCreator;
        }

        public List<IPuzzleMove> Solve(IPuzzleState rootState, int iterations, double maxTimeInMinutes = 5)
        {
            IPuzzleMove bestMove = Search(rootState, iterations, maxTimeInMinutes);
            return bestRollout;
        }

        public IPuzzleMove Search(IPuzzleState rootState, int iterations, double maxTimeInMinutes = 5)
        {

            int nodeCount = 0; 
            long startTime = DateTime.Now.Ticks;
            if (!search)
            {
                search = true;
            }

            ISPTreeNode rootNode = treeCreator.GenRootNode(rootState);

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
                ISPTreeNode node = rootNode;
                IPuzzleState state = rootState.Clone();

                // Select
                while (!node.HasMovesToTry() && node.HasChildren())
                {
                    node = node.SelectChild();
                    state.DoMove(node.Move);
                }

                // Expand
                if (node.HasMovesToTry())
                {
                    IPuzzleMove move = node.SelectUntriedMove();
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

                List<IPuzzleMove> currentRollout = new List<IPuzzleMove>();
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
                if (state.GetScore() > topScore)
                {
                    topScore = state.GetScore();
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
                    node.Update(state.GetResult());
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
            IPuzzleMove bestMove;
            if (bestRollout != null)
            {
                bestMove = bestRollout.First<IPuzzleMove>();
                bestRollout.RemoveAt(0);
            }
            else
            {
                bestMove = rootNode.GetBestMove();
            }
            //Remove first move from rollout so that if the topScore is not beaten we can just take the next move
            
            
            return bestMove;
        }

        public ISPTreeNodeCreator TreeCreator
        {
            get { return treeCreator; }
        }

        public void Abort()
        {
            search = false;
        }
    }
}
