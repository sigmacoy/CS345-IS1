// MazeEnvironment.cs
using System.Collections.Generic;
namespace Aug24;

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
        // private static readonly int[,] Grid = new int[20, 20];
        
        // Use below for prelimDFS
        private static readonly int[,] Grid = new int[13, 10];
        
        static MazeSolver()
        {
            prelimDFS();
            // Randomize();
        }
        
        public static void Randomize()
        {
            var rand = new System.Random();
            for (int r = 0; r < 20; r++)
            {
                for (int c = 0; c < 20; c++)
                {
                    Grid[r, c] = rand.NextDouble() < 0.15 ? 1 : 0; 
                }
            }
    
            // Ensure start and goal are walkable
            Grid[0, 0] = 0; 
            Grid[19, 19] = 0;
        }

        public static void prelimDFS()
        {
            int[,] temp =
            {
                { 0,0,0,1,1,1,0,0,0,0 },    // 1
                { 0,0,1,0,0,0,1,1,0,0 },    // 11
                { 1,0,0,0,1,0,0,1,0,0 }, //    21 start 2, 3
                { 0,0,0,0,0,1,0,1,0,0 },    // 31
                { 0,1,0,1,0,1,0,1,0,0 },    // 41
                { 0,0,0,1,0,1,0,0,1,0 },    // 51
                { 1,0,0,0,0,0,0,1,1,0 },    // 61
                { 0,0,0,1,0,0,0,0,0,0 },    // 71
                { 0,0,0,1,1,0,0,1,1,0 },    // 81 end 7, 6
                { 0,0,0,0,0,1,1,0,0,0 },    // 101
                { 0,0,0,0,1,0,0,0,0,0 },    // 111
                { 0,0,0,0,0,0,0,0,0,0 },    // 121
                { 0,0,0,0,0,0,0,0,0,0 }     // 131
            };

            for (int r = 0; r < 12; r++)
                for (int c = 0; c < 10; c++)
                    Grid[r, c] = temp[r, c];
        }
        
        public static int[,] GetGrid() => Grid;

        public static IEnumerable<Node> GetNeighbors(Node current)
        {
            int rows = Grid.GetLength(0);
            int cols = Grid.GetLength(1);
            var neighbors = new List<Node>();

            int[] dRow = { -1, 1, 0, 0 };
            int[] dCol = { 0, 0, 1, -1 };

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