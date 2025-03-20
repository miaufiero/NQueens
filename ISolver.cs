namespace NQueen
{
    /// <summary>
    /// Represents a generic interface for N-Queens solvers to ensure consistent interaction.
    /// </summary>
    public interface ISolver
    {
        /// <summary>Gets the solution board as an integer array.</summary>
        int[] GetSolution();

        /// <summary>Number of generations used.</summary>
        int Generations { get; }

        /// <summary>Elapsed time of the solver.</summary>
        string ElapsedTime { get; }
        
        /// <summary>Elapsed milliseconds of the solver.</summary>
        double ElapsedMilliseconds { get; }

        /// <summary>Complexity of the solution.</summary>
        double Complexity { get; }

        /// <summary>Summary of solver performance.</summary>
        string Summary { get; }

        /// <summary>Displays the board.</summary>
        void DisplayBoard();

        /// <summary>Best board found during solving.</summary>
        Board BestSolution { get; }

        /// <summary>Mutation rate at solver completion.</summary>
        double MutationRate { get; }

        /// <summary>Population size used by the solver.</summary>
        int PopulationSize { get; }

        /// <summary>
        /// Final mutation rate after solver execution, useful for adaptive mutation analysis.
        /// </summary>
        double FinalMutationRate { get; }

        /// <summary>
        /// Number of generations where no improvement occurred, used to analyze stagnation behavior.
        /// </summary>
        int StagnationCount { get; }

        /// <summary>
        /// Threshold at which mutation rate was significantly increased due to stagnation.
        /// </summary>
        int StagnationMutationThresholdHigh { get; }

        /// <summary>
        /// Lower threshold where minor mutation rate increases were applied due to stagnation.
        /// </summary>
        int StagnationMutationThresholdLow { get; }

    }
}