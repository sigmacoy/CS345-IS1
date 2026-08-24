// Form1.axaml.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.Threading;
using System.Linq;
using static Aug24.MazeEnvironment;

namespace Aug24;

public partial class Form1 : Window
{
    private Dictionary<Node, Node> parentMap;
    private Queue<Node> frontier;
    private HashSet<Node> visited;
    private List<Node> origin;
    private Node current;

    public Form1()
    {
        InitializeComponent();
        DrawMaze(); // Initial draw
    }

    private CancellationTokenSource cts;

    public async Task runBFS(Node start, Node goal)
    {
        parentMap = new Dictionary<Node, Node>();
        richTextBox1.Text = "running BFS";
        label1.Text = "Queue";
        frontier = new Queue<Node>();
        visited = new HashSet<Node>();
        origin = new List<Node>();
        
        frontier.Enqueue(start);
        visited.Add(start);

        while (frontier.Count > 0)
        {
            if (cts.Token.IsCancellationRequested) return; // Check for stop

            current = frontier.Dequeue();
            richTextBox1.Text += $"\ncurrent node: {current}";
            DrawMaze(); 

            if (current.Row == goal.Row && current.Col == goal.Col)
            {
                richTextBox1.Text += "\ngoal reached!";
        
                // Backtrack from goal to start
                HashSet<Node> finalPath = new HashSet<Node>();
                Node trace = current;
                while (parentMap.ContainsKey(trace))
                {
                    finalPath.Add(trace);
                    trace = parentMap[trace];
                }
                finalPath.Add(start);
        
                DrawMaze(finalPath); // Pass the path to draw
                return;
            }

            foreach(Node next in MazeSolver.GetNeighbors(current))
            {
                if (!visited.Contains(next))
                {
                    visited.Add(next);
                    frontier.Enqueue(next);
                    origin.Add(current);
                    parentMap[next] = current;
                }
            }

            richTextBox2.Text = string.Join("\n", frontier);
            richTextBox3.Text = string.Join("\n", visited);
            richTextBox4.Text = string.Join("\n", origin);
            
            try { await Task.Delay(100, cts.Token); } 
            catch (TaskCanceledException) { return; } // Safely exit if stopped
        }

        richTextBox1.Text += "\nWala nakita ang goal";
    }
    
    public async Task runDFS(Node start, Node goal)
    {    
        parentMap = new Dictionary<Node, Node>();
        richTextBox1.Text = "running DFS";
        label1.Text = "Stack";
        Stack<Node> frontierStack = new Stack<Node>(); 
        visited = new HashSet<Node>();
        origin = new List<Node>();
        
        frontierStack.Push(start);
        visited.Add(start);
        
        while (frontierStack.Count > 0)
        {
            if (cts.Token.IsCancellationRequested) return; // Check for stop

            current = frontierStack.Pop();
            richTextBox1.Text += $"\ncurrent node: {current}";
            DrawMaze();

            if (current.Row == goal.Row && current.Col == goal.Col)
            {
                richTextBox1.Text += "\ngoal reached!";
        
                // Backtrack from goal to start
                HashSet<Node> finalPath = new HashSet<Node>();
                Node trace = current;
                while (parentMap.ContainsKey(trace))
                {
                    finalPath.Add(trace);
                    trace = parentMap[trace];
                }
                finalPath.Add(start);
        
                DrawMaze(finalPath); // Pass the path to draw
                return;
            }

            // REVERSE so it pushes Right, Left, Down, Up -> pops Up, Down, Left, Right
            var neighbors = MazeSolver.GetNeighbors(current).ToList();
            neighbors.Reverse();

            foreach(Node next in neighbors)
            {
                if (!visited.Contains(next))
                {
                    visited.Add(next);
                    frontierStack.Push(next);
                    origin.Add(current);
                    parentMap[next] = current;
                }
            }

            richTextBox2.Text = string.Join("\n", frontierStack);
            richTextBox3.Text = string.Join("\n", visited);
            richTextBox4.Text = string.Join("\n", origin);
            
            try { await Task.Delay(100, cts.Token); } 
            catch (TaskCanceledException) { return; } // Safely exit if stopped
        }

        richTextBox1.Text += "\nWala nakita ang goal";
    }
    

    private async void button1_Click(object sender, RoutedEventArgs e)
    {
        cts?.Cancel(); 
        cts = new CancellationTokenSource();
        await runBFS(ParseNode(txtStart.Text), ParseNode(txtGoal.Text));
    }

    private async void button2_Click(object sender, RoutedEventArgs e)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        await runDFS(ParseNode(txtStart.Text), ParseNode(txtGoal.Text));
    }
    
    private async void button3_Click(object sender, RoutedEventArgs e)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        MazeSolver.Randomize();
        // Reset current position so red box disappears before search
        current = new Node(0, 0); 
        DrawMaze();             
    }

    private async void buttonStop_Click(object sender, RoutedEventArgs e)
    {
        cts?.Cancel();
    }

    private void DrawMaze(HashSet<Node> finalPath = null)
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
                Node cellNode = new Node(r, c);
                IBrush brush = grid[r, c] == 1 ? Brushes.Black : Brushes.White;
                
                // Color the final path Red
                if (finalPath != null && finalPath.Contains(cellNode))
                {
                    brush = Brushes.Red;
                }

                var rect = new Rectangle
                {
                    Width = cellW,
                    Height = cellH,
                    Fill = brush,
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
    
    private Node ParseNode(string input)
    {
        var parts = input.Split(',');
        return new Node(int.Parse(parts[0]), int.Parse(parts[1]));
    }
}