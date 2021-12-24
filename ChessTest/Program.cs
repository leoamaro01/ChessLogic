using EasyTcp4;
using EasyTcp4.ServerUtils;
using EasyTcp4.ClientUtils;
using System.Threading;
using System.Net;
namespace PixelDashCore.ChessLogic.Tests;
class Program
{
    const int UPDATES_PER_SECOND = 5;
    const int MS_PER_UPDATE = 1000 / UPDATES_PER_SECOND;
    const int PORT = 48999;
    const string HEADER = "    A   B   C   D   E   F   G   H\n" +
                          "  ┏━━━┳━━━┳━━━┳━━━┳━━━┳━━━┳━━━┳━━━┓";
    const string FOOTER = "  ┗━━━┻━━━┻━━━┻━━━┻━━━┻━━━┻━━━┻━━━┛\n" +
                          "    A   B   C   D   E   F   G   H";
    const string SEPARATOR = "  ┣━━━╋━━━╋━━━╋━━━╋━━━╋━━━╋━━━╋━━━┫";

    static void Main()
    {
        HostMultiplayer();
    }
    static void JoinMultiplayer()
    {
        string ip = "127.0.0.1"; // got through arcane magic.
        using var client = new EasyTcpClient();
        bool playing = false;

        Board b = new();
        bool whiteTurn = true;
        bool whitePlayer = false;

        client.OnDataReceive += (sender, message) =>
        {
            if (message.Data[0] == ServerPacketCodes.WELCOME)
            {
                whitePlayer = message.Data[1] == 0;
                client.Send(new byte[1] { ClientPacketCodes.WELCOME_RECEIVED });

                playing = true;
            }
            else if (message.Data[0] == GeneralPacketCodes.MAKE_MOVE)
            {
                whiteTurn = !whiteTurn;
                string move = Board.CoordsToAlgebraic((message.Data[1], message.Data[2])) +
                 Board.CoordsToAlgebraic((message.Data[3], message.Data[4])) +
                 (message.Data.Length == 6 ?
                    Board.PieceTypeToAlgebraicChar((PieceType)message.Data[5])
                     : "");

                b.MakeMove(move);
            }
        };

        if (!client.Connect(ip, PORT))
            Console.WriteLine("Failed to connect to " + ip);

        while (true)
        {
            if (!playing)
            {
                Thread.Sleep(MS_PER_UPDATE);
                continue;
            }

            RenderBoard(b);

            if (b.IsInCheckMate(whiteTurn))
            {
                System.Console.WriteLine("Checkmate! " +
                    (!whiteTurn ? "White" : "Black") + " wins!");
                System.Console.WriteLine("Press P to play again!");
                if (Console.ReadKey(true).Key == ConsoleKey.P)
                    return; // TODO: Replaying.
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
                    return; // TODO: Replaying.
                else break;
            }

            if (whiteTurn == whitePlayer)
            {
                MultiplayerTurn(client, b, whitePlayer);
            }
            else
            {
                Thread.Sleep(MS_PER_UPDATE);
                continue;
            }
        }
    }
    static void HostMultiplayer()
    {
        using var server = new EasyTcpServer().Start(PORT);
        EasyTcpClient? connectedClient = null;
        bool listening = true;

        Board b = new();
        bool whitePlayer = new Random().Next(0, 2) == 0;
        bool playing = false;
        bool whiteTurn = true;

        server.OnConnect += (sender, client) =>
        {
            if (listening)
            {
                listening = false;
                connectedClient = client;
                client.Send(new byte[2] { ServerPacketCodes.WELCOME, (byte)(!whitePlayer ? 0 : 1) });
            }
            else
                client.Dispose();
        };
        server.OnDataReceive += (sender, message) =>
        {
            if (message.Data[0] == ClientPacketCodes.WELCOME_RECEIVED)
            {
                playing = true;
            }
            else if (message.Data[0] == GeneralPacketCodes.MAKE_MOVE)
            {
                whiteTurn = !whiteTurn;
                string move = Board.CoordsToAlgebraic((message.Data[1], message.Data[2])) +
                 Board.CoordsToAlgebraic((message.Data[3], message.Data[4])) +
                 (message.Data.Length == 6 ?
                    Board.PieceTypeToAlgebraicChar((PieceType)message.Data[5])
                     : "");

                b.MakeMove(move);
            }
        };

        while (true)
        {
            if (!playing || connectedClient == null)
            {
                Thread.Sleep(MS_PER_UPDATE);
                continue;
            }

            RenderBoard(b);

            if (b.IsInCheckMate(whiteTurn))
            {
                System.Console.WriteLine("Checkmate! " +
                    (!whiteTurn ? "White" : "Black") + " wins!");
                System.Console.WriteLine("Press P to play again!");
                if (Console.ReadKey(true).Key == ConsoleKey.P)
                    return; // TODO: Replaying.
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
                    return; // TODO: Replaying.
                else break;
            }

            if (whiteTurn == whitePlayer)
            {
                MultiplayerTurn(connectedClient, b, whitePlayer);
            }
            else
            {
                Thread.Sleep(MS_PER_UPDATE);
                continue;
            }
        }
    }
    static void MultiplayerTurn(EasyTcpClient client, Board b, bool whitePlayer)
    {
    SELECT_PIECE:
        Console.WriteLine();
        System.Console.Write("{0} Turn. Enter piece to move (ex. e4):\n> ",
            whitePlayer ? "White" : "Black");

        string piece = (Console.ReadLine() ?? "").ToLower();

        (int x, int y) coords = Board.AlgebraicToCoords(piece);
        if (b.GetPieceAt(coords).isWhite != whitePlayer)
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
        (int moveFromX, int moveFromY) = possibleMoves[move].from;
        (int moveToX, int moveToY) = possibleMoves[move].to;

        byte[] data;
        if (possibleMoves[move].promotion == PieceType.None)
        {
            data = new byte[]
           {
                GeneralPacketCodes.MAKE_MOVE,
                (byte)moveFromX, (byte)moveFromY,
                (byte)moveToX, (byte)moveToY
           };
        }
        else
        {
            data = new byte[]
           {
                GeneralPacketCodes.MAKE_MOVE,
                (byte)moveFromX, (byte)moveFromY,
                (byte)moveToX,(byte)moveToY,
                (byte)possibleMoves[move].promotion
           };
        }

        client.Send(data);
    }
    static void SinglePlayer()
    {
    NEWGAME:
        Board b = new();
        bool whiteTurn = true;

        while (true)
        {
            RenderBoard(b);

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