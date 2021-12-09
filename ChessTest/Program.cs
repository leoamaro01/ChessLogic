namespace PixelDashCore.ChessLogic.Tests;
class Program
{
    const string HEADER = "┏━━━┳━━━┳━━━┳━━━┳━━━┳━━━┳━━━┳━━━┓";
    const string FOOTER = "┗━━━┻━━━┻━━━┻━━━┻━━━┻━━━┻━━━┻━━━┛";
    const string SEPARATOR = "┣━━━╋━━━╋━━━╋━━━╋━━━╋━━━╋━━━╋━━━┫";

    static void Main(string[] args)
    {
    NEWGAME:
        Board b = new Board();

        while (true)
        {
            if (b.IsInCheckMate(true))
            {
                //Black win
            }
            else if (b.IsInCheckMate(false))
            {
                //White win
            }
            else if (b.IsInStalemate(true))
            {
                //White stalemate
            }
            else if (b.IsInStalemate(false))
            {
                //Black stalemate
            }

            RenderUI(b);

            Console.ReadLine();
        }
    }

    const ConsoleColor whiteColor = ConsoleColor.White,
                        blackColor = ConsoleColor.DarkYellow;
    public static void RenderUI(Board board)
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
            Console.Write("┃");
            for (int x = 0; x < 8; x++)
            {
                Piece piece = board.GetPieceAt((x, y));

                Console.ForegroundColor = piece.isWhite ? whiteColor : blackColor;
                Console.Write(" {0} ",
                Board.PieceTypeToAlgebraicChar(piece.pieceType).ToString().ToUpper());

                Console.ForegroundColor = col;
                Console.Write("┃");
            }
            Console.WriteLine();
        }
        Console.WriteLine(FOOTER);
    }
}