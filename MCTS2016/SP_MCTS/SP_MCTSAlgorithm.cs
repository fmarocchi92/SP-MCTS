using System.Text;
using System.Threading.Tasks;
using MCTS2016.SP_MCTS.SP_UCT;
using MCTS2016.Common.Abstract;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Diagnostics;
using System.Linq;

namespace MCTS2016.SP_MCTS
{
    class SP_MCTSAlgorithm
    {
        private ISPTreeNodeCreator treeCreator;
        private bool search = true;

        private List<IPuzzleMove> bestRollout;
        private double topScore = double.MinValue;
        private bool stopOnResult;

        private int iterationsExecuted;

        public int IterationsExecuted { get => iterationsExecuted; set => iterationsExecuted = value; }

        public SP_MCTSAlgorithm(ISPTreeNodeCreator treeCreator, bool stopOnResult)
        {
            this.treeCreator = treeCreator;
            this.stopOnResult = stopOnResult;
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
            iterationsExecuted = 0;
            

            for (iterationsExecuted = 0; iterationsExecuted < iterations; iterationsExecuted++)
            {
                ISPTreeNode node = rootNode;
                IPuzzleState state = rootState.Clone();
                HashSet<IPuzzleState> visitedStatesInRollout = new HashSet<IPuzzleState>() { state.Clone() };
                List<IPuzzleMove> currentRollout = new List<IPuzzleMove>();
                int currentBranchDepth = 0;
                // Select
                while (!node.HasMovesToTry() && node.HasChildren())
                {
                    node = node.SelectChild();
                    state.DoMove(node.Move);
                    visitedStatesInRollout.Add(state.Clone());
                    currentRollout.Add(node.Move);
                    currentBranchDepth++;
                }
                IPuzzleState backupState = state.Clone();
                // Expand
                if (node.HasMovesToTry())
                {
                    IPuzzleMove move = node.SelectUntriedMove();
                    if (move != -1)
                    {
                        //Debug.WriteLine("Expand");
                        //Debug.WriteLine(move);
                        //Debug.WriteLine(state.PrettyPrint());
                        state.DoMove(move);
                        if (visitedStatesInRollout.Contains(state))
                        {
                            List<IPuzzleMove> untriedMoves = new List<IPuzzleMove>(node.GetUntriedMoves());
                            int um = 0;
                            for (;um < untriedMoves.Count() && (visitedStatesInRollout.Contains(state)); um++)
                            {
                                state = backupState.Clone();
                                move = untriedMoves[um];
                                state.DoMove(move);
                                node.RemoveUntriedMove(move);
                            }
                            if (um < untriedMoves.Count())
                            {
                                node = node.AddChild(move, state);
                                currentRollout.Add(move);
                                nodeCount++;
                            }
                            else
                            {
                                //Console.WriteLine("NO untried moves available");
                                state = backupState;
                            }
                        }
                        else
                        {
                            node = node.AddChild(move, state);
                            currentRollout.Add(move);
                            nodeCount++;
                        }
                        visitedStatesInRollout.Add(state.Clone());
                    }
                    else
                    {
                        state.Pass();
                    }
                }
                //if a node is a dead end remove it from the tree and adde the state to the set of dead ends
                if(!node.HasChildren() && !node.HasMovesToTry() && !state.EndState())
                {
                    node.Parent.RemoveChild(node);
                }  

                

                
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
                        //Debug.WriteLine("Rollout1");
                        //Debug.WriteLine(move);
                        //Debug.WriteLine(state.PrettyPrint());
                        state.DoMove(move);


                        //Debug.WriteLine(state);
                        if (visitedStatesInRollout.Contains(state))
                        {
                            state = backupState.Clone();
                            List<IPuzzleMove> availableMoves = state.GetMoves();
                            int triedMoves = 0;
                            for (triedMoves = 0; triedMoves < availableMoves.Count() && visitedStatesInRollout.Contains(state); triedMoves++)
                            {
                                state = backupState.Clone();
                                move = availableMoves[triedMoves];
                                state.DoMove(move);
                            }
                            if (triedMoves == availableMoves.Count())
                            {
                                //Console.WriteLine("No untried move left in current rollout");
                                break;
                                
                            }
                            else
                            {
                                
                            }
                        }
                        currentRollout.Add(move);
                        visitedStatesInRollout.Add(state.Clone());
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
                    //Debug.WriteLine(state.PrettyPrint());
                }
                if (result > topScore || result == topScore && currentRollout.Count() < bestRollout.Count())
                {
                    topScore = result;
                    bestRollout = currentRollout ;
                    
                    if (state.EndState() && stopOnResult)
                    {
                        //SinglePlayerMCTSMain.Log("Solution found in " + iterationsExecuted + "rollouts");
                        break;
                    }
                }

                

                // Backpropagate
                while (node != null)
                {
                    node.Update(result);
                    node = node.Parent;
                }

                

                if (!search)
                {
                    search = true;
                    return null;
                }

                afterMemory = GC.GetTotalMemory(false);
                usedMemory = afterMemory - beforeMemory;
                averageUsedMemoryPerIteration = usedMemory / (iterationsExecuted + 1);

                var outStringToWrite = string.Format(" MCTS search: {0:0.00}% [{1} of {2}] - Total used memory b(mb): {3}({4}) - Average used memory per iteration b(mb): {5}({6:N7})", (float)((iterationsExecuted + 1) * 100) / (float)iterations, iterationsExecuted + 1, iterations, usedMemory, usedMemory / 1024 / 1024, averageUsedMemoryPerIteration, (float)averageUsedMemoryPerIteration / 1024 / 1024);
#if DEBUG
                if (iterations > 1000000000)
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
            if (bestRollout != null && bestRollout.Count() > 0)
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
