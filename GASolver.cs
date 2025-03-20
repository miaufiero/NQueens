using System.Diagnostics;

namespace NQueen
{
    /// <summary>
    /// GASolver implements a Genetic Algorithm (GA) to solve the N-Queens problem.
    /// It uses an "Adam & Eve" selection method, adaptive mutation, and parallel computation.
    /// Output and results display are managed externally by the Driver class.
    /// </summary>
    internal class GASolver : ISolver
    {
        /// <summary>
        /// Shared global Random instance to ensure reproducibility when using a seed.
        /// </summary>
        private static Random globalRand;

        /// <summary>
        /// The number of queens on the chessboard (board size, N×N).
        /// </summary>
        private readonly int N;

        /// <summary>
        /// Random seed used for reproducibility of solver runs.
        /// </summary>
        private readonly int SEED;

        /// <summary>
        /// The total number of generations the algorithm executed before finding a solution.
        /// </summary>
        public int Generations;

        /// <summary>
        /// Computed complexity metric estimating computational effort.
        /// </summary>
        public double Complexity { get; private set; }

        /// <summary>
        /// The total execution time for the solver in a readable format.
        /// </summary>
        public string ElapsedTime { get; private set; }

        /// <summary>
        /// Stores the best board solution found during the algorithm's execution.
        /// </summary>
        protected Board bestSolution;

        /// <summary>
        /// Maintains the population sorted by fitness (number of conflicts).
        /// </summary>
        private SortedSet<Board> sortedPopulation;

        /// <summary>
        /// Thread-local random number generator ensuring thread safety during parallel operations.
        /// </summary>
        private readonly ThreadLocal<Random> threadRand;

        /// <summary>
        /// Current mutation rate, adjusted adaptively during algorithm execution.
        /// </summary>
        private double MutationRate = InitialMutationRate;

        /// <summary>
        /// Represents the best individual in the population ("Adam").
        /// </summary>
        private Board adam;

        /// <summary>
        /// Represents the second-best individual in the population ("Eve").
        /// </summary>
        private Board eve;

        internal int PopulationSize;  // made internal so Driver can display if desired

        // Constants controlling the Genetic Algorithm behavior:
        private const double InitialMutationRate = 0.2;  // Initial mutation probability
        private const double MaxMutationRate = 0.4;  // Maximum allowable mutation rate
        private const int StagnationResetThreshold = 5000;  // Threshold to reinitialize the population
        private const int StagnationMutationIncreaseThresholdHigh = 200; // High threshold to significantly increase mutation rate
        private const int StagnationMutationIncreaseThresholdLow = 50;   // Low threshold to slightly increase mutation rate
        private const int MaxGenerations = 10000;  // Upper limit on generation count before stopping execution

        /// <summary>
        /// Controls the output of detailed debugging information.
        /// </summary>
        protected static bool? _debug = null;

        public static bool? ShowDebug
        {
            get => _debug ?? false;
            set => _debug = value;
        }

        // Explicit implementation for ISolver to ensure all interface methods are clearly satisfied:

        /// <summary>
        /// Gets the best solution board found.
        /// </summary>
        Board ISolver.BestSolution => bestSolution;

        /// <summary>
        /// Mutation rate at the end of solver execution.
        /// </summary>
        double ISolver.MutationRate => MutationRate;

        /// <summary>
        /// Population size used in genetic algorithm execution.
        /// </summary>
        int ISolver.PopulationSize => PopulationSize;

        /// <summary>
        /// Number of generations processed by the solver.
        /// </summary>
        int ISolver.Generations => Generations;

        /// <summary>
        /// Elapsed time for solver execution in a user-friendly format.
        /// </summary>
        string ISolver.ElapsedTime => ElapsedTime;

        /// <summary>
        /// Computed complexity estimation.
        /// </summary>
        double ISolver.Complexity => Complexity;

        /// <summary>
        /// Gets the total elapsed execution time of the solver in milliseconds, used for accurate numeric analysis.
        /// </summary>
        public double ElapsedMilliseconds { get; private set; }

        /// <summary>
        /// Final mutation rate at the end of solver execution.
        /// </summary>
        public double FinalMutationRate => MutationRate;

        /// <summary>
        /// Number of consecutive generations without improvement.
        /// </summary>
        public int StagnationCount { get; private set; }

        /// <summary>
        /// High stagnation mutation threshold reached.
        /// </summary>
        public int StagnationMutationThresholdHigh => StagnationMutationIncreaseThresholdHigh;

        /// <summary>
        /// Low stagnation mutation threshold reached.
        /// </summary>
        public int StagnationMutationThresholdLow => StagnationMutationIncreaseThresholdLow;


        /// <summary>
        /// Generates a detailed, formatted string summarizing the execution of the Genetic Algorithm solver,
        /// including algorithm parameters, solver performance, and complexity metrics.
        /// </summary>
        public string Summary
        {
            get
            {
                string coreSummary = Extensions.GetCoreSummary();

                // Details on Tournament algorithm-specific computational complexity calculation.
                string timeComplexityDetail = $"O(G x P x T)/C: (G={Generations}, P={PopulationSize}, N={N})/(C={Extensions.LogicalCores} = {Complexity}";

                return
                    $"=== Solver Summary ===\n" +
                    $"Algorithm: GA (Adam & Eve)\n" +
                    $"N (Queens): {N}\n" +
                    $"Seed: {SEED}\n" +
                    $"Mutation Rate: {MutationRate:F3}\n" +
                    $"Generations: {Generations}\n" +
                    $"Population Size: {PopulationSize}\n" +
                       coreSummary +
                    $"Elapsed Time: {ElapsedTime}\n" +
                    $"Computed Complexity: {Complexity:F2}\n" +
                    $"Fitness: {bestSolution.GetFitness():F4}\n" +
                    $"Intersections: {bestSolution.GetIntersections()}\n\n" +
                    "Algorithm Analysis:\n" +
                    $"Time Complexity: O(G × P × N / C) = O(({Generations} x {PopulationSize} x {N})/{Extensions.LogicalCores}) = {Complexity}\n" +
                    "Space Complexity: O(population size)\n";
            }
        }

        /// <summary>
        /// Retrieves the solution state as an integer array representing queen positions.
        /// Returns an empty array if no valid solution was found.
        /// </summary>
        /// <returns>The best solution's board state array.</returns>
        public int[] GetSolution()
        {
            return bestSolution != null ? (int[])bestSolution.GetState().Clone() : Array.Empty<int>();
        }

        /// <summary>
        /// Initializes a new instance of the Genetic Algorithm solver for the N-Queens problem.
        /// Configures algorithm parameters, initializes population, runs the evolution process,
        /// manages stagnation handling, and computes complexity metrics.
        /// </summary>
        /// <param name="nQueens">Number of queens (board dimension) to solve for.</param>
        /// <param name="seed">Optional random seed for reproducibility. Randomized if not provided.</param>
        /// <param name="debug">Enables detailed debugging output if set to true.</param>
        public GASolver(int nQueens, int? seed = null, bool debug = false)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            ShowDebug = debug;
            N = nQueens;
            SEED = seed ?? new Random().Next();
            globalRand = new Random(SEED);
            PopulationSize = Math.Max(50, Math.Min(N * 10, 2000));

            threadRand = new ThreadLocal<Random>(() => new Random(globalRand.Next()));
            List<Board> population = InitializePopulation();

            Generations = 1;
            int bestFitness = population.Min(b => b.GetIntersections());
            int noImprovementCounter = 0;


            while (Generations <= MaxGenerations)
            {
                int newBest = population.Min(b => b.GetIntersections());
                population = EvolvePopulation(population, noImprovementCounter);
                Generations++;
                if (newBest < bestFitness)
                {
                    bestFitness = newBest;
                    noImprovementCounter = 0;
                    MutationRate = InitialMutationRate;
                }
                else
                    noImprovementCounter++;

                if (noImprovementCounter > StagnationResetThreshold)
                {
                    population = InitializePopulation();
                    noImprovementCounter = 0;
                }
                else if (noImprovementCounter > StagnationMutationIncreaseThresholdHigh)
                    MutationRate = Math.Min(MutationRate * 1.1, MaxMutationRate);
                else if (noImprovementCounter > StagnationMutationIncreaseThresholdLow)
                    MutationRate = Math.Min(MutationRate * 1.05, MaxMutationRate);

                // Early termination if a valid solution is found
                if (population.Any(b => b.GetIntersections() == 0))
                    break;
            }

            Complexity = (double)(Generations * PopulationSize * N) / Environment.ProcessorCount;
            bestSolution = population.Where(b => b.GetIntersections() == 0).OrderBy(b => b.GetIntersections()).FirstOrDefault();

            // (If debug, you could write a debug message—but for final output the Driver will handle it.)
            stopwatch.Stop();
            ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            ElapsedTime = Extensions.ToTimeString(stopwatch.Elapsed);
        }

        /// <summary>
        /// Displays the best solution board visually to the console. 
        /// Output formatting and colorization are handled externally in the Driver.
        /// </summary>
        public void DisplayBoard()
        {
            bestSolution?.DisplayBoard();
            Console.Write("1D Representation: ");
            bestSolution?.PrintArray();
        }

        /// <summary>
        /// Deprecated: Previously handled console output of solver execution details.
        /// Now, all outputs are managed externally in the Driver class.
        /// </summary>
        /// <param name="population">The final solver population (unused, method retained for backward compatibility).</param>
        public void ShowOutput(List<Board> population)
        {
            // (This method is now deprecated—output is handled by Driver.)
        }

        /// <summary>
        /// Internal helper class used to track the progress of population initialization.
        /// Used primarily for updating progress bars during lengthy initialization processes.
        /// </summary>
        private class ProgressData
        {
            public int CurrentCount;
            public bool Done;
        }

        private int attemptCount = 0;
        /// <summary>
        /// Generates an initial population of random boards, optionally showing a progress bar.
        /// </summary>
        /// <param name="useProgressBar">If true, shows a progress bar indicating initialization progress.</param>
        /// <returns>List of randomly initialized Board objects.</returns>
        private List<Board> InitializePopulation(bool useProgressBar = true)
        {
            // For simplicity, only show brief reinitialization info if not in detailed debug mode.
            if (useProgressBar)
            {
                Console.WriteLine($"\rReinitializing population... Attempt {++attemptCount}");
            }
            int progressBarWidth = 50;
            ProgressData progressData = new ProgressData() { CurrentCount = 0, Done = false };

            int[] seeds = new int[PopulationSize];
            for (int i = 0; i < PopulationSize; i++)
            {
                seeds[i] = globalRand.Next();
            }

            Thread progressThread = null;
            // If detailed progress is desired and debug is true, start a progress thread.
            if (ShowDebug.Value && useProgressBar && PopulationSize >= 100)
            {
                progressThread = new Thread(() =>
                {
                    while (!progressData.Done)
                    {
                        double progress = (double)progressData.CurrentCount / PopulationSize;
                        int pos = (int)(progress * progressBarWidth);
                        Console.Write($"\r[{new string('#', pos)}{new string('-', progressBarWidth - pos)}] {progress:P0}");
                        Thread.Sleep(100);
                    }
                    Console.Write($"\r[{new string('#', progressBarWidth)}] 100%\n");
                });
                progressThread.Start();
            }

            Board[] boards = new Board[PopulationSize];
            Parallel.For(0, PopulationSize, i =>
            {
                Random localRand = new Random(seeds[i]);
                int[] board = GenerateRandomBoard(localRand);
                boards[i] = new Board(board);
                Interlocked.Increment(ref progressData.CurrentCount);
            });

            progressData.Done = true;
            progressThread?.Join();

            return boards.ToList();
        }

        /// <summary>
        /// Evolves the current population by selecting parents (Adam & Eve), performing crossover and mutation,
        /// and generating a new population.
        /// </summary>
        /// <param name="population">The current list of boards in the population.</param>
        /// <param name="stagnationCounter">The number of generations without improvement.</param>
        /// <returns>The evolved population for the next generation.</returns>
        private List<Board> EvolvePopulation(List<Board> population, int stagnationCounter)
        {
            if (population.Count == 0)
            {
                if (ShowDebug.Value)
                    Console.WriteLine("WARNING: Evolution failed. Reinitializing...");
                return InitializePopulation();
            }

            // **Always reassign Adam & Eve from sortedPopulation**
            MaintainSortedPopulation(population);
            adam = GetAdam();
            eve = GetEve();

            if (stagnationCounter > 100)
            {
                if (ShowDebug.Value)
                    Console.WriteLine("Adam & Eve seem stuck inside EvolvePopulation(). Resetting Eve...");
                eve = new Board(Mutate(adam.GetState(), 10));
            }

            Board[] newPopulation = new Board[PopulationSize];
            newPopulation[0] = adam;
            newPopulation[1] = eve;
            int numPairs = (PopulationSize - 2) / 2;

            Parallel.For(0, numPairs, i =>
            {
                List<int[]> children = (threadRand.Value.NextDouble() < 0.5) ? CrossoverPMX(new List<Board> { adam, eve }) : CrossoverOX(new List<Board> { adam, eve });
                Board child1 = new Board(Mutate(children[0], stagnationCounter));
                Board child2 = new Board(Mutate(children[1], stagnationCounter));
                newPopulation[2 + i * 2] = child1;
                newPopulation[2 + i * 2 + 1] = child2;
            });
            return newPopulation.ToList();
        }

        /// <summary>
        /// Sorts and maintains the population, ensuring the two best solutions (Adam & Eve) are always present.
        /// If insufficient valid solutions exist, a fresh population is generated.
        /// </summary>
        /// <param name="population">The current population list to maintain.</param>
        private void MaintainSortedPopulation(List<Board> population)
        {
            if (population == null || population.Count < 2)
            {
                population = InitializePopulation();
            }
            sortedPopulation = new SortedSet<Board>(population);
            if (sortedPopulation.Count < 2)
            {
                adam = new Board(GenerateRandomBoard(threadRand.Value));
                eve = new Board(Mutate(adam.GetState(), 5));
                sortedPopulation.Add(adam);
                sortedPopulation.Add(eve);
            }
            else
            {
                adam = sortedPopulation.First();
                eve = sortedPopulation.Skip(1).FirstOrDefault();
            }
            if (adam == null || eve == null)
            {
                adam = new Board(GenerateRandomBoard(threadRand.Value));
                eve = new Board(Mutate(adam.GetState(), 5));
                sortedPopulation = new SortedSet<Board> { adam, eve };
            }
        }

        /// <summary>
        /// Retrieves the best-performing board ("Adam") from the sorted population.
        /// </summary>
        private Board GetAdam() => sortedPopulation.Min;

        /// <summary>
        /// Retrieves the second-best board ("Eve") from the sorted population.
        /// </summary>
        private Board GetEve() => sortedPopulation.Skip(1).FirstOrDefault();

        /// <summary>
        /// Performs Partially Mapped Crossover (PMX) between two parent boards.
        /// </summary>
        /// <param name="parents">List containing exactly two parent boards.</param>
        /// <returns>List containing two child states after crossover.</returns>
        private List<int[]> CrossoverPMX(List<Board> parents)
        {
            int[] parent1 = parents[0].GetState();
            int[] parent2 = parents[1].GetState();
            int[] child1 = new int[N];
            int[] child2 = new int[N];
            int start = threadRand.Value.Next(N / 3);
            int end = start + threadRand.Value.Next(N / 3, N - start);
            Array.Copy(parent1, start, child1, start, end - start);
            Array.Copy(parent2, start, child2, start, end - start);
            HashSet<int> used1 = new HashSet<int>(child1[start..end]);
            HashSet<int> used2 = new HashSet<int>(child2[start..end]);
            int index1 = 0, index2 = 0;
            for (int i = 0; i < N; i++)
            {
                if (i >= start && i < end) continue;
                while (used1.Contains(parent2[index1])) index1++;
                while (used2.Contains(parent1[index2])) index2++;
                child1[i] = parent2[index1++];
                child2[i] = parent1[index2++];
            }
            return new List<int[]> { child1, child2 };
        }

        /// <summary>
        /// Performs Order Crossover (OX) between two parent boards.
        /// </summary>
        /// <param name="parents">List containing exactly two parent boards.</param>
        /// <returns>List containing two child states after crossover.</returns>
        private List<int[]> CrossoverOX(List<Board> parents)
        {
            int[] parent1 = parents[0].GetState();
            int[] parent2 = parents[1].GetState();
            int[] child1 = new int[N];
            int[] child2 = new int[N];
            int start = threadRand.Value.Next(N / 4);
            int end = start + threadRand.Value.Next(N / 4, N - start);
            Array.Copy(parent1, start, child1, start, end - start);
            Array.Copy(parent2, start, child2, start, end - start);
            HashSet<int> used1 = new HashSet<int>(child1[start..end]);
            HashSet<int> used2 = new HashSet<int>(child2[start..end]);
            int index1 = 0, index2 = 0;
            for (int i = 0; i < N; i++)
            {
                if (i >= start && i < end) continue;
                while (index1 < N && used1.Contains(parent2[index1])) index1++;
                while (index2 < N && used2.Contains(parent1[index2])) index2++;
                if (index1 < N) child1[i] = parent2[index1++];
                if (index2 < N) child2[i] = parent1[index2++];
            }
            return new List<int[]> { child1, child2 };
        }

        /// <summary>
        /// Applies mutation to a given board state, with mutation rate adjusted adaptively based on stagnation.
        /// </summary>
        /// <param name="state">The original board state to mutate.</param>
        /// <param name="stagnationCounter">Count of generations without improvement to adjust mutation rate.</param>
        /// <returns>A mutated state array.</returns>
        private int[] Mutate(int[] state, int stagnationCounter)
        {
            double adaptiveMutationRate = MutationRate + Math.Log(stagnationCounter + 1) * 0.05;
            adaptiveMutationRate = Math.Min(adaptiveMutationRate, 0.9);
            if (threadRand.Value.NextDouble() < adaptiveMutationRate)
            {
                int idx1 = threadRand.Value.Next(N);
                int idx2;
                do { idx2 = threadRand.Value.Next(N); } while (idx2 == idx1);
                int[] newState = (int[])state.Clone();
                (newState[idx1], newState[idx2]) = (newState[idx2], newState[idx1]);
                return newState;
            }
            return state;
        }

        /// <summary>
        /// Attempts to generate a random board configuration ensuring minimal conflicts.
        /// Retries multiple times before defaulting to a sequential arrangement.
        /// </summary>
        /// <param name="localRand">Random instance for thread-safe randomness.</param>
        /// <returns>An integer array representing queen positions.</returns>
        private int[] GenerateRandomBoard(Random localRand)
        {
            int maxAttempts = 1000;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int[] board = new int[N];
                HashSet<int> usedPositions = new HashSet<int>();
                bool valid = true;
                for (int i = 0; i < N; i++)
                {
                    List<int> availablePositions = Enumerable.Range(0, N).Where(p => !usedPositions.Contains(p)).ToList();
                    availablePositions.FisherYatesShuffle(localRand);
                    bool placed = false;
                    foreach (int pos in availablePositions)
                    {
                        board[i] = pos;
                        if (IsValidPartialSolution(board, i))
                        {
                            usedPositions.Add(pos);
                            placed = true;
                            break;
                        }
                    }
                    if (!placed)
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid)
                {
                    return board;
                }
            }
            return Enumerable.Range(0, N).ToArray();
        }

        /// <summary>
        /// Checks if the board is valid up to the given row (no conflicts).
        /// </summary>
        /// <param name="board">Board state array to check.</param>
        /// <param name="row">Row index up to which validation is performed.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private bool IsValidPartialSolution(int[] board, int row)
        {
            for (int i = 0; i < row; i++)
            {
                if (board[i] == board[row])
                    return false;
                if (Math.Abs(board[i] - board[row]) == Math.Abs(i - row))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Validates the entire board configuration, ensuring no queens are conflicting.
        /// Used primarily for final solution verification.
        /// </summary>
        /// <param name="board">The complete board state array to validate.</param>
        /// <returns>True if the solution is valid, false otherwise.</returns>
        private static bool IsValidSolution(int[] board)
        {
            if (board == null || board.Length == 0)
            {
                if (ShowDebug.Value)
                    Console.WriteLine("DEBUG: Empty board detected.");
                return false;
            }
            int n = board.Length;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (Math.Abs(board[i] - board[j]) == Math.Abs(i - j))
                    {
                        if (ShowDebug.Value)
                            Console.WriteLine($"DEBUG: Conflict detected between positions {i} and {j}");
                        return false;
                    }
                }
            }
            if (ShowDebug.Value)
                Console.WriteLine("DEBUG: Board is valid.");
            return true;
        }
    }
}