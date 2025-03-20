using System.Diagnostics;
using System.Globalization;
/********************************************************************************
 * 
 * GA (Tournament Selection) - O(G × P × N)	
 * Tournament selection requires checking T candidates per selection, but since T is often constant or proportional to P, 
 * we simplify it to O(G × P × N), assuming N operations per evaluation (e.g., computing board conflicts).
 * 
 * Proposed Algorithm (Adam & Eve + Parallelism) - O((G × P × N) / C)	
 * If computations are parallelized across C cores, the effective per-core workload decreases by a factor of C, 
 * leading to an expected speedup of approximately C times.
 * 
 ********************************************************************************/
namespace NQueen
{
    class Program
    {
        // Enumeration for algorithm choices.
        enum AlgorithmType { Genetic, Tournament }

        /// <summary>
        /// Prompts the user to enter the number of queens (board size), ensuring valid input.
        /// Accepts values between 4 and 512, or 'Q' to exit the program.
        /// </summary>
        /// <returns>
        /// Integer representing the number of queens entered by the user,
        /// or -1 if the user chooses to quit.
        /// </returns>
        static int GetNumberOfQueens()
        {
            int nQueens;
            while (true)
            {
                Console.Write("\nEnter number of queens (4-512) or 'Q' to quit: ");
                string input = Console.ReadLine()?.Trim().ToUpper();
                if (input == "Q")
                    return -1;
                if (int.TryParse(input, out nQueens) && nQueens >= 4 && nQueens <= 512)
                    return nQueens;
                Console.WriteLine("Invalid input. Please enter a valid number.");
            }
        }

        /// <summary>
        /// Prompts the user to enter the number of queens (board size), ensuring valid input.
        /// Accepts values between 4 and 512, or 'Q' to exit the program.
        /// </summary>
        /// <returns>
        /// Integer representing the number of queens entered by the user,
        /// or -1 if the user chooses to quit.
        /// </returns>
        static (AlgorithmType algorithm, bool debugMode, int maxSeed) GetCustomParameters()
        {
            Console.Write("\nChoose Algorithm: [1] Genetic Algorithm Solver | [2] Tournament Solver: ");
            AlgorithmType algorithm = Console.ReadLine()?.Trim() == "2" ? AlgorithmType.Tournament : AlgorithmType.Genetic;

            Console.Write("\nEnable debug output? (true/false): ");
            bool debugMode = Console.ReadLine()?.Trim().ToLower() == "true";

            Console.Write("\nEnter maximum seed iteration value (0-100, or 0 for single execution): ");
            int maxSeed = 0;
            if (int.TryParse(Console.ReadLine()?.Trim(), out int seed) && seed >= 0)
                maxSeed = Math.Min(seed, 100);
            else
                Console.WriteLine("Invalid seed range. Defaulting to 10.");

            return (algorithm, debugMode, maxSeed);
        }

        // Main execution entry point for N-Queens solver application.
        static void Main(string[] args)
        {
            if (args.Length >= 3)
            {
                int nQueens = int.Parse(args[0]);
                AlgorithmType algorithm = args[1] == "Genetic" ? AlgorithmType.Genetic : AlgorithmType.Tournament;
                int seed = int.Parse(args[2]);
                bool debugMode = args.Length > 3 ? bool.Parse(args[3]) : false;

                Console.WriteLine($"\nRunning Automated: N={nQueens}, Algorithm={algorithm}, Seed={seed}");

                ISolver solver = algorithm == AlgorithmType.Genetic
                    ? new GASolver(nQueens, seed, debugMode)
                    : new TournamentSolver(nQueens, seed, debugMode);

                ExportSummary(solver, algorithm.ToString(), nQueens, seed);
                return; // Exit after execution
            }

            Console.WriteLine("Starting N-Queens Solver...");

            // Main loop allows user to repeatedly test different N values and configurations.
            while (true)
            {
                // Prompt user for number of queens (N), or quit.
                int nQueens = GetNumberOfQueens();
                if (nQueens == -1)
                {
                    // Ask user if they want to analyze solver results
                    Console.Write("\nWould you like to analyze the solver results and generate charts? (Y/n): ");
                    bool runAnalysis = Console.ReadLine()?.Trim().ToLower() != "n";

                    if (runAnalysis)
                    {
                        RunPythonScript();
                    }
                    break;
                }

                // Determine if custom parameters (algorithm type, debug mode, seed iterations, best fit) will be used.
                Console.Write("\nWould you like to specify custom parameters? (Y/n): ");
                bool specifyCustom = Console.ReadLine()?.Trim().ToLower() == "y";

                AlgorithmType algorithm;
                bool debugMode;
                int maxSeed;

                if (specifyCustom)
                {
                    (algorithm, debugMode, maxSeed) = GetCustomParameters();
                }
                else
                {
                    // Default configuration if custom settings aren't provided.
                    algorithm = AlgorithmType.Genetic;
                    debugMode = false;
                    maxSeed = 0;
                }

                Console.WriteLine($"\nUsing parameters: Algorithm = {algorithm}, Debug = {debugMode}, Seeds = {maxSeed}.");

                Stopwatch totalStopwatch = Stopwatch.StartNew();
                ISolver bestSolver = null;
                long bestTime = long.MaxValue;

                // This loop handles both single (maxSeed=0) and multi-seed scenarios uniformly.
                Parallel.For(0, maxSeed + 1, seedValue =>
                {
                    Stopwatch seedStopwatch = Stopwatch.StartNew();

                    // Initialize the solver based on the user's algorithm choice.
                    ISolver currentSolver = algorithm == AlgorithmType.Genetic
                        ? new GASolver(nQueens, seedValue, debugMode)
                        : new TournamentSolver(nQueens, seedValue, debugMode);

                    seedStopwatch.Stop();
                    long elapsedTime = seedStopwatch.ElapsedMilliseconds;

                    lock (Console.Out)
                    {
                        // Output a short summary for each seed execution.
                        Console.WriteLine($"Seed {seedValue}, Queens {nQueens}, Time: {Extensions.ToTimeString(seedStopwatch.Elapsed)}");

                        // Export the solver summary data to CSV for later regression analysis.
                        ExportSummary(currentSolver, algorithm.ToString(), nQueens, seedValue);

                        // Track the best solver (fastest time) across all seeds.
                        if (elapsedTime < bestTime)
                        {
                            bestTime = elapsedTime;
                            bestSolver = currentSolver;
                        }
                    }
                });

                // Once all seeds are processed, display the best solver details clearly.
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\nBest Result (Seed with shortest runtime: {Extensions.ToTimeString(TimeSpan.FromMilliseconds(bestTime))}):");
                Console.WriteLine($"Algorithm Used: {algorithm}");
                Console.WriteLine($"Number of Generations: {bestSolver.Generations}");
                Console.ResetColor();

                bestSolver.DisplayBoard();
                Console.WriteLine(bestSolver.Summary);
                totalStopwatch.Stop();
                Console.WriteLine($"\nTotal Process Time: {Extensions.ToTimeString(totalStopwatch.Elapsed)}");
            }
        }

        /// <summary>
        /// Exports solver summary details to a CSV file, including failures and intersections.
        /// Creates a new file with headers if not present.
        /// </summary>
        /// <param name="solver">Solver instance implementing ISolver.</param>
        /// <param name="algorithmType">Algorithm type name (GA or Tournament).</param>
        /// <param name="nQueens">Number of queens on the board.</param>
        /// <param name="seed">Seed used for randomization.</param>
        private static void ExportSummary(ISolver solver, string algorithmType, int nQueens, int seed)
        {
            try
            {
                // Define file path outside bin directory
                string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
                string folderPath = Path.Combine(projectRoot, "Data");
                string filePath = Path.Combine(folderPath, "summary.csv");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                bool fileExists = File.Exists(filePath);

                // Add "Intersections" to the header if file is newly created
                if (!fileExists)
                {
                    File.WriteAllText(filePath,
                        "Id,DateTime,AlgorithmType,NQueens,Seed,MutationRate,FinalMutationRate,Generations,PopulationSize,Complexity,ElapsedTimeSeconds,StagnationCount,StagnationMutationThresholdHigh,StagnationMutationThresholdLow,Intersections,Failures\n");
                }

                int nextId = fileExists ? File.ReadAllLines(filePath).Length : 1;
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                double elapsedTimeSeconds = solver.ElapsedMilliseconds / 1000.0;

                // Capture the number of intersections in the best solution
                int intersections = solver.BestSolution.GetIntersections();

                // Determine failure (if intersections > 0)
                int failure = intersections > 0 ? 1 : 0;

                string line = string.Join(",",
                    nextId,
                    timestamp,
                    algorithmType,
                    nQueens,
                    seed,
                    solver.MutationRate.ToString("F3"),
                    solver.FinalMutationRate.ToString("F3"), // Track final mutation after adaptations
                    solver.Generations,
                    solver.PopulationSize,
                    solver.Complexity.ToString("F2"),
                    elapsedTimeSeconds.ToString("F3"),
                    solver.StagnationCount, // Number of consecutive stagnant generations
                    solver.StagnationMutationThresholdHigh, // When mutation rate increased significantly
                    solver.StagnationMutationThresholdLow,  // When mutation rate increased slightly
                    intersections, // Total number of intersections (conflicts)
                    failure // Failure flag (1 = failure, 0 = success)
                );

                File.AppendAllText(filePath, line + Environment.NewLine);

                // Notify user if a solution failed
                if (failure == 1)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Solution for seed {seed} not exported. Intersections = {intersections}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error exporting CSV: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static void RunPythonScript()
        {
            // Get the project root directory
            string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
            string scriptPath = Path.Combine(projectRoot, "analyze_nqueens.py");
            string dataPath = Path.Combine(projectRoot, "Data");

            if (!File.Exists(scriptPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Python script not found at {scriptPath}");
                Console.ResetColor();
                return;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "py",  // Uses system's default Python version
                    Arguments = $"\"{scriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();

                    // Read and display Python output
                    while (!process.StandardOutput.EndOfStream)
                    {
                        Console.WriteLine(process.StandardOutput.ReadLine());
                    }

                    while (!process.StandardError.EndOfStream)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(process.StandardError.ReadLine());
                        Console.ResetColor();
                    }

                    process.WaitForExit();
                }

                // Automatically open all generated charts
                string[] chartFiles = { "elapsed_time_comparison.png", "mutation_vs_generations.png", "stagnation_vs_time.png" };
                foreach (string chart in chartFiles)
                {
                    string chartPath = Path.Combine(dataPath, chart);
                    if (File.Exists(chartPath))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = chartPath,
                            UseShellExecute = true // Opens with default image viewer
                        });
                    }
                }

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error running Python script: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}