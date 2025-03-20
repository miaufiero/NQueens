using System.Diagnostics;

namespace NQueen
{
    /// <summary>
    /// TournamentSolver uses a Tournament-Based Genetic Algorithm (non-parallel) to solve the N-Queens problem.
    /// Output is now suppressed; summary information is exposed via properties.
    /// </summary>
    internal class TournamentSolver : ISolver
    {
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
        private int[] bestSolution;

        /// <summary>
        /// Thread-local random number generator ensuring thread-safe randomness.
        /// </summary>
        private ThreadLocal<Random> rand;

        /// <summary>
        /// Number of boards (solutions) considered simultaneously.
        /// </summary>
        private int PopulationSize;

        /// <summary>
        /// Gets the total elapsed execution time of the solver in milliseconds, used for accurate numeric analysis.
        /// </summary>
        public double ElapsedMilliseconds { get; private set; }

        /// <summary>
        /// Constants for controlling Tournament-based Genetic Algorithm behavior.
        /// </summary>
        private const int MaxGenerations = 10000; // Max generations before stopping.

        // Explicit implementation for ISolver to ensure all interface methods are clearly satisfied:

        /// <summary>
        /// Retrieves the best solution board found during solver execution.
        /// </summary>
        Board ISolver.BestSolution => bestSolution != null ? new Board(bestSolution) : null;

        /// <summary>
        /// Mutation rate for TournamentSolver (fixed at 0 as the algorithm doesn't adaptively mutate).
        /// </summary>
        public double MutationRate => 0.0;

        /// <summary>
        /// Population size used during solver execution.
        /// </summary>
        int ISolver.PopulationSize => PopulationSize;

        /// <summary>
        /// Number of generations the TournamentSolver executed.
        /// </summary>
        int ISolver.Generations => Generations;

        /// <summary>
        /// Formatted elapsed time of the solver execution.
        /// </summary>
        string ISolver.ElapsedTime => ElapsedTime;

        /// <summary>
        /// Computed complexity estimation of solver execution.
        /// </summary>
        double ISolver.Complexity => Complexity;

        /// <summary>
        /// Since TournamentSolver does not adaptively mutate, FinalMutationRate remains 0.
        /// </summary>
        public double FinalMutationRate => 0.0;

        /// <summary>
        /// Tournament solver does not track stagnation explicitly, defaulting to 0.
        /// </summary>
        public int StagnationCount { get; private set; }

        /// <summary>
        /// Tournament solver does not use high mutation rate thresholds, so remains at 0.
        /// </summary>
        public int StagnationMutationThresholdHigh => 0;

        /// <summary>
        /// Tournament solver does not use low mutation rate thresholds, so remains at 0.
        /// </summary>
        public int StagnationMutationThresholdLow => 0;

        /// <summary>
        /// Tracks how many times the population has been completely reinitialized due to stagnation.
        /// </summary>
        public int ReinitializationCount { get; private set; } = 0;

        /// <summary>
        /// The number of candidates selected per tournament round.
        /// </summary>
        private readonly int TournamentSize;

        /// <summary>
        /// Controls the output of detailed debugging information.
        /// </summary>
        protected static bool? _debug = null;

        public static bool? ShowDebug
        {
            get => _debug ?? false;
            set => _debug = value;
        }

        private const int StagnationResetThreshold = 5000;  // Threshold to reinitialize the population

        /// <summary>
        /// Provides a comprehensive, formatted summary of the Tournament Genetic Algorithm execution,
        /// including configuration parameters, algorithm performance, complexity details,
        /// and explanation of complexity notation for clarity.
        /// </summary>
        public string Summary
        {
            get
            {
                int nQueens = GetSolution().Length;
                string coreSummary = Extensions.GetCoreSummary();

                // Details on Tournament algorithm-specific computational complexity calculation.
                string timeComplexityDetail = $"O(G x P x T): (G={Generations}, P={PopulationSize}, T={TournamentSize}) = {Complexity}";

                return $"=== Tournament Solver Summary ===\n" +
                       $"N (Queens): {nQueens}\n" +
                       $"Generations (G): {Generations}\n" +
                       $"Population Size (P): {PopulationSize}\n" +
                       coreSummary +
                       $"Elapsed Time: {ElapsedTime}\n" +
                       $"Computed Complexity: {Complexity:F2}\n\n" +
                       "Algorithm Analysis:\n" +
                       $"Time Complexity: {timeComplexityDetail}\n" +
                       $"  where G = Generations, P = Population Size, T = Tournament Size\n" +
                       $"Space Complexity: O(P × N): {PopulationSize * N}";
            }
        }

        /// <summary>
        /// Initializes a new instance of the Tournament-based Genetic Algorithm solver.
        /// Configures algorithm parameters, initializes the initial population, and executes
        /// the evolutionary process until a valid solution is found or the generation limit is reached.
        /// </summary>
        /// <param name="nQueens">Number of queens (size of board N×N).</param>
        /// <param name="seed">Optional seed value for reproducibility. If null, random seed is used.</param>
        /// <param name="debug">Determines if debug mode (verbose output) is enabled.</param>
        /// <param name="findBest">Determines if the algorithm should continue searching for the optimal solution after the first valid solution.</param>
        public TournamentSolver(int nQueens, int? seed = null, bool debug = false)
        {
            N = nQueens;
            Stopwatch stopwatch = new Stopwatch();
            rand = new ThreadLocal<Random>(() => seed.HasValue ? new Random(seed.Value) : new Random());
            PopulationSize = Math.Min(N * 5, 5000);

            // Define tournament size based on population, Increased selection pressure
            TournamentSize = GetTournamentSize(); //TournamentSize = Math.Max(5, PopulationSize / 10); // updated from Math.Max(3, PopulationSize / 20); due to too many failures

            List<Board> population = InitializePopulation();
            Generations = 1;
            stopwatch.Start();

            //  Ensure that the best solution selected has 0 intersections
            bestSolution = population.OrderBy(b => b.GetIntersections()).ThenByDescending(b => b.GetFitness()).FirstOrDefault()?.GetState();


            while (Generations <= MaxGenerations)
            {
                if (population.Count > 0 && population[0].GetIntersections() == 0)
                {
                    if (ShowDebug.Value)
                        Console.WriteLine($"Valid solution found at generation {Generations}.");
                    break;
                }

                // Fix: Compare against the best solution found so far
                if (population[0].GetIntersections() >= (bestSolution != null ? new Board(bestSolution).GetIntersections() : int.MaxValue))
                {
                    StagnationCount++;
                }
                else
                {
                    StagnationCount = 0; // Reset stagnation count when an improvement is found
                    bestSolution = population[0].GetState(); // Update bestSolution to track progress
                }

                // If stagnation persists for too long, reset the population
                if (StagnationCount > StagnationResetThreshold)
                {
                    Console.WriteLine("Stagnation detected. Replacing bottom 50% of the population...");

                    int replaceCount = PopulationSize / 2;
                    for (int i = 0; i < replaceCount; i++)
                    {
                        population[PopulationSize - 1 - i] = new Board(GenerateRandomBoard());
                    }
                    StagnationCount = 0;
                }


                population = EvolvePopulationTournament(population);
                Generations++;

                if (population.Count == 0)
                {
                    if(ShowDebug.Value)
                        Console.WriteLine("WARNING: Population was wiped out. Reinitializing...");
                    population = InitializePopulation();
                    bestSolution = population[0].GetState();
                    ReinitializationCount++; // Track how often this happens
                }
            }
            stopwatch.Stop();
            Complexity = (double)(Generations * PopulationSize * N);
            ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            ElapsedTime = Extensions.ToTimeString(stopwatch.Elapsed);
        }

        /// <summary>
        /// Calculates an adaptive tournament size based on population size and stagnation count.
        /// This helps balance selection pressure dynamically:
        /// - Higher stagnation count leads to a smaller tournament size (more random selection).
        /// - A smaller stagnation count leads to stronger selection pressure.
        /// </summary>
        /// <returns>The dynamically adjusted tournament size.</returns>
        private int GetTournamentSize()
        {
            return Math.Max(3, PopulationSize / (10 + StagnationCount));
        }

        /// <summary>
        /// Retrieves the best solution as an array of integers representing queen positions.
        /// If no solution is found, returns a default empty board configuration.
        /// </summary>
        /// <returns>
        /// An integer array where each element indicates the column position of the queen in each row.
        /// </returns>
        public int[] GetSolution() => bestSolution ?? new int[N];
        
        /// <summary>
        /// Initializes the starting population for the genetic algorithm by generating random board states.
        /// </summary>
        /// <returns>A list of randomly generated Board objects forming the initial population.</returns>
        private List<Board> InitializePopulation()
        {
            return Enumerable.Range(0, PopulationSize)
                .Select(_ => new Board(GenerateRandomBoard()))
                .ToList();
        }

        /// <summary>
        /// Generates a randomly shuffled board state representing a potential solution.
        /// </summary>
        /// <returns>An array representing queen positions for a randomized board.</returns>
        private int[] GenerateRandomBoard()
        {
            return Enumerable.Range(0, N)
                .OrderBy(_ => rand.Value.Next())
                .ToArray();
        }

        /// <summary>
        /// Evolves the current population by performing tournament selection, crossover, and mutation,
        /// resulting in a new population for the next generation.
        /// </summary>
        /// <param name="population">Current population to evolve.</param>
        /// <returns>The newly evolved population.</returns>
        private List<Board> EvolvePopulationTournament(List<Board> population)
        {
            var newPopulation = new List<Board>();
            for (int i = 0; i < PopulationSize / 2; i++)
            {
                var parents = TournamentSelection(population);
                var children = Crossover(parents);
                newPopulation.Add(new Board(Mutate(children[0])));
                newPopulation.Add(new Board(Mutate(children[1])));
            }
            return newPopulation;
        }

        /// <summary>
        /// Selects two parent solutions from the population using tournament selection.
        /// This method prioritizes selecting valid boards first (Intersections == 0).
        /// If no valid solutions exist in the tournament subset, it falls back to the least bad ones.
        /// Ensures that at least two parents are always returned.
        /// </summary>
        /// <param name="population">The current population of boards.</param>
        /// <returns>A list containing the two best-selected boards.</returns>
        private List<Board> TournamentSelection(List<Board> population)
        {
            int tournamentSize = GetTournamentSize();
            List<Board> tournament = new List<Board>();

            for (int i = 0; i < tournamentSize; i++)
            {
                tournament.Add(population[rand.Value.Next(population.Count)]);
            }

            // First, check for valid solutions (intersections == 0)
            var validBoards = tournament.Where(b => b.GetIntersections() == 0).ToList();

            if (validBoards.Count >= 2)
            {
                // If multiple valid solutions exist, select the two best based on fitness
                return validBoards.OrderByDescending(b => b.GetFitness()).Take(2).ToList();
            }
            else if (validBoards.Count == 1)
            {
                // If there's only one valid board, pair it with the least bad option
                var leastBad = tournament.OrderBy(b => b.GetIntersections()).ThenByDescending(b => b.GetFitness()).FirstOrDefault();
                return new List<Board> { validBoards[0], leastBad ?? validBoards[0] };
            }

            // If no valid boards exist, return the two least bad ones (ensures at least one solution is improving)
            return tournament
                .OrderBy(b => b.GetIntersections())  // Prioritize lower intersections
                .ThenByDescending(b => b.GetFitness())  // Break ties with fitness
                .Take(2)
                .ToList();
        }

        /// <summary>
        /// Performs crossover between two parent boards to produce offspring solutions.
        /// Ensures that at least two parents exist before attempting crossover.
        /// If only one parent is available, returns duplicates to prevent errors.
        /// </summary>
        /// <param name="parents">A list containing exactly two parent boards.</param>
        /// <returns>A list containing two child states resulting from crossover.</returns>
        private List<int[]> Crossover(List<Board> parents)
        {
            if (parents.Count < 2)
            {
                Console.WriteLine("⚠ Warning: Not enough parents selected for crossover. Returning duplicates.");
                return new List<int[]> { parents[0].GetState(), parents[0].GetState() };
            }

            int[] parent1 = parents[0].GetState();
            int[] parent2 = parents[1].GetState();
            int[] child1 = new int[N];
            int[] child2 = new int[N];

            int start = rand.Value.Next(N / 3);
            int end = start + rand.Value.Next(N / 3, N - start);

            Array.Copy(parent1, start, child1, start, end - start);
            Array.Copy(parent2, start, child2, start, end - start);

            return new List<int[]> { child1, child2 };
        }

        /// <summary>
        /// Applies mutation by randomly swapping two positions in the board state.
        /// Mutation introduces genetic diversity into the population.
        /// </summary>
        /// <param name="state">The original state array representing a potential solution.</param>
        /// <returns>A mutated board state.</returns>
        private int[] Mutate(int[] state)
        {
            if (rand.Value.Next(2) == 0)
            {
                int idx1 = rand.Value.Next(N);
                int idx2;
                do { idx2 = rand.Value.Next(N); } while (idx1 == idx2);
                (state[idx1], state[idx2]) = (state[idx2], state[idx1]);
            }
            return state;
        }

        /// <summary>
        /// Displays the best solution board state visually in the console.
        /// If no solution has been found, nothing is displayed.
        /// </summary>
        public void DisplayBoard()
        {
            if (bestSolution != null)
            {
                Board b = new Board(bestSolution);
                b?.DisplayBoard();
                Console.Write("1D Representation: ");
                b?.PrintArray();
            }
        }
    }
}