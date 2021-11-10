using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GalaSoft.MvvmLight;
using MvvmGen;
using Ookii.Dialogs.Wpf;

namespace AjedrezWpf;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = MainMenu.Instance;
    }
}

[ViewModel]
public partial class MainMenu
{
    public static readonly MainMenu Instance = new();

    [Property] private bool isOpen = true;
    [Property] private ChessBoard chessBoard = new();

    [Command] private async Task NewGame()  => await LoadBoard("NewGame.json");
    [Command] private async Task LoadGame() => await LoadBoard();

    private async Task LoadBoard(string fileName = null)
    {
        if (fileName == null)
        {
            var dialog = new VistaOpenFileDialog { Filter = "json chess board|*.json" };
            if (dialog.ShowDialog() != true)
                return;
            fileName = dialog.FileName;
        }

        var boardData = await File.ReadAllTextAsync(fileName);

        IsOpen = false;

        ChessBoard = JsonSerializer.Deserialize<ChessBoard>(boardData);
        await ChessBoard.RefreshPieceCount();
    }

    [Command(nameof(CanSave))]
    private async Task SaveGame()
    {
        var dialog = new VistaSaveFileDialog { DefaultExt = ".json", Filter = "json chess board|*.json" };
        if (dialog.ShowDialog() != true)
            return;

        var boardData = JsonSerializer.Serialize(ChessBoard);
        await File.WriteAllTextAsync(dialog.FileName, boardData);
    }

    [CommandInvalidate(nameof(ChessBoard))] 
    private bool CanSave() => ChessBoard.Squares.Any();

    [Command] private void Exit() => System.Windows.Application.Current.Shutdown();
}

public class ChessBoard : ViewModelBase
{
    public ObservableCollection<Square> Squares { get; set;  } = new();
    public ObservableCollection<Movimiento> Movimientos { get; set; } = new();
    public ObservableCollection<PieceCount> BlackPieces { get; set; } = new();
    public ObservableCollection<PieceCount> WhitePieces { get; set; } = new();

    private bool blackPlays;
    public bool BlackPlays
    {
        get => blackPlays;
        set
        {
            Set(ref blackPlays, value);
            RaisePropertyChanged(() => WhitePlays);
        }
    }

    public bool WhitePlays => !BlackPlays && !MainMenu.Instance.IsOpen;

    private Square selectedSquare;
    public Square SelectedSquare
    {
        get => selectedSquare;
        set
        {
            OnSelectedSquareChanged(selectedSquare, value);
            Set(ref selectedSquare, value);
        }
    }

    public async Task RefreshPieceCount()
    {
        WhitePieces.Clear();
        BlackPieces.Clear();
        var pieces =
            Squares.Select(x => x.Piece)
                   .Where(x => x != null)
                   .GroupBy(x => new { x.IsBlack, x.Type })
                   .Select(p => (p.Key.IsBlack, p.Key.Type, p.Count()))
                   .OrderBy(x => x.Item2);

        foreach (var p in pieces)
            (p.IsBlack ? BlackPieces : WhitePieces).Add(new PieceCount(Type: p.Type, Count: p.Item3, ImageSource: new Piece(p.IsBlack, p.Item2).ImageSource ));
    }

    public async Task OnSelectedSquareChanged(Square previousSquare, Square newSquare)
    {
        void ClearMoveTargets()
        {
            foreach (var s in Squares)
                s.IsValidMoveTarget = false;
        }

        if (newSquare?.IsValidMoveTarget ?? false)
        {
            Movimientos.Add(new Movimiento(previousSquare.Piece, previousSquare, newSquare));

            newSquare.Piece = previousSquare.Piece;
            previousSquare.Piece = null;

            ClearMoveTargets();
            BlackPlays = !BlackPlays;
            
            await RefreshPieceCount();
        }
        else
        {
            ClearMoveTargets();

            if (newSquare?.Piece?.IsBlack == BlackPlays)
                foreach (var v in GetValidMoveTargets(newSquare))
                    v.IsValidMoveTarget = true;
        }
    }

    private IEnumerable<Square> GetValidMoveTargets(Square square)
    {
        Square MoveFrom(Square s, int v, int h) =>
            Squares.FirstOrDefault(x => x.Row == s.Row + v && x.Col == s.Col + h);

        Square Move(int v, int h) => MoveFrom(square, v, h);

        bool Captures(Square sq) => sq?.Piece != null && sq.Piece.IsBlack != square.Piece.IsBlack;

        IEnumerable<Square> Traverse(IEnumerable<(int r, int c)> paths)
        {
            foreach (var p in paths)
            {
                var current = square;
                while (true)
                {
                    current = MoveFrom(current, p.r, p.c);
                    if (current == null)
                        break;
                    else if (Captures(current))
                    {
                        yield return current;
                        break;
                    }
                    else if (current != null && current.Piece == null)
                        yield return current;
                    else if (current?.Piece?.IsBlack == square.Piece.IsBlack)
                        break;
                    if (square.Piece.Type == PieceTypes.Rey)
                        break;
                }
            }
        }

        var Cartesian = (int[] a, int[] b) => from r in a from c in b select (r, c);

        switch (square.Piece.Type)
        {
            case PieceTypes.Peón:
                {
                    var v = square.Piece.IsBlack ? 1 : -1;

                    var straight = Move(v, 0);
                    if (straight.Piece == null)
                        yield return straight;

                    var isDefaultPosition =
                        square.Piece.IsBlack && square.Row == 2 ||
                        !square.Piece.IsBlack && square.Row == 7;

                    if (isDefaultPosition)
                    {
                        var straight2 = Move(v * 2, 0);
                        if (straight2.Piece == null)
                            yield return straight2;
                    }

                    foreach (var m in new[] { 1, -1 }.Select(x => Move(v, x)).Where(Captures))
                        yield return m;
                }

                break;
            case PieceTypes.Torre:
                {
                    var paths = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };

                    foreach (var s in Traverse(paths))
                        yield return s;
                }
                break;
            case PieceTypes.Caballo:
                {
                    var moves =
                        new[] { (-1, -2), (-1, 2), (1, -2), (1, 2), (-2, -1), (-2, 1), (2, -1), (2, 1) }
                        .Select(x => Move(x.Item1, x.Item2))
                        .Where(x => x != null && (x.Piece == null || x.Piece.IsBlack != square.Piece.IsBlack));

                    foreach (var m in moves)
                        yield return m;
                }
                break;
            case PieceTypes.Alfil:
                {
                    var paths = Cartesian(new[] { -1, 1 }, new[] { -1, 1 });

                    foreach (var s in Traverse(paths))
                        yield return s;
                }
                break;
            case PieceTypes.Reina:
                {
                    var paths = Cartesian(new[] { -1, 0, 1 }, new[] { -1, 0, 1 });

                    foreach (var s in Traverse(paths))
                        yield return s;
                }
                break;
            case PieceTypes.Rey:
                {
                    var paths = Cartesian(new[] { -1, 0, 1 }, new[] { -1, 0, 1 });

                    foreach (var s in Traverse(paths))
                        yield return s;
                }
                break;
        }

    }
}

[ViewModel]
public partial class Square
{
    public int Row { get; set; }
    public int Col { get; set; }
    public bool IsBlack { get; set; }

    [Property] private bool isValidMoveTarget;
    [Property] private Piece piece;

    [JsonIgnore]
    public string Name => $"{"abcdefgh"[Col - 1]}{9 - Row}";
}

public record Piece(bool IsBlack, PieceTypes Type)
{
    public string ImageSource => "Img/" + (IsBlack ? "Negro-" : "Blanco-") + Type.ToString() + ".png";
}

public enum PieceTypes { Peón, Torre, Caballo, Alfil, Reina, Rey }

public record Movimiento(Piece Piece, Square From, Square To);

public record PieceCount(PieceTypes Type, int Count, string ImageSource);