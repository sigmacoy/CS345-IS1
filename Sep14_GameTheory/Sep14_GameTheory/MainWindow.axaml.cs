using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Sep14_GameTheory;

public partial class MainWindow : Window
{
    private char[] board = new char[9];
    private Button[] buttons;
    private const char HUMAN = 'X';
    private const char AI = 'O';
    private const char EMPTY = ' ';

    public MainWindow()
    {
        InitializeComponent();
        InitializeGame();
    }

    private void InitializeGame()
    {
        buttons = new Button[]
        {
            this.FindControl<Button>("button1"),
            this.FindControl<Button>("button2"),
            this.FindControl<Button>("button3"),
            this.FindControl<Button>("button4"),
            this.FindControl<Button>("button5"),
            this.FindControl<Button>("button6"),
            this.FindControl<Button>("button7"),
            this.FindControl<Button>("button8"),
            this.FindControl<Button>("button9")
        };

        for (int i = 0; i < 9; i++)
        {
            board[i] = EMPTY;
            if (buttons[i] != null)
            {
                buttons[i].Content = "";
                buttons[i].IsEnabled = true;
                buttons[i].Foreground = SolidColorBrush.Parse("#ffffff");
            }
        }
        
        var lblStatus = this.FindControl<TextBlock>("lblStatus");
        if (lblStatus != null)
        {
            lblStatus.Text = "Your turn (X)";
            lblStatus.Foreground = SolidColorBrush.Parse("#00e5ff"); // Cyan for X
        }
    }

    private async void Cell_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button clickButton)
        {
            int index = Array.IndexOf(buttons, clickButton);
            if (index != -1 && board[index] == EMPTY)
            {
                MakeMove(index, HUMAN);

                if (CheckGameEnd()) return;

                // AI MOVE via Minimax with Alpha-Beta Pruning
                var lblStatus = this.FindControl<TextBlock>("lblStatus");
                if (lblStatus != null)
                {
                    lblStatus.Text = "AI IS THINKING...";
                    lblStatus.Foreground = SolidColorBrush.Parse("#ff007f"); // Pink for AI
                }

                // Yield to UI thread to allow 'AI is thinking...' to render
                await Task.Delay(50); 

                // Call the highly optimized Alpha-Beta Pruning Minimax
                int bestMove = FindBestMove();
                if (bestMove != -1)
                {
                    MakeMove(bestMove, AI);
                    if (CheckGameEnd()) return;
                }
            }
        }
    }

    private void MakeMove(int index, char player)
    {
        board[index] = player;
        if (buttons[index] != null)
        {
            buttons[index].Content = player.ToString();
            buttons[index].IsEnabled = false;
            
            // Add dynamic glow coloring for X and O
            if (player == HUMAN)
            {
                buttons[index].Foreground = SolidColorBrush.Parse("#00e5ff"); // Neon Cyan
            }
            else
            {
                buttons[index].Foreground = SolidColorBrush.Parse("#ff007f"); // Neon Pink
            }
        }
    }

    private void DisableGrid()
    {
        foreach (Button btn in buttons)
        {
            if (btn != null)
                btn.IsEnabled = false;
        }
    }

    private bool CheckGameEnd()
    {
        var lblStatus = this.FindControl<TextBlock>("lblStatus");

        if (CheckWin(HUMAN))
        {
            if (lblStatus != null) 
            {
                lblStatus.Text = "You win!";
                lblStatus.Foreground = SolidColorBrush.Parse("#00e5ff");
            }
            DisableGrid();
            return true;
        }
        if (CheckWin(AI))
        {
            if (lblStatus != null) 
            {
                lblStatus.Text = "AI wins!";
                lblStatus.Foreground = SolidColorBrush.Parse("#ff007f");
            }
            DisableGrid();
            return true;
        }
        if (IsBoardFull())
        {
            if (lblStatus != null) 
            {
                lblStatus.Text = "It's a draw!";
                lblStatus.Foreground = SolidColorBrush.Parse("#ffffff");
            }
            return true;
        }
        
        if (lblStatus != null) 
        {
            lblStatus.Text = "Your turn (X)";
            lblStatus.Foreground = SolidColorBrush.Parse("#00e5ff");
        }
        return false;
    }

    private bool IsBoardFull()
    {
        foreach (char cell in board)
        {
            if (cell == EMPTY)
                return false;
        }
        return true;
    }

    private bool CheckWin(char p)
    {
        int[,] winPatterns = new int[,]
        {
            {0,1,2}, {3,4,5}, {6,7,8}, // Rows
            {0,3,6}, {1,4,7}, {2,5,8}, // Columns
            {0,4,8}, {2,4,6}           // Diagonals
        };
        for (int i = 0; i < 8; i++)
        {
            if (board[winPatterns[i, 0]] == p && board[winPatterns[i, 1]] == p && board[winPatterns[i, 2]] == p)
            {
                return true;
            }
        }
        return false;
    }

    // MINIMAX ALGORITHM INTEGRATED WITH ALPHA-BETA PRUNING
    private int FindBestMove()
    {
        int bestVal = int.MinValue;
        int bestMove = -1;
        for (int i = 0; i < 9; i++)
        {
            if (board[i] == EMPTY)
            {
                board[i] = AI;
                // Running the Alpha-Beta Pruning optimized variant
                int moveVal = MinimaxAlphaBeta(board, 0, false, int.MinValue, int.MaxValue);
                board[i] = EMPTY;
                if (moveVal > bestVal)
                {
                    bestMove = i;
                    bestVal = moveVal;
                }
            }
        }
        return bestMove;
    }

    // ALPHA-BETA PRUNING IMPLEMENTATION
    private int MinimaxAlphaBeta(char[] currentBoard, int depth, bool isMax, int alpha, int beta)
    {
        if (CheckWin(AI)) return 10 - depth;
        if (CheckWin(HUMAN)) return depth - 10;
        if (IsBoardFull()) return 0;
        
        if (isMax)
        {
            int best = int.MinValue;
            for (int i = 0; i < 9; i++)
            {
                if (currentBoard[i] == EMPTY)
                {
                    currentBoard[i] = AI;
                    best = Math.Max(best, MinimaxAlphaBeta(currentBoard, depth + 1, false, alpha, beta));
                    currentBoard[i] = EMPTY;

                    alpha = Math.Max(alpha, best);
                    if (beta <= alpha)
                        break; // Prune remaining branches successfully
                }
            }
            return best;
        }
        else
        {
            int best = int.MaxValue;
            for (int i = 0; i < 9; i++)
            {
                if (currentBoard[i] == EMPTY)
                {
                    currentBoard[i] = HUMAN;
                    best = Math.Min(best, MinimaxAlphaBeta(currentBoard, depth + 1, true, alpha, beta));
                    currentBoard[i] = EMPTY;

                    beta = Math.Min(beta, best);
                    if (beta <= alpha)
                        break; // Prune remaining branches successfully
                }
            }
            return best;
        }
    }

    private void BtnReset_Click(object? sender, RoutedEventArgs e)
    {
        InitializeGame();
    }
}