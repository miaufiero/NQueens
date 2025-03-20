import itertools
import os
import subprocess

# Dynamically determine project root (up three levels from this script)
project_root = os.path.abspath(os.path.dirname(__file__))

# Define path to the compiled executable
exe_path = os.path.join(project_root, "bin", "Release", "net8.0", "NQueensV1.exe")
analysis_script = os.path.join(project_root, "analyze_nqueens.py")

# Ensure the executable exists
if not os.path.exists(exe_path):
    print(f"Error: Executable not found at {exe_path}")
    exit(1)

# Define parameters to iterate over
n_values = [4, 6, 8, 10, 12, 14, 16, 18, 20, 24, 32, 50, 64, 80]  # Different board sizes to test
algorithms = ["Genetic", "Tournament"]  # Algorithm types
seeds = range(0, 100)  # Run x different seeds
debug_mode = False  # Keep debug mode off for large runs

# Iterate over all parameter combinations
for n, algorithm, seed in itertools.product(n_values, algorithms, seeds):
    print(f"\nRunning N-Queens Solver: N={n}, Algorithm={algorithm}, Seed={seed}")

    try:
        # Construct the command to execute Driver with parameters
        process = subprocess.run(
            [exe_path, str(n), algorithm, str(seed), str(debug_mode)],
            capture_output=True, text=True
        )

        # Print output to track execution
        print(process.stdout)

        # If errors occur, print them
        if process.stderr:
            print("Error:", process.stderr)

    except Exception as e:
        print(f"Execution failed: {e}")

# Run analysis script after all experiments complete
if os.path.exists(analysis_script):
    print("\nRunning analysis on results...")
    subprocess.run(["python", analysis_script])
else:
    print("Analysis script not found. Skipping analysis.")

print("\nAll executions completed! Check `summary.csv` and generated charts.")
