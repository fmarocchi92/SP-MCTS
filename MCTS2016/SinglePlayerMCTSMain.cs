using Common;
using Common.Abstract;
using GraphAlgorithms;
using MCTS2016.Common.Abstract;
using MCTS2016.Puzzles.SameGame;
using MCTS2016.Puzzles.Sokoban;
using MCTS2016.SP_MCTS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MCTS2016
{
    class SinglePlayerMCTSMain
    {
        private static Object taskLock = new object();
        private static int[] taskTaken;
        private static int[] scores;
        private static List<IPuzzleMove>[] bestMoves;
        private static int currentTaskIndex=0;
        private static uint threadIndex;
        private static int restarts;
        static TextWriter textWriter;
        /// <summary>
        /// - game
        /// - constC
        /// - constD
        /// - iterations per search
        /// - number of randomized restarts
        /// - maximum number of threads
        /// - seed
        /// - log path
        /// - level path
        /// </summary>
        /// <param name="args">
        /// </param>
        public static void Main(string[] args)
        {
            if (args.Length < 9)
            {
                PrintInputError("Missing arguments");
                return;
            }
            string game = args[0];
            double const_C;
            if (!double.TryParse(args[1], out const_C))
            {
                PrintInputError("Const_C requires a double value");
                return;
            }
            double const_D;
            if (!double.TryParse(args[2], out const_D))
            {
                PrintInputError("Const_D requires a double value");
                return;
            }
            int iterations;
            if (!int.TryParse(args[3], out iterations))
            {
                PrintInputError("iterations requires an integer value");
                return;
            }
            int restarts;
            if (!int.TryParse(args[4], out restarts))
            {
                PrintInputError("number of restarts requires an integer value");
                return;
            }
            int maxThread;
            if (!int.TryParse(args[5], out maxThread))
            {
                PrintInputError("maximum number of threads requires an integer value");
                return;
            }
            uint seed;
            if (!uint.TryParse(args[6], out seed))
            {
                PrintInputError("seed requires an unsigned integer value");
                return;
            }

            bool abstractSokoban;
            if (!bool.TryParse(args[7], out abstractSokoban))
            {
                PrintInputError("abstract sokoban requires a boolean value");
                return;
            }
            //RNG.Seed(seed);
            string logPath = args[8];
            textWriter = File.AppendText(logPath);
            string levelPath = args[9];
            Log("BEGIN TASK: " + game + " - const_C: " + const_C + " - const_D: " + const_D + " - iterations per move: " + iterations + " - restarts: " + restarts + " - max threads: "+maxThread+" - abstract: "+abstractSokoban);

            if (game.Equals("sokoban"))
            {
                //ManualSokoban();
                SokobanTest(const_C, const_D, iterations, restarts, levelPath, seed, abstractSokoban);
                //MultiThreadSokobanTest(const_C, const_D, iterations, restarts, levelPath, maxThread, seed);
            }
            else if (game.Equals("samegame"))
            {

                MultiThreadSamegameTest(const_C, const_D, iterations, restarts, levelPath, maxThread, seed);

            }
            else
            {
                PrintInputError("Game must have value 'sokoban' or 'samegame'");
            }
            //textWriter.Close();
        }

        private static void PrintInputError(string errorMessage)
        {
            Console.WriteLine(errorMessage + ".\nArguments list:\n - game\n -const_C\n -const_D\n -iterations per search\n -number of randomized restarts\n -maximum number of threads\n -seed\n -log path\n -level path");
            if (textWriter != null)
            {
                textWriter.Close();
            }
        }

        private static void MultiThreadSokobanTest(double const_C, double const_D, int iterations, int restarts, string levelPath, int threadNumber, uint seed)
        {
            int threadCount = Math.Min(Environment.ProcessorCount, threadNumber);
            Thread[] threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() => SokobanTest(const_C, const_D, iterations, restarts, levelPath, seed, true));
                threads[i].Start();
            }
            for (int i = 0; i < threadCount; i++)
            {
                threads[i].Join();
            }
        }

        private static void SokobanTest(double const_C, double const_D, int iterations, int restarts, string levelPath, uint seed, bool abstractSokoban)
        {

            ///////////////////////////////////////////
            //int[][] costs = new int[4][];
            //costs[0] = new int[] {80,40,50,86 };
            //costs[1] = new int[] {40,70,20,25 };
            //costs[2] = new int[] { 30, 10, 20, 30 };
            //costs[3] = new int[] { 35, 20, 25, 30 };
            //int[][] costs2 = new int[4][];
            //costs2[0] = new int[] { 80, 40, 50, 86 };
            //costs2[1] = new int[] { 40, 70, 20, 25 };
            //costs2[2] = new int[] { 30, 10, 20, 30 };
            //costs2[3] = new int[] { 35, 20, 25, 30 };
            int[,] costs = new int[3, 3] { { 80, 40, 500 }, { 40, 70, 20 }, { 30, 15, 20 } };
            //costs[0] = new int[] { 80, 40, 500 };
            //costs[1] = new int[] { 40, 70, 20};
            //costs[2] = new int[] { 30, 15, 20};
            int[][] costs2 = new int[3][];
            //costs2[0] = (int[])costs[0].Clone();
            //costs2[1] = (int[])costs[1].Clone();
            //costs2[2] = (int[])costs[2].Clone();
            int[] pairings = new HungarianAlgorithm(costs).Run();
            int totalcost = 0;
            for(int i = 0; i < pairings.Length; i++)
            {
                totalcost += costs[i,pairings[i]];
            }

            ///////////////////////////////////////////


            string[] levels = ReadSokobanLevels(levelPath);
            uint threadIndex = GetThreadIndex();
            
            RNG.Seed(seed+threadIndex);
            MersenneTwister rng = new MersenneTwister(seed+threadIndex);
            ISPSimulationStrategy simulationStrategy;
            if (abstractSokoban)
            {
                simulationStrategy = new SokobanInertiaStrategy(rng, 0.6);
            }
            else
            {
                simulationStrategy = new SokobanRandomStrategy();
            }
            
            IPuzzleState[] states = new IPuzzleState[levels.Length];
            SokobanMCTSStrategy player = new SokobanMCTSStrategy(rng, iterations, 600, null, const_C, const_D);
            int solvedLevels = 0;
            for (int i = 0; i < states.Length; i++)
            {
                
                states[i] = new SokobanGameState(levels[i], simulationStrategy, abstractSokoban);
                Debug.WriteLine(states[i].PrettyPrint());

                List<IPuzzleMove> moveList = new List<IPuzzleMove>();
                player = new SokobanMCTSStrategy(rng, iterations, 600, null, const_C, const_D);
                Log("Level"+(i+1)+":\n" + states[i].PrettyPrint());

                string moves = "";

                moveList = player.GetSolution(states[i]);
                foreach (IPuzzleMove m in moveList)
                {
                    if (abstractSokoban)
                    {
                        //Log("Move: " + m);
                        SokobanPushMove push = (SokobanPushMove)m;
                        foreach (IPuzzleMove basicMove in push.MoveList)
                        {
                            moves += basicMove;
                        }
                        moves += push.PushMove;

                    }
                    else
                    {
                        moves += m;
                    }
                    states[i].DoMove(m);
                    //Log("\n" + states[i].PrettyPrint());
                    //Log("IsTerminal: " + states[i].isTerminal());
                }
                if (states[i].EndState())
                {
                    solvedLevels++;
                }
                Log("Level " + (i + 1) + " solved: " + (states[i].EndState()) + " solution length:" + moves.Count());
                Log("Moves: " + moves);
                Log("Solved "+solvedLevels+"/"+(i+1));
                Console.Write("\rSolved " + solvedLevels + "/" + (i + 1));
                //Log("Result:\n" + states[i].PrettyPrint());

                //IPuzzleMove move;
                //while (!states[i].isTerminal())
                //{
                //    move = player.selectMove(states[i]);
                //    states[i].DoMove(move);
                //    //Log("Move: " + move);
                //    moves += move;
                //    //Log("\n" + states[i].PrettyPrint());
                //    //Log("IsTerminal: " + states[i].isTerminal());
                //}
                //Log("Level " + (i + 1) + " solved: " + (states[i].EndState()) + " solution length:" + moves.Count());
                //Log("Moves: " + moves);
                //Log("Result:\n" + states[i].PrettyPrint());
                Log("Final score: " + states[i].GetScore());
            }
        }

        private static void ManualSokoban()
        {
            string level = " #####\n #   ####\n #   #  #\n ##    .#\n### ###.#\n# $ # #.#\n# $$# ###\n#@  #\n#####";
            //string level = "####\n# .#\n#  ###\n#*@  #\n#  $ #\n#  ###\n####";
            Log("Level:\n" + level);
            MersenneTwister rng = new MersenneTwister(1+threadIndex);
            ISPSimulationStrategy simulationStrategy = new SokobanRandomStrategy();
            SokobanGameState s = new SokobanGameState(level, simulationStrategy);
            SokobanGameState backupState = (SokobanGameState) s.Clone();
            bool quit = false;
            IPuzzleMove move=null;
            Console.WriteLine(s.PrettyPrint());
            while (!quit)
            {
                ConsoleKeyInfo input = Console.ReadKey();
                List<IPuzzleMove> moves = s.GetMoves();
                switch (input.Key)
                {
                    case ConsoleKey.UpArrow:
                        if(moves.Contains(new SokobanGameMove("u"))){
                            move = new SokobanGameMove("u");
                        }
                        else
                        {
                            move = new SokobanGameMove("U");
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        if (moves.Contains(new SokobanGameMove("d")))
                        {
                            move = new SokobanGameMove("d");
                        }
                        else
                        {
                            move = new SokobanGameMove("D");
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        if (moves.Contains(new SokobanGameMove("l")))
                        {
                            move = new SokobanGameMove("l");
                        }
                        else
                        {
                            move = new SokobanGameMove("L");
                        }
                        break;
                    case ConsoleKey.RightArrow:
                        if (moves.Contains(new SokobanGameMove("r")))
                        {
                            move = new SokobanGameMove("r");
                        }
                        else
                        {
                            move = new SokobanGameMove("R");
                        }
                        break;
                    case ConsoleKey.R:
                        s = (SokobanGameState) backupState.Clone();
                        move = null;
                        break;
                    case ConsoleKey.Q:
                        move = null;
                        quit = true;
                        break;
                }
                if (move != null)
                {
                    Console.WriteLine("Move: " + move);
                    s.DoMove(move);
                }
                    Console.WriteLine(s.PrettyPrint());
                    Console.WriteLine("Score: " + s.GetScore() + "  |  isTerminal: "+s.isTerminal());
                
                
            }
        }

        private static void MultiThreadSamegameTest(double const_C, double const_D, int iterations, int restarts, string levelPath, int threadNumber, uint seed)
        {
            string[] levels = ReadSamegameLevels(levelPath);
            taskTaken = new int[levels.Length];
            scores = new int[levels.Length];
            SinglePlayerMCTSMain.restarts = restarts;
            bestMoves = new List<IPuzzleMove>[levels.Length];
            for (int i = 0; i < scores.Length; i++)
            {
                scores[i] = int.MinValue;
            }
            int threadCount = Math.Min(Environment.ProcessorCount, threadNumber);
            Thread[] threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() => SamegameTest(const_C, const_D, iterations, restarts, levels, seed));
                threads[i].Start();
            }
            for (int i = 0; i < threadCount; i++)
            {
                threads[i].Join();
            }
            int totalScore = 0;
            Log("*** FINAL RESULT ***");
            for(int i = 0; i < scores.Length; i++)
            {
                totalScore += scores[i];
                Log("Level "+(i+1)+" score: "+scores[i]);
                PrintMoveList(i, bestMoves[i]);
            }
            Log("Total score:" + totalScore);
            textWriter.Close();
        }

        private static void SamegameTest(double const_C, double const_D, int iterations, int restarts, string[] levels, uint seed)
        {
            uint threadIndex = GetThreadIndex();
            Console.WriteLine("Thread "+ threadIndex +" started");
            MersenneTwister rnd = new MersenneTwister(seed+threadIndex);
            int currentLevelIndex = GetTaskIndex(threadIndex);
            while (currentLevelIndex >= 0)
            {
                ISPSimulationStrategy simulationStrategy = new SamegameTabuColorRandomStrategy(levels[currentLevelIndex],rnd);
                //Console.Write("\rRun " + (restartN + 1) + " of " + restarts + "  ");
                SamegameGameState s = new SamegameGameState(levels[currentLevelIndex], rnd, simulationStrategy);
                IPuzzleMove move;
                ISPSimulationStrategy player = new SamegameMCTSStrategy(rnd,iterations, 600, null, const_C, const_D);
                string moveString = string.Empty;
                List<IPuzzleMove> moveList = new List<IPuzzleMove>();
                while (!s.isTerminal())
                {
                    move = player.selectMove(s);
                    moveList.Add(move);
                    s.DoMove(move);
                }
                lock (taskLock)
                {
                    if (s.GetScore() > scores[currentLevelIndex])
                    {
                        scores[currentLevelIndex] = s.GetScore();
                        bestMoves[currentLevelIndex] = moveList;
                        Log("Completed run " + taskTaken[currentLevelIndex] + "/" + restarts + " of level " + (currentLevelIndex + 1) + ". New top score found: " + scores[currentLevelIndex]);
                        PrintMoveList(currentLevelIndex, moveList);
                        PrintBestScore();
                    }
                    else
                    {
                        Log("Completed run " + taskTaken[currentLevelIndex] + "/" + restarts + " of level " + (currentLevelIndex + 1) + " with score: " + s.GetScore());
                    }
                }
                currentLevelIndex = GetTaskIndex(threadIndex);
            }
            
        }

        private static string[] ReadSamegameLevels(string levelPath)
        {
            StreamReader reader = File.OpenText(levelPath);
            string fullString = reader.ReadToEnd();
            reader.Close();
            string[] levels = fullString.Split(new string[] { "#" }, StringSplitOptions.RemoveEmptyEntries);
            return levels;
        }

        private static string[] ReadSokobanLevels(string levelPath)
        {
            StreamReader reader = File.OpenText(levelPath);
            string fullString = reader.ReadToEnd();
            fullString = Regex.Replace(fullString, @"[\d]", string.Empty);
            reader.Close();
            string[] levels = fullString.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            return levels;
        }

            private static int GetTaskIndex( uint threadIndex)
        {
            lock (taskLock)
            {
                for (int i = 0; i < taskTaken.Length; i++)
                {
                    if (taskTaken[i] < restarts && i%SinglePlayerMCTSMain.threadIndex == threadIndex)
                    {
                        taskTaken[i]++;
                        return i;
                    }
                    if (taskTaken[i] == restarts )
                    {
                        taskTaken[i]++;
                        //Console.WriteLine("Level " + (i+1) + " completed");
                    }
                }
                return -1;
            }
        }

        private static uint GetThreadIndex()
        {
            lock (taskLock)
            {
                return threadIndex++;
            }
        }

        public static void PrintMoveList(int level, List<IPuzzleMove> moves)
        {
            for(int i = 0; i < moves.Count; i++)
            {
                Log("Level " + (level + 1) + " - move " + i + ": " + moves[i]);
            }
        }

        public static void PrintBestScore()
        {
            int partialScore = 0;
            int scoresCount = 0;
            for (int i = 0; i < scores.Length; i++)
            {
                if (scores[i] > int.MinValue)
                {
                    partialScore += scores[i];
                    scoresCount++;
                }
            }
            Log("Partial score : " + partialScore + " on " + scoresCount + " levels");
        }

        public static void Log(string logMessage, bool autoFlush = true)
        {
            lock (taskLock)
            {
                textWriter.WriteLine("{0} - {1}  :  {2}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString(), logMessage);
                if (autoFlush)
                {
                    textWriter.Flush();
                }
            }
        }
    }
}
