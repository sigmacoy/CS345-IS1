// MazeEnvironment.cs
using System.Collections.Generic;
namespace Aug19;

public class MazeEnvironment
{
    public struct Node
    {
        public int Row { get; }
        public int Col { get; }

        public Node(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public override string ToString() => $"{Row},{Col}";

        // Required for HashSet<Node> comparisons
        public override bool Equals(object? obj) => obj is Node n && Row == n.Row && Col == n.Col;
        public override int GetHashCode() => (Row, Col).GetHashCode();
    }
    
    public static class MazeSolver
    {
        private static readonly int[,] Grid =
        {
            {0, 0, 1, 0, 0},
            {0, 0, 1, 0, 0},
            {0, 1, 1, 0, 1},
            {0, 0, 0, 0, 0},
            {1, 1, 0, 1, 0}
        };

        public static int[,] GetGrid() => Grid;

        public static IEnumerable<Node> GetNeighbors(Node current)
        {
            int rows = Grid.GetLength(0);
            int cols = Grid.GetLength(1);
            var neighbors = new List<Node>();

            int[] dRow = { -1, 1, 0, 0 };
            int[] dCol = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int r = current.Row + dRow[i];
                int c = current.Col + dCol[i];

                if (r >= 0 && r < rows && c >= 0 && c < cols && Grid[r, c] == 0)
                {
                    neighbors.Add(new Node(r, c));
                }
            }
            return neighbors;
        }
    }
}