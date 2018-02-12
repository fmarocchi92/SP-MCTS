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
        private bool checkForRepeatingStates = true;


        public SP_MCTSAlgorithm(ISPTreeNodeCreator treeCreator)
        {
            this.treeCreator = treeCreator;
        }

        public List<IPuzzleMove> Solve(IPuzzleState rootState, int iterations, double maxTimeInMinutes = 5)
        {
            IPuzzleMove bestMove = Search(rootState, iterations, maxTimeInMinutes);
            List<IPuzzleMove> moves = new List<IPuzzleMove>() { bestMove };
            moves.AddRange(bestRollout);
            return moves;
        }

        public IPuzzleMove Search(IPuzzleState rootState, int iterations, double maxTimeInMinutes = 5)
        {
            HashSet<IPuzzleState> visitedStatesInTree = new HashSet<IPuzzleState>() {rootState};


            int nodeCount = 0; 
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
                ISPTreeNode node = rootNode;
                IPuzzleState state = rootState.Clone();
                int currentBranchDepth = 0;
                // Select
                while (!node.HasMovesToTry() && node.HasChildren())
                {
                    node = node.SelectChild();
                    state.DoMove(node.Move);
                    //Debug.WriteLine(state.PrettyPrint());
                    currentBranchDepth++;
                }
                IPuzzleState backupState = state.Clone();
                // Expand
                if (node.HasMovesToTry())
                {
                    IPuzzleMove move = node.SelectUntriedMove();
                    if (move != -1)
                    {
                        state.DoMove(move);
                        //Debug.WriteLine(state.PrettyPrint());
                        currentBranchDepth++;
                    }
                    else
                    {
                        state.Pass();
                    }
                    //if (!visitedStates.Contains(state))
                    //{

                        node = node.AddChild(move, state);
                        nodeCount++;

                        //break;
                    //}
                    //else//TODO find shortest route to state and delete the rest
                    //{
                    //    state = backupState;
                    //}
                }

                List<IPuzzleMove> currentRollout = new List<IPuzzleMove>();

                HashSet<IPuzzleState> visitedStatesInRollout = new HashSet<IPuzzleState>() { state };
                // Rollout
                int rolloutMovesCount = 0;
                while (!state.isTerminal() && rolloutMovesCount<1000)
                {
                    //IPuzzleState clonedState = state.Clone();
                    rolloutMovesCount++;
                    var move = state.GetSimulationMove();
                    backupState = state.Clone();
                    
                    if (move != -1)
                    {
                        //Keep rollout moves
                        
                        //Debug.WriteLine(move);
                        state.DoMove(move);
                        if (visitedStatesInRollout.Contains(state))
                        {
                            state = backupState.Clone();
                            List<IPuzzleMove> availableMoves = state.GetMoves();
                            int triedMoves = 0;
                            for (triedMoves = 0; triedMoves < availableMoves.Count() && visitedStatesInRollout.Contains(state); triedMoves++)
                            {
                                state = backupState.Clone();
                                state.DoMove(availableMoves[triedMoves]);
                            }
                            if(triedMoves < availableMoves.Count())
                            {
                                currentRollout.Add(move);
                                visitedStatesInRollout.Add(state.Clone());
                            }
                            else
                            {
                                //Console.WriteLine("No untried move left in current rollout");
                                break;
                            }
                        }
                        currentRollout.Add(move);
                        //Debug.WriteLine(state.PrettyPrint());
                    }
                    else
                    {
                        state.Pass();
                    }
                }
                if (rolloutMovesCount == 1000)
                {
                    Console.WriteLine("Rollout terminated after 1000 steps");
                }
                //Debug.WriteLine(state);
                //Keep topScore and update bestRollout
                double result = state.GetResult();
                if(result==1 && !state.EndState())
                {
                    Debug.WriteLine(state.PrettyPrint());
                }
                if (result > topScore || result == topScore && currentRollout.Count()+currentBranchDepth < bestRollout.Count())
                {
                    //if(bestRollout!=null) Console.WriteLine("bestRollout:" + bestRollout.Count());
                    //Console.WriteLine("Total Depth: "+(currentRollout.Count() + currentBranchDepth) +" rollout:"+currentRollout.Count() + " branch:"+currentBranchDepth);
                    topScore = result;
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
                    node.Update(result);
                    node = node.Parent;
                    
                }

                if (updateTopScore)
                {
                    //Debug.WriteLine(bestRollout.Count);
                    bestRollout.Reverse();
                    //updateTopScore = false;
                    if (topScore > 0)
                    {
                        //Console.WriteLine("New top score " + topScore + " found at rollout " + i + ". Solution length: " + bestRollout.Count());
                    }
                }

                if (state.EndState() && updateTopScore)
                {
                    //Console.WriteLine("Solution found with score " + topScore + " found at rollout " + i + ".Solution length: " + (currentRollout.Count() + currentBranchDepth));
                    break;//HACK delete this to continue search after finding a result;
                }
                if (updateTopScore)
                {
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
                if (iterations > /*50000*/100000000000)
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
            if (bestRollout != null && bestRollout.Count()>0)
            {
                bestMove = bestRollout.First<IPuzzleMove>();
                bestRollout.RemoveAt(0);
            }
            else
            {
                bestMove = rootNode.GetBestMove();
            }
            //Remove first move from rollout so that if the topScore is not beaten we can just take the next move

            //Console.WriteLine("Top score so far: " + topScore);
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
