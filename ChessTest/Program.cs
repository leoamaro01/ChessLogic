using System.Net.Sockets;
using System.Net.Mail;
using System.Net;
using EasyTcp4;
using EasyTcp4.ServerUtils;
using EasyTcp4.ClientUtils;
using System.Text.RegularExpressions;

namespace PixelDashCore.ChessLogic.Tests;
class Program
{
    const int UPDATES_PER_SECOND = 5;
    const int MS_PER_UPDATE = 1000 / UPDATES_PER_SECOND;
    const int PORT = 48999;
    const string HEADER = "    A   B   C   D   E   F   G   H\n" +
                          "  ┌───┬───┬───┬───┬───┬───┬───┬───┐";
    const string FOOTER = "  └───┴───┴───┴───┴───┴───┴───┴───┘\n" +
                          "    A   B   C   D   E   F   G   H";
    const string SEPARATOR = "  ├───┼───┼───┼───┼───┼───┼───┼───┤";

    static void Main()
    {
        while (true)
        {
            Console.Clear();
            System.Console.WriteLine("♟️ Welcome to ChessLogic Test! ♟️");

            Console.WriteLine("Main Menu:");
            string[] options = { "Start Offline game. 🖥️", "Host Online game. 🌐➡️🖥️", "Join Online game. 🌐⬅️💻" };
            for (int i = 0; i < options.Length; i++)
                Console.WriteLine($"{i} - {options[i]}");
            System.Console.Write("> ");

            if (!int.TryParse(Console.ReadLine(), out int option))
            {
                System.Console.WriteLine("\nInvalid option, choose a number from the list\n");
                Thread.Sleep(1000);
                continue;
            }

            switch (option)
            {
                case 0:
                    SinglePlayer();
                    break;
                case 1:
                    HostMultiplayer();
                    break;
                case 2:
                    JoinMultiplayer();
                    break;
                default:
                    System.Console.WriteLine("\nInvalid option, choose a number from the list\n");
                    Thread.Sleep(1000);
                    continue;
            }
        }
    }
    static void JoinMultiplayer()
    {
        Console.Clear();
        System.Console.WriteLine("🌐 Multiplayer Joining Menu, enter `exit` in any input to return to the Main Menu");

        using var client = new EasyTcpClient();
        bool playing = false;
        bool inGame = true;

        Board b = new();
        bool whiteTurn = true;
        bool whitePlayer = false;
        bool doNotUpdate = false;

        var disconnectAction = () =>
        {
            Console.WriteLine("Exiting...");
            client.Dispose();
            inGame = false;
        };
        client.OnDataReceive += (sender, message) =>
            {
                if (message.Data[0] == ServerPacketCodes.WELCOME)
                {
                    System.Console.WriteLine("Connected to server!");

                    whitePlayer = message.Data[1] == 0;
                    client.Send(new byte[1] { ClientPacketCodes.WELCOME_RECEIVED });

                    playing = true;
                }
                else if (message.Data[0] == GeneralPacketCodes.MAKE_MOVE)
                {
                    whiteTurn = !whiteTurn;
                    byte[] moveData = message.Data[1..^0];
                    string move = Board.CoordsToAlgebraic((moveData[0], moveData[1])) +
                                    Board.CoordsToAlgebraic((moveData[2], moveData[3])) +
                                    (moveData.Length == 5 ?
                                        Board.PieceTypeToAlgebraicChar((PieceType)moveData[4])
                                        : "");

                    b.MakeMove(move);
                    doNotUpdate = false;
                }
            };
        client.OnDisconnect += (sender, message) =>
        {
            if (!inGame)
                return;

            Console.WriteLine($"Disconnected from server...\nReturning to Main Menu in 3 seconds...");
            Thread.Sleep(3000);
            disconnectAction();
        };

    HOST_IP:
        System.Console.Write("Enter Host IP:\n> ");
        string ip = Console.ReadLine() ?? "";
        while (!Regex.IsMatch(ip, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
        {
            if (ip == "exit")
            {
                System.Console.WriteLine("Exiting...");
                client.Dispose();
                return;
            }

            System.Console.WriteLine("That is not a valid IP, it should be in IPv4 format (ex. 192.168.1.100).");
            Console.Write("Enter a valid IP:\n> ");
            ip = Console.ReadLine() ?? "";
        }
        Console.WriteLine("Connecting...");
        if (!client.Connect(ip, PORT))
        {
            Console.WriteLine("Failed to connect to " + ip);
            goto HOST_IP;
        }

        while (true)
        {
            if (!inGame)
                return;
            if (!playing)
            {
                Thread.Sleep(MS_PER_UPDATE);
                continue;
            }

            bool _update = !doNotUpdate;
            doNotUpdate = false;

            if (_update)
            {
                RenderBoard(b);

                if (b.IsInCheckMate(whiteTurn))
                {
                    System.Console.WriteLine("Checkmate! " +
                        (!whiteTurn ? "White" : "Black") + " wins!");
                    System.Console.WriteLine("Returning to Main Menu...");

                    disconnectAction();
                    continue;
                }
                else if (b.IsInCheck(whiteTurn))
                {
                    System.Console.WriteLine((whiteTurn ? "White" : "Black") + "is in check!");
                }
                else if (b.IsInStalemate(whiteTurn))
                {
                    System.Console.WriteLine((whiteTurn ? "White" : "Black") + "Stalemate! IT'S A DRAW!");

                    System.Console.WriteLine("Returning to Main Menu...");

                    disconnectAction();
                    continue;
                }
            }

            if (whiteTurn == whitePlayer)
            {
                MultiplayerTurn(client, b, whitePlayer, out bool exit);
                if (exit)
                {
                    disconnectAction();
                    continue;
                }
                whiteTurn = !whiteTurn;
            }
            else
            {
                if (_update)
                    Console.WriteLine("Waiting for " + (!whitePlayer ? "white" : "black") + " player to play...");

                doNotUpdate = true;
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
        bool inGame = true;

        IPEndPoint? remoteEndPoint = ((IPEndPoint?)server.BaseSocket.RemoteEndPoint);
        IPEndPoint? localEndPoint = ((IPEndPoint?)server.BaseSocket.LocalEndPoint);

        if (remoteEndPoint != null)
            Console.WriteLine("Remote IP Address: " + IPAddress.Parse(remoteEndPoint.Address.ToString()));
        if (localEndPoint != null)
            Console.WriteLine("Local IP Address: " + IPAddress.Parse(localEndPoint.Address.ToString()));
        Console.WriteLine("Waiting for another player...");

        Board b = new();
        bool whitePlayer = new Random().Next(0, 2) == 0;
        bool playing = false;
        bool whiteTurn = true;
        bool doNotUpdate = false;

        var disconnectAction = () =>
        {
            Console.WriteLine("Exiting...");

            server.Dispose();
            connectedClient?.Dispose();
            inGame = false;
        };
        server.OnConnect += (sender, client) =>
        {
            if (listening)
            {
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
                System.Console.WriteLine("Connected!");
                listening = false;
                playing = true;
            }
            else if (message.Data[0] == GeneralPacketCodes.MAKE_MOVE)
            {
                whiteTurn = !whiteTurn;
                byte[] moveData = message.Data[1..^0];
                string move = Board.CoordsToAlgebraic((moveData[0], moveData[1])) +
                                Board.CoordsToAlgebraic((moveData[2], moveData[3])) +
                                (moveData.Length == 5 ?
                                    Board.PieceTypeToAlgebraicChar((PieceType)moveData[4])
                                    : "");

                b.MakeMove(move);
                doNotUpdate = false;
            }
        };
        server.OnDisconnect += (sender, message) =>
        {
            if (!inGame)
                return;

            Console.WriteLine($"Client disconnected...\nReturning to Main Menu in 3 seconds...");
            Thread.Sleep(3000);
            disconnectAction();
        };

        while (true)
        {
            if (!inGame)
                return;
            if (!playing || connectedClient == null)
            {
                Thread.Sleep(MS_PER_UPDATE);
                continue;
            }

            bool _update = !doNotUpdate;
            doNotUpdate = false;

            if (_update)
            {
                RenderBoard(b);

                if (b.IsInCheckMate(whiteTurn))
                {
                    System.Console.WriteLine("Checkmate! " +
                        (!whiteTurn ? "White" : "Black") + " wins!");
                    System.Console.WriteLine("Returning to Main Menu...");

                    disconnectAction();
                    continue;
                }
                else if (b.IsInCheck(whiteTurn))
                {
                    System.Console.WriteLine((whiteTurn ? "White" : "Black") + "is in check!");
                }
                else if (b.IsInStalemate(whiteTurn))
                {
                    System.Console.WriteLine((whiteTurn ? "White" : "Black") + "Stalemate! IT'S A DRAW!");

                    System.Console.WriteLine("Returning to Main Menu...");

                    disconnectAction();
                    continue;
                }
            }

            if (whiteTurn == whitePlayer)
            {
                MultiplayerTurn(connectedClient, b, whitePlayer, out bool exit);
                if (exit)
                {
                    disconnectAction();
                    continue;
                }
                whiteTurn = !whiteTurn;
            }
            else
            {
                if (_update)
                    Console.WriteLine("Waiting for " + (!whitePlayer ? "white" : "black") + " player to play...");

                doNotUpdate = true;
                Thread.Sleep(MS_PER_UPDATE);
                continue;
            }
        }
    }
    static void MultiplayerTurn(EasyTcpClient client, Board b, bool whitePlayer, out bool exit)
    {
        exit = false;

    SELECT_MOVE:
        Console.WriteLine();
        System.Console.Write("{0} Turn. Enter a move to make (ex. e4e5) or piece to move (ex. e4):\n> ",
            whitePlayer ? "White" : "Black");

        string input = Console.ReadLine() ?? "";
        MoveData data;

        while (true)
        {
            if (input == "exit")
            {
                exit = true;
                return;
            }

            try
            {
                data = Board.AlgebraicToMoveData(input);
            }
            catch
            {
                System.Console.WriteLine("Invalid input.");
                System.Console.Write("Enter a valid algebraic expression:\n> ");
                input = Console.ReadLine() ?? "";
                continue;
            }

            break;
        }

        (int x, int y) coords = data.from;

        if (b.GetPieceAt(coords).isWhite != whitePlayer)
        {
            System.Console.WriteLine("You can't move that piece!");
            goto SELECT_MOVE;
        }
        else if (b.GetPieceAt(coords).pieceType == PieceType.None)
        {
            System.Console.WriteLine("There is no piece in that spot.");
            goto SELECT_MOVE;
        }

        var possibleMoves = b.GetPossibleMoves(coords);

        if (possibleMoves.Length == 0)
        {
            System.Console.WriteLine("That piece can't move!");
            goto SELECT_MOVE;
        }

        MoveData moveMade;

        if (input.Length >= 4 && possibleMoves.Any(d => d.algebraic == input))
        {
            possibleMoves = possibleMoves.Where(d => d.algebraic == input).ToArray();
            b.MakeMove(possibleMoves[0]);
            moveMade = possibleMoves[0];
        }
        else
        {
            System.Console.WriteLine();

            System.Console.WriteLine("Select move to make:");

            for (int i = 0; i < possibleMoves.Length; i++)
            {
                System.Console.WriteLine($"{i} - {possibleMoves[i].algebraic}");
            }

            System.Console.WriteLine($"{possibleMoves.Length} - Select another piece.");
            System.Console.Write("> ");

            string moveInput = Console.ReadLine() ?? "";
            while (!int.TryParse(moveInput, out int option))
            {
                if (moveInput == "exit")
                {
                    exit = true;
                    return;
                }

                System.Console.Write("Invalid Input. Select an option from the previous list:\n> ");
                moveInput = Console.ReadLine() ?? "";
            }

            int move = int.Parse(moveInput);

            if (move >= possibleMoves.Length || move < 0)
                goto SELECT_MOVE;

            b.MakeMove(possibleMoves[move]);
            moveMade = possibleMoves[move];
        }

        (int moveFromX, int moveFromY) = moveMade.from;
        (int moveToX, int moveToY) = moveMade.to;

        byte[] bufferData;
        if (moveMade.promotion == PieceType.None)
        {
            bufferData = new byte[]
           {
                GeneralPacketCodes.MAKE_MOVE,
                (byte)moveFromX, (byte)moveFromY,
                (byte)moveToX, (byte)moveToY
           };
        }
        else
        {
            bufferData = new byte[]
           {
                GeneralPacketCodes.MAKE_MOVE,
                (byte)moveFromX, (byte)moveFromY,
                (byte)moveToX,(byte)moveToY,
                (byte)moveMade.promotion
           };
        }

        client.Send(bufferData);
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
                System.Console.WriteLine((whiteTurn ? "White" : "Black") + "Stalemate! IT'S A DRAW!");
                System.Console.WriteLine("Press P to play again!");
                if (Console.ReadKey(true).Key == ConsoleKey.P)
                    goto NEWGAME;
                else break;
            }

            Console.WriteLine();

            Console.WriteLine($"Write \"exit\" in any input to return to the Main Menu.");

        SELECT_MOVE:
            System.Console.WriteLine();
            System.Console.Write("{0} Turn. Enter a move to make (ex. e4e5), a piece to move (ex. e4) or write \"UNDO\" to undo:\n> ",
                whiteTurn ? "White" : "Black");

            string input = (Console.ReadLine() ?? "").ToLower();
            if (input == "undo")
            {
                b.UndoMove();
                whiteTurn = !whiteTurn;
                continue;
            }
            else if (input == "exit")
            {
                Console.WriteLine($"Returning to Main Menu...");
                Thread.Sleep(500);
                return;
            }
            try
            {
                MoveData tryData = Board.AlgebraicToMoveData(input);
            }
            catch
            {
                Console.WriteLine("Invalid input.");
                goto SELECT_MOVE;
            }

            (int x, int y) coords = Board.AlgebraicPairToCoords(input[0..2]);

            if (b.GetPieceAt(coords).isWhite != whiteTurn)
            {
                System.Console.WriteLine("You can't move that piece!");
                goto SELECT_MOVE;
            }
            else if (b.GetPieceAt(coords).pieceType == PieceType.None)
            {
                System.Console.WriteLine("There is no piece in that spot.");
                goto SELECT_MOVE;
            }

            var possibleMoves = b.GetPossibleMoves(coords);

            if (possibleMoves.Length == 0)
            {
                System.Console.WriteLine("That piece can't move!");
                goto SELECT_MOVE;
            }

            if (input.Length >= 4 && possibleMoves.Any(d => d.algebraic == input))
            {
                possibleMoves = possibleMoves.Where(d => d.algebraic == input).ToArray();
                b.MakeMove(possibleMoves[0]);
            }
            else
            {
                System.Console.WriteLine();

                System.Console.WriteLine("Select move to make:");

                for (int i = 0; i < possibleMoves.Length; i++)
                {
                    System.Console.WriteLine($"{i} - {possibleMoves[i].algebraic}");
                }

                System.Console.WriteLine($"{possibleMoves.Length} - Select another piece.");
                System.Console.Write("> ");

                int move;
                while (!int.TryParse(Console.ReadLine(), out move) || move > possibleMoves.Length || move < 0)
                    Console.Write("Invalid option, choose one from the list above\n> ");

                if (move == possibleMoves.Length)
                    goto SELECT_MOVE;

                b.MakeMove(possibleMoves[move]);
            }

            whiteTurn = !whiteTurn;
        }
    }
    const ConsoleColor whiteColor = ConsoleColor.White,
                        blackColor = ConsoleColor.Black,
                        backgroundColor = ConsoleColor.DarkGray;
    public static void RenderBoard(Board board)
    {
        Console.Clear();
        Console.WriteLine();

        Console.BackgroundColor = backgroundColor;

        ConsoleColor col = Console.ForegroundColor;
        Console.WriteLine(HEADER);
        for (int y = 7; y >= 0; y--)
        {
            if (y != 7)
            {
                Console.WriteLine(SEPARATOR);
            }
            Console.Write($"{y + 1} │");
            for (int x = 0; x < 8; x++)
            {
                Piece piece = board.GetPieceAt((x, y));

                Console.ForegroundColor = piece.isWhite ? whiteColor : blackColor;
                Console.Write(" {0} ",
                Board.PieceTypeToAlgebraicChar(piece.pieceType).ToString().ToUpper());

                Console.ForegroundColor = col;
                Console.Write("│");
            }
            Console.Write($" {y + 1}");
            Console.WriteLine();
        }
        Console.WriteLine(FOOTER);
        System.Console.WriteLine();
    }
}