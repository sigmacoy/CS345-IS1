// MainWindow.axaml.cs
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Aug17;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void button1_Click(object? sender, RoutedEventArgs e)
    {
        var env = new VacuumEnvironment();
        var agent = new SimpleReflexAgent();

        OutputLog.Text = "Creating 2x2 world...\n\n";
        OutputLog.Text += env.ToString() + "\n";
        
        UpdateVisuals(env);
        await Task.Delay(1000); 

        for (int step = 0; step < 10; step++)
        {
            var percept = env.Percept(agent);
            var action = agent.Program(percept) as string;
            
            env.ExecuteAction(agent, action);

            string locationText = $"({percept.Item1}, {percept.Item2})";
            OutputLog.Text += $"Step {step + 1}: Action = {action,-5} | Location = {locationText} | Score = {agent.Performance}\n";
            OutputLog.CaretIndex = OutputLog.Text.Length;

            UpdateVisuals(env);
            await Task.Delay(1000); 
        }
    }

    private void UpdateVisuals(VacuumEnvironment env)
    {
        SimulationCanvas.Children.Clear();

        double cellW = SimulationCanvas.Bounds.Width / 2;
        double cellH = SimulationCanvas.Bounds.Height / 2;

        if (cellW <= 0 || cellH <= 0) return; 

        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 2; col++)
            {
                if (env.GetDirt(row, col) == 1)
                {
                    var dirt = new Avalonia.Controls.Shapes.Rectangle
                    {
                        Fill = Avalonia.Media.Brushes.SaddleBrown,
                        Width = cellW * 0.8,
                        Height = cellH * 0.8
                    };
                    Canvas.SetLeft(dirt, (col * cellW) + (cellW * 0.1));
                    Canvas.SetTop(dirt, (row * cellH) + (cellH * 0.1));
                    SimulationCanvas.Children.Add(dirt);
                }
            }
        }

        var agentShape = new Avalonia.Controls.Shapes.Ellipse
        {
            Fill = Avalonia.Media.Brushes.Red,
            Width = cellW * 0.5,
            Height = cellH * 0.5
        };
        Canvas.SetLeft(agentShape, (env.AgentY * cellW) + (cellW * 0.25));
        Canvas.SetTop(agentShape, (env.AgentX * cellH) + (cellH * 0.25));
        
        SimulationCanvas.Children.Add(agentShape);
    }
}