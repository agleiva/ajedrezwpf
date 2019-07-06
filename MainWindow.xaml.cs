using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace AjedrezWpf
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = ChessBoard.Instance;
        }
    }

    public class MainMenu : ViewModelBase
    {
        private bool isOpen = true;
        public bool IsOpen
        {
            get => isOpen;
            set => Set(ref isOpen, value);
        }

        public MainMenu()
        {
            NewGameCommand = new RelayCommand(() => NewGame());
            SaveCommand = new RelayCommand(() => Save(), () => ChessBoard.Instance.Squares.Any());
        }

        public RelayCommand NewGameCommand { get; }
        public RelayCommand SaveCommand { get; }

        private async Task NewGame()
        {
            IsOpen = false;
            await Task.Delay(250);
            await ChessBoard.Instance.Create();
        }

        private async Task Save()
        {

        }
    }

    public class ChessBoard : ViewModelBase
    {
        public static ChessBoard Instance { get; } = new ChessBoard();

        public MainMenu MainMenu { get; } = new MainMenu();
        public ObservableCollection<Square> Squares { get; } = new ObservableCollection<Square>();
        public ObservableCollection<object> BlackPieces { get; } = new ObservableCollection<object>();
        public ObservableCollection<object> WhitePieces { get; } = new ObservableCollection<object>();
        public ObservableCollection<Movimiento> Movimientos { get; } = new ObservableCollection<Movimiento>();

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

        public bool WhitePlays => !BlackPlays && !MainMenu.IsOpen;

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

        public async Task Create()
        {
            Movimientos.Clear();
            SelectedSquare = null;
            if (!Squares.Any())
                foreach (var r in Enumerable.Range(1, 8))
                {
                    await Task.Delay(50);
                    var isBlack = r % 2 == 1;
                    foreach (var c in Enumerable.Range(1, 8))
                        Squares.Add(new Square { Row = r, Col = c, IsBlack = (isBlack = !isBlack) });
                }
            else
                Squares.ToList().ForEach(x => x.Piece = null);

            Square Square(int r, int c) => Squares.First(x => x.Row == r && x.Col == c);

            var pieces =
                File.ReadAllLines("Default.brd")
                    .Select(x => x.Split(','))
                    .Select(x => (int.Parse(x[0]), int.Parse(x[1]), Enum.Parse<PieceTypes>(x[2]), bool.Parse(x[3])));

            var rnd = new Random();

            foreach (var p in pieces.OrderBy(x => rnd.Next()))
            {
                await Task.Delay(50);
                Square(p.Item1, p.Item2).Piece = new Piece { Type = p.Item3, IsBlack = p.Item4 };
            }

            await RefreshPieceCount();

            BlackPlays = Movimientos.Count % 2 != 0;
        }

        private async Task RefreshPieceCount()
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
            {
                await Task.Delay(50);
                (p.IsBlack ? BlackPieces : WhitePieces).Add(new { Type = p.Item2, Count = p.Item3, ImageSource = new Piece { Type = p.Item2, IsBlack = p.IsBlack }.ImageSource });
            }
                
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
                Movimientos.Add(new Movimiento
                {
                    From = previousSquare,
                    To = newSquare,
                    Piece = previousSquare.Piece,
                });

                var captures = newSquare.Piece != null;

                newSquare.Piece = previousSquare.Piece;
                previousSquare.Piece = null;

                ClearMoveTargets();
                BlackPlays = !BlackPlays;
                if (captures)
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

            bool Eats(Square sq) => sq?.Piece != null && sq.Piece.IsBlack != square.Piece.IsBlack;

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
                        else if (Eats(current))
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

            IEnumerable<(int, int)> Cartesian(int[] a, int[] b) => from r in a from c in b select (r, c);

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

                        foreach (var m in new[] { 1, -1 }.Select(x => Move(v, x)).Where(Eats))
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
                        var paths = Cartesian(new[] { -1,0, 1 }, new[] { -1,0, 1 });

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

    public class Square : ObservableObject
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public bool IsBlack { get; set; }

        private bool isValidMoveTarget;
        public bool IsValidMoveTarget
        {
            get => isValidMoveTarget;
            set => Set(ref isValidMoveTarget, value);
        }

        private Piece piece;
        public Piece Piece
        {
            get => piece;
            set => Set(ref piece, value);
        }

        public string Name => $"{"abcdefgh"[Col-1]}{9 - Row}";
    }

    public class Piece : ObservableObject
    {
        public bool IsBlack { get; set; }
        public PieceTypes Type { get; set; }

        public string ImageSource => "Img/" + (IsBlack ? "Negro-" : "Blanco-") + Type.ToString() + ".png";
    }

    public enum PieceTypes { Peón, Torre, Caballo, Alfil, Reina, Rey }

    public class Movimiento
    {
        public Piece Piece { get; set; }
        public Square From { get; set; }
        public Square To { get; set; }
    }
}

