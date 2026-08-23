// Form1.axaml.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using static Aug19.MazeEnvironment;

namespace Aug19;

public partial class Form1 : Window
{
    private Queue<Node> frontier;
    private HashSet<Node> visited;
    private List<Node> origin;
    private Node current;

    public Form1()
    {
        InitializeComponent();
        DrawMaze(); // Initial draw
    }

    public async Task runBFS(Node start, Node goal)
    {
        richTextBox1.Text = "running BFS";
        frontier = new Queue<Node>();
        visited = new HashSet<Node>();
        origin = new List<Node>();
        
        frontier.Enqueue(start);
        visited.Add(start);

        while (frontier.Count > 0)
        {
            current = frontier.Dequeue();
            richTextBox1.Text += $"\ncurrent node: {current}";
            DrawMaze(); // Update UI

            if (current.Row == goal.Row && current.Col == goal.Col)
            {
                richTextBox1.Text += "\ngoal reached!";
                return;
            }

            foreach(Node next in MazeSolver.GetNeighbors(current))
            {
                if (!visited.Contains(next))
                {
                    visited.Add(next);
                    frontier.Enqueue(next);
                    origin.Add(current);
                }
            }

            richTextBox2.Text = string.Join("\n", frontier);
            richTextBox3.Text = string.Join("\n", visited);
            
            await Task.Delay(1000); // Non-blocking wait to visualize steps
        }

        richTextBox1.Text += "\nwala nakita ang goal";
    }

    private async void button1_Click(object sender, RoutedEventArgs e)
    {
        await runBFS(new Node(0, 0), new Node(4, 4));
    }

    private void DrawMaze()
    {
        pictureBox1.Children.Clear();
        int[,] grid = MazeSolver.GetGrid();
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        double cellW = pictureBox1.Width / cols;
        double cellH = pictureBox1.Height / rows;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var rect = new Rectangle
                {
                    Width = cellW,
                    Height = cellH,
                    Fill = grid[r, c] == 1 ? Brushes.Black : Brushes.White,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(rect, c * cellW);
                Canvas.SetTop(rect, r * cellH);
                pictureBox1.Children.Add(rect);
            }
        }

        // Draw current position
        var currentRect = new Rectangle
        {
            Width = cellW,
            Height = cellH,
            Fill = Brushes.Red
        };
        Canvas.SetLeft(currentRect, current.Col * cellW);
        Canvas.SetTop(currentRect, current.Row * cellH);
        pictureBox1.Children.Add(currentRect);
    }
}
// Test