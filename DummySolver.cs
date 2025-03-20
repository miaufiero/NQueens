using System;

namespace NQueen
{
    /// <summary>
    /// DummySolver is used to encapsulate and store the summary results 
    /// of solver executions, especially when comparing multiple seeds or algorithms.
    /// It doesn't perform computations but serves as a structured holder of solver data.
    /// </summary>
    internal class DummySolver : ISolver
    {
        /// <summary>
        /// The best solution state found by the solver.
        /// </summary>
        public int[] BestSolution { get; }

        /// <summary>
        /// Number of generations solver took to find the solution.
        /// </summary>
        public int Generations { get; }

        /// <summary>
        /// Formatted elapsed time for solving.
        /// </summary>
        public string ElapsedTime { get; }

        /// <summary>
        /// Computed complexity value (simplified for DummySolver).
        /// </summary>
        public double Complexity { get; }

        /// <summary>
        /// Seed value used in the solver.
        /// </summary>
        public int Seed { get; }

        /// <summary>
        /// DummySolver does not use mutation; defaulted to 0.0.
        /// </summary>
        public double MutationRate => 0.0;

        /// <summary>
        /// DummySolver does not have population size; defaulted to 0.
        /// </summary>
        public int PopulationSize => 0;

        /// <summary>
        /// Gets the total elapsed execution time of the solver in milliseconds, used for accurate numeric analysis.
        /// </summary>
        public double ElapsedMilliseconds { get; private set; }

        /// <summary>
        /// Final mutation rate used in the actual solver execution.
        /// </summary>
        public double FinalMutationRate { get; }

        /// <summary>
        /// Stagnation count recorded from the actual solver execution.
        /// </summary>
        public int StagnationCount { get; }

        /// <summary>
        /// High threshold mutation increase value used.
        /// </summary>
        public int StagnationMutationThresholdHigh { get; }

        /// <summary>
        /// Low threshold mutation increase value used.
        /// </summary>
        public int StagnationMutationThresholdLow { get; }

        /// <summary>
        /// Initializes DummySolver with all stored values.
        /// </summary>
        public DummySolver(int nQueens, int seed, int generations, long elapsedMs, int[] bestSolution,
                           double finalMutationRate, int stagnationCount, int stagnationHigh, int stagnationLow)
        {
            Seed = seed;
            Generations = generations;
            ElapsedTime = Extensions.ToTimeString(TimeSpan.FromMilliseconds(elapsedMs));
            Complexity = (double)(Generations * nQueens);
            BestSolution = bestSolution;

            // Store stagnation and mutation-related parameters
            FinalMutationRate = finalMutationRate;
            StagnationCount = stagnationCount;
            StagnationMutationThresholdHigh = stagnationHigh;
            StagnationMutationThresholdLow = stagnationLow;
        }


        /// <summary>
        /// Initializes a new DummySolver with summary details.
        /// </summary>
        /// <param name="nQueens">Number of queens used for complexity calculation.</param>
        /// <param name="seed">Seed used for the solver.</param>
        /// <param name="generations">Number of generations.</param>
        /// <param name="elapsedMs">Elapsed milliseconds.</param>
        /// <param name="bestSolution">Best solution found.</param>
        public DummySolver(int nQueens, int seed, int generations, long elapsedMs, int[] bestSolution)
        {
            Seed = seed;
            Generations = generations;
            ElapsedTime = Extensions.ToTimeString(TimeSpan.FromMilliseconds(elapsedMs));
            Complexity = (double)(Generations * nQueens);
            BestSolution = bestSolution;
        }

        /// <summary>
        /// Returns the solution state.
        /// </summary>
        public int[] GetSolution() => BestSolution;

        /// <summary>
        /// Generates a summary of the solver's execution.
        /// </summary>
        public string Summary =>
            $"=== Dummy Solver Summary ===\n" +
            $"Seed: {Seed}\n" +
            $"Generations: {Generations}\n" +
            $"Elapsed Time: {ElapsedTime}\n" +
            $"Complexity: {Complexity:F2}\n" +
            "Note: Dummy solver instance stores summary data only, no computation performed.";

        /// <summary>
        /// Explicit implementation to satisfy ISolver interface; returns the best solution as Board object.
        /// </summary>
        Board ISolver.BestSolution => BestSolution != null ? new Board(BestSolution) : null;

        /// <summary>
        /// Displays the stored best solution board to the console.
        /// </summary>
        public void DisplayBoard()
        {
            if (BestSolution != null)
                new Board(BestSolution).DisplayBoard();
            else
                Console.WriteLine("No solution to display.");
        }
    }
}
