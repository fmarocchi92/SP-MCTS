﻿using Common;
using Common.Abstract;
using MCTS2016.Puzzles.SameGame;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCTS2016
{
    class SinglePlayerMCTSMain
    {

        static TextWriter textWriter;
        /// <summary>
        /// - game
        /// - constC
        /// - constD
        /// - time per search in minutes
        /// - iterations per search
        /// - number of randomized restarts
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
            double searchTime;
            if (!double.TryParse(args[3], out searchTime))
            {
                PrintInputError("search time requires a double value");
                return;
            }
            int iterations;
            if (!int.TryParse(args[4], out iterations))
            {
                PrintInputError("iterations requires an integer value");
                return;
            }
            int restarts;
            if (!int.TryParse(args[5], out restarts))
            {
                PrintInputError("number of restarts requires an integer value");
                return;
            }
            uint seed;
            if (!uint.TryParse(args[6], out seed))
            {
                PrintInputError("seed requires an unsigned integer value");
                return;
            }
            RNG.Seed(seed);
            string logPath = args[7];
            textWriter = File.AppendText(logPath);
            string levelPath = args[8];
            Log("BEGIN TASK: " + game+" - const_C: "+const_C + " - const_D: " + const_D+" - minutes per move: "+searchTime+" - iterations per move: "+iterations + " - restarts: " + restarts);
            if (game.Equals("sokoban")){
                
            }else if (game.Equals("samegame"))
            {
                SamegameTest(const_C, const_D, searchTime, iterations, restarts, levelPath);
            }
            else
            {
                PrintInputError("Game must have value 'sokoban' or 'samegame'");
            }
            textWriter.Close();
        }

        private static void PrintInputError(string errorMessage)
        {
            Console.WriteLine(errorMessage+".\nArguments list:\n - game\n -const_C\n -const_D\n -time per search\n -iterations per search\n -seed\n -log path\n -level path");
            if (textWriter != null)
            {
                textWriter.Close();
            }
            return;
        }

        private static void SokobanTest(double const_C, double const_D, double searchTime, int iterations, int restarts, string levelPath)
        {
            
        }

        private static void SamegameTest(double const_C, double const_D, double searchTime, int iterations, int restarts, string levelPath)
        {
            string[] levels = ReadSamegameLevels(levelPath);
            int totalScore = 0;
            for (int i = 0; i < levels.Length; i++)
            {
                ISimulationStrategy simulationStrategy = new SamegameTabuColorRandomStrategy(levels[i]);
                
                //Log(s.PrettyPrint());
                int maxScore = int.MinValue;
                for (int restartN = 0; restartN < restarts; restartN++)
                {
                    SamegameGameState s = new SamegameGameState(levels[i], simulationStrategy);
                    IGameMove move;
                    ISimulationStrategy player = new SamegameMCTSStrategy(iterations, searchTime, null, const_C, const_D);
                    //double startTime = DateTime.Now.TimeOfDay.TotalSeconds;//used to keep track of the time needed to solve each level
                    while (!s.isTerminal())
                    {

                        move = player.selectMove(s);
                        //Log(move);
                        s.DoMove(move);
                        //Log(s.GetScore(1));
                        //Log(s.PrettyPrint());
                    }
                    maxScore = Math.Max(maxScore, s.GetScore(0));
                }
                //Log("Final configuration level " + (i + 1) + ": \n" + s.PrettyPrint()); //this describes the last run, not the best one
                Log("Score level " + (i + 1) + ": " + maxScore);
                
                totalScore += maxScore;
                //double elapsedTime = DateTime.Now.TimeOfDay.TotalSeconds - startTime;
                //Log("Time elapsed level " + (i+1) + ": " + Math.Truncate(elapsedTime / 60) + " minutes and " + Math.Truncate(elapsedTime % 60) + " seconds\n");
            }
            Log("TotalScore: " + totalScore);
            Log("TASK COMPLETED");
        }

        private static string[] ReadSamegameLevels(string levelPath)
        {
            StreamReader reader = File.OpenText(levelPath);
            string fullString = reader.ReadToEnd();
            reader.Close();
            string[] levels = fullString.Split(new string[] {"#"}, StringSplitOptions.RemoveEmptyEntries);
            return levels;
        }

        public static void Log(string logMessage, bool autoFlush = true)
        {
            textWriter.WriteLine("{0} - {1}  :  {2}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString(), logMessage);
            if (autoFlush)
            {
                textWriter.Flush();
            }
        }
    }
}