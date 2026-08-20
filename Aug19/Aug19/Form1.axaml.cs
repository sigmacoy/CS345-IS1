// MainWindow.axaml.cs

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Media;

namespace Aug19;
// SEARCHDemo

public partial class Form1 : Window
{
    
    // BFS
    private Queue<Node> frontier;
    private HashSet<Node> visited;
    private List<Node> origin;
    Node current;
    
    public Form1()
    {
        InitializeComponent();
    }

    public void runBFS(Node start, Node goal)
    {
        richTextBox1.Text = "running BFS";
        frontier = new Queue<Node>();
        visited = new HashSet<Node>();
        origin = new List<Node>();
        frontier.Enqueue(start);
        visited.Add(goal);
        while (frontier.Count > 0)
        {
            current = frontier.Dequeue();
            richTextBox1.Text += "\ncurrent node: " + current.ToString();
            // check if goal is reached
            if (current.Row == goal.Row && current.Col == goal.Col)
            {
                richTextBox1.text += "\ngoal reached!";
                this.Refresh();
                return;
            }
            foreach(Node in MazeEnvironment.MazeSolver.GetNeighbors(current))
            {
                if (visited.Contains(next))
                {
                    visited.Add(next);
                    frontier.Enqueue(next);
                    origin.Add(current);
                }
            }
            richTextBox2.Text = string.Join("\n", frontier.ToArray());
            richTextBox3.Text = string.Join("\n", visited.ToArray());
            richTextBox4.Text = string.Join("\n", origin.ToArray());
            this.Refresh();
            Thread.Sleep(2000);
        }

        richTextBox1.Text += "wala nakita ang goal";

        private void button1_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            runBFS(new Node(0, 0), new Node(4, 4));
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            int[,] grid = MazeEnvironment.MazeSolver.GetGrid();
            // 320, 320 for pictureBox
            int rows = grid.GetLength(0);
            int cols =  grid.GetLength(1);
            int cellW = Math.Max(1, pictureBox1.ClientSize.Width / cols);
            int cellH = Math.Max(1, pictureBox1.ClientSize.Height / cols);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Rectangle rect = new Rectangle(c * cellW, r * cellH, cellW, cellH);
                    if (grid[r, c] == 1)
                    {
                        e.Graphics.FillRectangle(Brushes.Black, rect);
                    }
                    else
                    {
                        e.Graphics.FillRectangle(Brushes.White, rect);
                    }

                    e.Graphics.DrawRectangle(Pen.Gray, rect);
                }

                Rectangle rect1 = new Rectangle(current.Col * cellW, current.Row * cellH, cellW, cellH);
                e.Graphics.FillRectangle(Brushes.Red, rect1);
                e.Graphics.DrawString("" + current.Row + "," + current.Col, new Font("Arial", 8), Brushes.Black, rect1);
            }
        }
    }
}