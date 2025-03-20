# NQueens
The N-Queens problem involves placing N queens on an N×N chessboard such that no two queens threaten each other. The objective is to develop an optimized algorithm addressing the weaknesses of existing solutions while ensuring scalability and efficiency.

# N-Queens Solver

## Overview
This project implements various algorithms to solve the **N-Queens problem**, a classic combinatorial optimization challenge. It includes solvers based on **Genetic Algorithms (GA)**, **Tournament Selection**, and **Adam & Eve selection strategies**. The project supports automated benchmarking and visualization of results.

## Features
- **Multiple Algorithms**: Includes **Genetic Algorithm Solver**, **Tournament Solver**, and an **Analysis Script**.
- **Parallel Computing**: Optimized execution using multi-threading for faster computation.
- **Data Analysis**: Generates CSV reports and visualizations using Python (`matplotlib`, `seaborn`).
- **Command-line Interface**: Allows users to configure problem size, algorithm selection, and debugging options.

## File Structure
```
📂 NQueensSolver
│── 📂 bin/                    # Compiled executable
│── 📂 Data/                   # CSV files and performance plots
│── 📂 src/                    # Source code for solvers and utilities
│   ├── Board.cs               # Represents the chessboard and queen placement
│   ├── Solver.cs              # Core solver with genetic algorithm logic
│   ├── GASolver.cs            # Genetic Algorithm-based solver
│   ├── TournamentSolver.cs    # Tournament selection-based solver
│   ├── Extensions.cs          # Helper functions (e.g., shuffling, formatting)
│   ├── ISolver.cs             # Interface defining solver methods
│   ├── Driver.cs              # Command-line interface for user interaction
│── run_nqueens.py             # Python script to automate solver runs
│── analyze_nqueens.py         # Python script to analyze and visualize results
│── README.md                  # Project documentation (this file)
│── .gitignore                 # Git ignore file
```

## Installation

### Prerequisites
- **.NET 8.0 SDK** (or later)
- **Python 3.x** with `pandas`, `matplotlib`, and `seaborn`
- **Windows, Linux, or macOS** (multi-platform support)

### Setup
1. Clone this repository:
   ```sh
   git clone https://github.com/yourusername/NQueensSolver.git
   cd NQueensSolver
   ```
2. Compile the C# project:
   ```sh
   dotnet build --configuration Release
   ```
3. Run the solver:
   ```sh
   dotnet run --project src/Driver.cs
   ```
4. Run an automated experiment:
   ```sh
   python run_nqueens.py
   ```
5. Analyze the results:
   ```sh
   python analyze_nqueens.py
   ```

## Usage
### Running the Solver
You can run the solver interactively or via command-line parameters.

#### Interactive Mode
```sh
dotnet run
```
- Enter the board size `N` (e.g., `8` for an 8x8 board).
- Select an algorithm:
  - `[1] Genetic Algorithm`
  - `[2] Tournament Solver`
- Enable or disable debug mode.
- Specify the number of random seed iterations.

#### Automated Execution
```sh
dotnet run -- 16 Genetic 42 true
```
- `16`: Board size (N=16)
- `Genetic`: Algorithm type (`Genetic` or `Tournament`)
- `42`: Random seed for reproducibility
- `true`: Enable debug output

### Running Batch Experiments
To execute multiple experiments automatically:
```sh
python run_nqueens.py
```
This will iterate through different values of `N`, solvers, and random seeds.

### Analyzing Results
Once experiments are complete, generate visualizations:
```sh
python analyze_nqueens.py
```
This will generate:
- **Elapsed Time Comparison** (`elapsed_time_comparison.png`)
- **Time Complexity Scatter Plot** (`time_complexity_comparison.png`)
- **Failure Rate Bar Chart** (`failure_rate_comparison.png`)

## Contribution
Feel free to contribute by:
- Optimizing the algorithms
- Adding new solver methods
- Improving visualization scripts

To contribute:
1. Fork the repository.
2. Create a new branch (`git checkout -b feature-name`).
3. Commit your changes (`git commit -am "Add new feature"`).
4. Push to the branch (`git push origin feature-name`).
5. Open a pull request.
