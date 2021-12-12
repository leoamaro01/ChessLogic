namespace PixelDashCore.ChessLogic.Tests;
class Program
{
    const string HEADER = "    A   B   C   D   E   F   G   H\n" +
                          "  ┏━━━┳━━━┳━━━┳━━━┳━━━┳━━━┳━━━┳━━━┓";
    const string FOOTER = "  ┗━━━┻━━━┻━━━┻━━━┻━━━┻━━━┻━━━┻━━━┛\n" +
                          "    A   B   C   D   E   F   G   H";
    const string SEPARATOR = "  ┣━━━╋━━━╋━━━╋━━━╋━━━╋━━━╋━━━╋━━━┫";

    static void Main(string[] args)
    {
    NEWGAME:
        Board b = new Board();
        bool whiteTurn = true;

        while (true)
        {
            if (b.IsInCheckMate(whiteTurn))
            {
                System.Console.WriteLine("Checkmate! " +
                    (!whiteTurn ? "White" : "Black") + " wins!");
                System.Console.WriteLine("Press P to play again!");
                if (Console.ReadKey(true).Key == ConsoleKey.P)
                    goto NEWGAME;
                else break;
            }
            else if (b.IsInCheck(whiteTurn))
            {
                System.Console.WriteLine("Check!");
            }
            else if (b.IsInStalemate(whiteTurn))
            {
                System.Console.WriteLine("Stalemate! IT'S A DRAW!");
                System.Console.WriteLine("Press P to play again!");
                if (Console.ReadKey(true).Key == ConsoleKey.P)
                    goto NEWGAME;
                else break;
            }

            RenderBoard(b);

        SELECT_PIECE:
            Console.WriteLine();
            System.Console.Write("{0} Turn. Enter piece to move (ex. e4) or write \"UNDO\" to undo:\n> ",
                whiteTurn ? "White" : "Black");

            string piece = (Console.ReadLine() ?? "").ToLower();
            if (piece == "undo")
            {
                b.UndoMove();
                whiteTurn = !whiteTurn;
                continue;
            }

            (int x, int y) coords = Board.AlgebraicToCoords(piece);
            if (b.GetPieceAt(coords).isWhite != whiteTurn)
            {
                System.Console.WriteLine("You can't move that piece!");
                goto SELECT_PIECE;
            }
            else if (b.GetPieceAt(coords).pieceType == PieceType.None)
            {
                System.Console.WriteLine("There is no piece in that spot.");
                goto SELECT_PIECE;
            }

            var possibleMoves = b.GetPossibleMoves(coords);

            if (possibleMoves.Length == 0)
            {
                System.Console.WriteLine("That piece can't move!");
                goto SELECT_PIECE;
            }

            System.Console.WriteLine();

            System.Console.WriteLine("Select move to make:");

            for (int i = 0; i < possibleMoves.Length; i++)
            {
                System.Console.WriteLine($"{i} - {possibleMoves[i].algebraic}");
            }

            System.Console.WriteLine($"{possibleMoves.Length} - Select another piece.");
            System.Console.Write("> ");

            int move = int.Parse(Console.ReadLine() ?? $"{possibleMoves.Length}");

            if (move >= possibleMoves.Length || move < 0)
                goto SELECT_PIECE;

            b.MakeMove(possibleMoves[move]);

            whiteTurn = !whiteTurn;
        }
    }

    const ConsoleColor whiteColor = ConsoleColor.White,
                        blackColor = ConsoleColor.DarkGray;
    public static void RenderBoard(Board board)
    {
        Console.Clear();
        Console.WriteLine();

        ConsoleColor col = Console.ForegroundColor;
        Console.WriteLine(HEADER);
        for (int y = 7; y >= 0; y--)
        {
            if (y != 7)
            {
                Console.WriteLine(SEPARATOR);
            }
            Console.Write($"{y + 1} ┃");
            for (int x = 0; x < 8; x++)
            {
                Piece piece = board.GetPieceAt((x, y));

                Console.ForegroundColor = piece.isWhite ? whiteColor : blackColor;
                Console.Write(" {0} ",
                Board.PieceTypeToAlgebraicChar(piece.pieceType).ToString().ToUpper());

                Console.ForegroundColor = col;
                Console.Write("┃");
            }
            Console.Write($" {y + 1}");
            Console.WriteLine();
        }
        Console.WriteLine(FOOTER);
        System.Console.WriteLine();
    }
}