namespace Aug19;

// Seq: Up Down Left Right

public class MazeEnvironment
{
    
    
    
    // the problem definition for the maze environment
    public static class MazeSolver
    {
        // 0 = Path, 1 = Wall
        private static readonly int[,] Grid =
        {
            {0, 0, 1, 0, 0},
            {0, 0, 1, 0, 0},
            {0, 1, 1, 0, 1},
            {0, 0, 0, 0, 0},
            {1, 1, 0, 1, 0}
        };
        private static readonly int Rows = Grid.GetLength(0);
        private static readonly int Cols = Grid.GetLength(1);
        
        
        
        
    }
    
    
}