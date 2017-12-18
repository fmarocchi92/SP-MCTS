﻿using Common;
using Common.Abstract;
using MCTS2016.Puzzles.SameGame;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MCTS2016
{
    class SinglePlayerMCTSMain
    {
        private static Object taskLock = new object();
        private static int[] taskTaken;
        private static int[] scores;
        private static List<IGameMove>[] bestMoves;
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
            //RNG.Seed(seed);
            string logPath = args[7];
            textWriter = File.AppendText(logPath);
            string levelPath = args[8];
            Log("BEGIN TASK: " + game + " - const_C: " + const_C + " - const_D: " + const_D + " - iterations per move: " + iterations + " - restarts: " + restarts + " - max threads: "+maxThread);

            if (game.Equals("sokoban"))
            {

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

        private static void SokobanTest(double const_C, double const_D, int iterations, int restarts, string levelPath)
        {

        }

        private static void MultiThreadSamegameTest(double const_C, double const_D, int iterations, int restarts, string levelPath, int threadNumber, uint seed)
        {
            string[] levels = ReadSamegameLevels(levelPath);
            taskTaken = new int[levels.Length];
            scores = new int[levels.Length];
            SinglePlayerMCTSMain.restarts = restarts;
            bestMoves = new List<IGameMove>[levels.Length];
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
                ISimulationStrategy simulationStrategy = new SamegameTabuColorRandomStrategy(levels[currentLevelIndex],rnd);
                //Console.Write("\rRun " + (restartN + 1) + " of " + restarts + "  ");
                SamegameGameState s = new SamegameGameState(levels[currentLevelIndex], rnd, simulationStrategy);
                IGameMove move;
                ISimulationStrategy player = new SamegameMCTSStrategy(rnd,iterations, 600, null, const_C, const_D);
                string moveString = string.Empty;
                List<IGameMove> moveList = new List<IGameMove>();
                while (!s.isTerminal())
                {
                    move = player.selectMove(s);
                    moveList.Add(move);
                    s.DoMove(move);
                }
                lock (taskLock)
                {
                    if (s.GetScore(0) > scores[currentLevelIndex])
                    {
                        scores[currentLevelIndex] = s.GetScore(0);
                        bestMoves[currentLevelIndex] = moveList;
                        Log("Completed run " + taskTaken[currentLevelIndex] + "/" + restarts + " of level " + (currentLevelIndex + 1) + ". New top score found: " + scores[currentLevelIndex]);
                        PrintMoveList(currentLevelIndex, moveList);
                        PrintBestScore();
                    }
                    else
                    {
                        Log("Completed run " + taskTaken[currentLevelIndex] + "/" + restarts + " of level " + (currentLevelIndex + 1) + " with score: " + s.GetScore(0));
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

        public static void PrintMoveList(int level, List<IGameMove> moves)
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
