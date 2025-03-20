using System.Management;
using System.Runtime.InteropServices;

namespace NQueen
{
    /// <summary>
    /// Represents an immutable board configuration for the N-Queens problem.
    /// Once created, the board state does not change; thus, the number of conflicts (intersections)
    /// is computed once and cached for efficiency.
    /// 
    /// Time Complexity for initial conflict calculation: O(n²) (done once per board).
    /// Space Complexity: O(n)
    /// </summary>
    public class Board : IComparable<Board>
    {
        private readonly int[] state;
        private readonly int intersections; // Cached value computed during construction

        /// <summary>
        /// Gets the board state as an array.
        /// </summary>
        public int[] State { get; private set; }

        /// <summary>
        /// Constructs a Board from a given state (array of queen positions). The board is immutable.
        /// </summary>
        /// <param name="newState">An array where each element represents the column position for a queen in that row.</param>
        public Board(int[] newState)
        {
            state = (int[])newState.Clone();
            State = (int[])newState.Clone();
            intersections = CalcIntersections();
        }

        /// <summary>
        /// Compares boards first by their cached conflict (intersection) count, then by each element of their state.
        /// Time Complexity: O(n)
        /// </summary>
        public int CompareTo(Board other)
        {
            int cmp = this.GetIntersections().CompareTo(other.GetIntersections());
            if (cmp != 0)
                return cmp;
            for (int i = 0; i < state.Length; i++)
            {
                cmp = state[i].CompareTo(other.state[i]);
                if (cmp != 0)
                    return cmp;
            }
            return 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is Board other)
            {
                if (state.Length != other.state.Length)
                    return false;
                for (int i = 0; i < state.Length; i++)
                {
                    if (state[i] != other.state[i])
                        return false;
                }
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            foreach (int pos in state)
            {
                hash = hash * 31 + pos;
            }
            return hash;
        }

        /// <summary>
        /// Returns the cached number of diagonal conflicts.
        /// Time Complexity: O(1)
        /// </summary>
        public int GetIntersections()
        {
            return intersections;
        }

        /// <summary>
        /// Returns a fitness score based on the number of conflicts.
        /// Time Complexity: O(1)
        /// </summary>
        public double GetFitness()
        {
            return intersections == 0 ? 1000.0 : 1.0 / (1.0 + intersections);
        }

        /// <summary>
        /// Returns a clone of the board state.
        /// Time Complexity: O(n)
        /// </summary>
        public int[] GetState() => (int[])state.Clone();

        /// <summary>
        /// Calculates the number of diagonal conflicts in the board.
        /// Time Complexity: O(n²)
        /// </summary>
        private int CalcIntersections()
        {
            int count = 0;
            int n = state.Length;
            for (int i = 0; i < n - 1; i++)
                for (int j = i + 1; j < n; j++)
                    if (Math.Abs(state[i] - state[j]) == Math.Abs(i - j))
                        count++;
            return count;
        }

        /// <summary>
        /// Prints the board state as an array.
        /// Time Complexity: O(n)
        /// </summary>
        public void PrintArray()
        {
            Console.Write("[");
            for (int i = 0; i < state.Length; i++)
            {
                Console.Write(state[i]);
                if (i < state.Length - 1)
                    Console.Write(", ");
            }
            Console.WriteLine("]");
        }

        public override string ToString() => ToString(3);

        /// <summary>
        /// Returns a formatted string representation of the board.
        /// Time Complexity: O(n²)
        /// </summary>
        public string ToString(int spaces)
        {
            int n = state.Length;
            string boardStr = "";
            string cellSpacing = new string(' ', spaces);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    boardStr += (state[i] == j ? "Q" : "-") + cellSpacing;
                boardStr = boardStr.TrimEnd() + "\n";
            }
            return boardStr;
        }

        /// <summary>
        /// Returns a simple string representation (one character per cell).
        /// Time Complexity: O(n²)
        /// </summary>
        public string SimpleToString()
        {
            int n = state.Length;
            string result = "";
            for (int i = 0; i < n; i++)
            {
                char[] row = new string('-', n).ToCharArray();
                row[state[i]] = 'Q';
                result += new string(row) + "\n";
            }
            return result;
        }

        /// <summary>
        /// Displays the board in a pretty format if the console is large enough;
        /// otherwise, uses a simple display.
        /// Time Complexity: O(n²)
        /// </summary>
        public void DisplayBoard()
        {
            int n = state.Length;
            int reqWidthComplex = 4 * n + 1;
            int reqWidthSimple = n;
            
            if (Console.WindowWidth >= reqWidthComplex)
            {
                PrintRows(0, n);
                PrintHorizontalBorder(n);
            }
            else if (Console.WindowWidth >= reqWidthSimple)
            {
                Console.WriteLine(SimpleToString());
            }
            else
            {
                Console.WriteLine("Board too large to display in pretty format.");
            }
        }

        private void PrintRows(int row, int n)
        {
            if (row >= n)
                return;
            PrintHorizontalBorder(n);
            Console.Write("|");
            PrintCells(row, 0, n);
            Console.WriteLine();
            PrintRows(row + 1, n);
        }

        private void PrintCells(int row, int col, int n)
        {
            if (col >= n)
                return;
            Console.Write(state[row] == col ? " Q |" : "   |");
            PrintCells(row, col + 1, n);
        }

        private void PrintHorizontalBorder(int n)
        {
            for (int i = 0; i < n; i++)
                Console.Write("+---");
            Console.WriteLine("+");
        }

        /// <summary>
        /// Maximizes the console window.
        /// </summary>
        public static void MaximizeConsole()
        {
            IntPtr handle = GetConsoleWindow();
            ShowWindow(handle, SW_MAXIMIZE);
        }

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_MAXIMIZE = 3;
    }
}