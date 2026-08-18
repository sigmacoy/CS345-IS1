// VacuumAgent.cs
using System;
using System.Text;

namespace Aug17;

public abstract class Environment
{
    public abstract void ExecuteAction(Agent agent, string action);
    public abstract Tuple<int, int, bool> Percept(Agent agent);
}

public abstract class Agent
{
    public int Performance { get; set; } = 0;
    public abstract object Program(Tuple<int, int, bool> percept);
}

public class VacuumEnvironment : Environment
{
    private int[,] grid = new int[2, 2];
    private int agentX = 0;
    private int agentY = 0;
    private Random rand = new Random();

    public int AgentX => agentX;
    public int AgentY => agentY;
    public int GetDirt(int row, int col) => grid[row, col];

    public VacuumEnvironment()
    {
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
                grid[i, j] = rand.Next(0, 2);
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Grid state:");
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++) sb.Append(grid[i, j] + " ");
            sb.AppendLine();
        }
        sb.AppendLine($"Agent position: ({agentX}, {agentY})");
        return sb.ToString();
    }

    public override Tuple<int, int, bool> Percept(Agent agent)
    {
        bool isDirty = grid[agentX, agentY] == 1;
        return Tuple.Create(agentX, agentY, isDirty);
    }

    public override void ExecuteAction(Agent agent, string action)
    {
        if (action == null) { agent.Performance -= 1; return; }

        if (action == "Suck")
        {
            if (grid[agentX, agentY] == 1)
            {
                grid[agentX, agentY] = 0;
                agent.Performance += 10;
            }
        }
        else if (action == "Up" && agentX > 0) { agentX -= 1; agent.Performance -= 1; }
        else if (action == "Down" && agentX < 1) { agentX += 1; agent.Performance -= 1; }
        else if (action == "Left" && agentY > 0) { agentY -= 1; agent.Performance -= 1; }
        else if (action == "Right" && agentY < 1) { agentY += 1; agent.Performance -= 1; }
        else { agent.Performance -= 1; }
    }
}

public class SimpleReflexAgent : Agent
{
    private readonly Random rand = new Random();

    public override object Program(Tuple<int, int, bool> percept)
    {
        if (percept == null) return null;
        if (percept.Item3) return "Suck";

        string[] choices = { "Up", "Down", "Left", "Right" };
        return choices[rand.Next(choices.Length)];
    }
}