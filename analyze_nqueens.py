import os
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
import numpy as np

# Define CSV path
project_root = os.path.dirname(os.path.abspath(__file__))
csv_file = os.path.join(project_root, "Data", "summary.csv")

# Ensure file exists
if not os.path.exists(csv_file):
    print("Error: summary.csv not found in Data folder.")
    exit(1)

# Load dataset
df = pd.read_csv(csv_file)

# Convert AlgorithmType to categorical for analysis
df['AlgorithmType'] = df['AlgorithmType'].astype('category')

# Convert columns that should be numeric
numeric_columns = [
    "Id", "NQueens", "Seed", "MutationRate", "FinalMutationRate", "Generations",
    "PopulationSize", "Complexity", "ElapsedTimeSeconds", "StagnationCount",
    "StagnationMutationThresholdHigh", "StagnationMutationThresholdLow", "Intersections", "Failures"
]
for col in numeric_columns:
    if col in df.columns:
        df[col] = pd.to_numeric(df[col], errors="coerce")

# Add failure metric (Failed solutions have Intersections > 0)
df['Failure'] = df['Intersections'] > 0

# Compute failure rates per algorithm type
failure_rates = df.groupby('AlgorithmType', observed=False)['Failure'].mean().reset_index()

# Set up Seaborn style
sns.set(style="whitegrid")

# Create output directory if it does not exist
output_dir = os.path.join(project_root, "Data")
os.makedirs(output_dir, exist_ok=True)

# ------------------------------
# Elapsed Time Comparison (Boxplot)
plt.figure(figsize=(8, 5))
sns.boxplot(x='AlgorithmType', y='ElapsedTimeSeconds', data=df)
plt.title("Elapsed Time Comparison: Genetic Algorithm vs Tournament Solver")
plt.ylabel("Elapsed Time (Seconds)")
plt.xlabel("Algorithm Type")
plt.savefig(os.path.join(output_dir, "elapsed_time_comparison.png"))
plt.close()

# ------------------------------
# Time Complexity Comparison (Scatter Plot)
plt.figure(figsize=(8, 5))
sns.scatterplot(x='NQueens', y='Complexity', hue='AlgorithmType', data=df, alpha=0.7)
plt.yscale("log")  # Log scale to handle large N values
plt.title("Time Complexity Comparison: Genetic Algorithm vs Tournament Solver")
plt.xlabel("Problem Size (N)")
plt.ylabel("Time Complexity (log scale)")
plt.savefig(os.path.join(output_dir, "time_complexity_comparison.png"))
plt.close()

# ------------------------------
# Failure Rate Comparison (Bar Plot)
plt.figure(figsize=(8, 5))
sns.barplot(x='AlgorithmType', y='Failure', data=failure_rates)
plt.title("Failure Rate Comparison: Genetic Algorithm vs Tournament Solver")
plt.ylabel("Failure Rate (%)")
plt.xlabel("Algorithm Type")
plt.ylim(0, 1)
plt.savefig(os.path.join(output_dir, "failure_rate_comparison.png"))
plt.close()

# ------------------------------
# New Analysis: Parameter Correlation Heatmap
# Select only numeric columns that might be relevant for tuning.
# Adjust column names as necessary (e.g., 'MutationRate', 'StagnationCount', etc.)
numeric_cols = df.select_dtypes(include=[np.number]).columns.tolist()

# If you want to exclude some columns (like seed or unrelated metrics), do it here.
# For example, if 'Seed' is not relevant:
if 'Seed' in numeric_cols:
    numeric_cols.remove('Seed')

plt.figure(figsize=(10, 8))
corr_matrix = df[numeric_cols].corr()
sns.heatmap(corr_matrix, annot=True, fmt=".2f", cmap="coolwarm")
plt.title("Correlation Heatmap of Performance Parameters")
plt.savefig(os.path.join(output_dir, "parameter_correlation_heatmap.png"))
plt.close()

# ------------------------------
# New Analysis: Average Elapsed Time for Valid Solutions
# Filter out failed runs (i.e., only consider valid solutions)
df_valid = df[df['Failure'] == False]

# Pass observed=False explicitly to retain current behavior
avg_elapsed = df_valid.groupby('AlgorithmType', observed=False)['ElapsedTimeSeconds'].mean().reset_index()

plt.figure(figsize=(8, 5))
# Assign 'AlgorithmType' as hue to satisfy the palette warning
ax = sns.barplot(x='AlgorithmType', y='ElapsedTimeSeconds', data=avg_elapsed, hue='AlgorithmType', palette="viridis")
# Remove redundant legend if it exists
legend = ax.get_legend()
if legend is not None:
    legend.remove()
plt.title("Average Elapsed Time (Valid Solutions Only)")
plt.ylabel("Average Elapsed Time (Seconds)")
plt.xlabel("Algorithm Type")

# Annotate each bar with the corresponding failure rate
for index, row in avg_elapsed.iterrows():
    alg = row['AlgorithmType']
    elapsed = row['ElapsedTimeSeconds']
    failure_rate = failure_rates[failure_rates['AlgorithmType'] == alg]['Failure'].values[0]
    ax.text(index, elapsed, f"\nFailure Rate: {failure_rate*100:.1f}%",
            color='black', ha="center", va='bottom', fontsize=10)

plt.savefig(os.path.join(output_dir, "avg_elapsed_time_valid.png"))
plt.close()

print("Updated charts saved in Data folder.")

df = pd.read_csv("Data/summary.csv")
partial_df = pd.concat([df.head(5), df.tail(5)])
partial_df.to_csv("Data/summaryReduced.csv", index=False)
print("Exported summaryReduced.csv successfully.")
