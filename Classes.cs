using static PixelDashCore.ChessLogic.Board;

namespace PixelDashCore.ChessLogic;

public enum PieceType
{
    None = -1, Pawn, Rook, Knight, Bishop, Queen, King
}
public struct Piece
{
    public PieceType pieceType;
    public bool isWhite;

    public Piece(PieceType pieceType, bool isWhite = true)
    {
        this.pieceType = pieceType;
        this.isWhite = isWhite;
    }

    public static Piece None => new Piece(PieceType.None);
}
public struct MoveData
{
    public string algebraic;
    public (int x, int y) from;
    public (int x, int y) to;
    public PieceType promotion;

    public MoveData(string algebraic, (int x, int y) from, (int x, int y) to, PieceType promotion = PieceType.None)
    {
        this.algebraic = algebraic;
        this.from = from;
        this.to = to;
        this.promotion = promotion;
    }
    public MoveData((int x, int y) from, (int x, int y) to, PieceType promotion = PieceType.None)
    {
        this.algebraic = CoordsToAlgebraic(from) + CoordsToAlgebraic(to);
        if (promotion != PieceType.None)
            this.algebraic += PieceTypeToAlgebraicChar(promotion);

        this.from = from;
        this.to = to;
        this.promotion = promotion;
    }
}
public class Board
{
    const string standardWhiteFormation = "12345321";
    private List<MoveData> moveHistory;
    private Piece[,] pieces;

    public Board(Board other)
    {
        pieces = new Piece[8, 8];

        //Copy source board
        for (int x = 0; x < pieces.GetLength(0); x++)
            for (int y = 0; y < pieces.GetLength(1); y++)
                pieces[x, y] = other.pieces[x, y];

        moveHistory = new List<MoveData>(other.moveHistory);
    }
    public Board(MoveData[] sourceHistory)
    {
        pieces = Initialize();
        moveHistory = new List<MoveData>();

        foreach (var move in sourceHistory)
            MakeMove(move);
    }
    public Board()
    {
        pieces = Initialize();
        moveHistory = new List<MoveData>();
    }
    private Piece[,] Initialize()
    {
        pieces = new Piece[8, 8];

        //Setting white pieces
        for (int i = 0; i < pieces.GetLength(0); i++)
        {
            int value = int.Parse(standardWhiteFormation[i].ToString());
            pieces[i, 0] = new Piece((PieceType)value, true);
        }
        for (int i = 0; i < pieces.GetLength(0); i++)
            pieces[i, 1] = new Piece(PieceType.Pawn, true);

        //Setting black pieces
        for (int i = 0; i < pieces.GetLength(0); i++)
        {
            int value = int.Parse(standardWhiteFormation[i].ToString());
            pieces[i, 7] = new Piece((PieceType)value, false);
        }
        for (int i = 0; i < pieces.GetLength(0); i++)
            pieces[i, 6] = new Piece(PieceType.Pawn, false);

        return pieces;
    }
    public Piece GetPieceAt((int x, int y) coords)
    {
        if (!IsValidPlace(coords))
            throw new ArgumentOutOfRangeException("Invalid Coordinates. Axes must be 0-indexed and up to 7.");

        return pieces[coords.x, coords.y];
    }

    // inlcudeKills Values:
    // > 0: Only include kills
    // = 0: Include kills as well as moves
    // < 0: Only include moves, no kills
    public MoveData[] GetPossibleMoves((int x, int y) pieceCoords, int includeKills = 0)
    {
        if (!IsValidPlace(pieceCoords))
            throw new ArgumentOutOfRangeException("Invalid Coordinates. Axes must be 0-indexed and up to 7.");

        Piece piece = GetPieceAt(pieceCoords);
        if (piece.pieceType == PieceType.None)
            return new MoveData[] { };

        var movesCoords = new List<(int x, int y)>();

        //Moves any piece to a point ignoring anything in between, if there is a piece in that point it will include the killing move if possible
        var pointMover = (int xChange, int yChange) =>
        {
            (int x, int y) point = (pieceCoords.x + xChange, pieceCoords.y + yChange);
            if (IsValidPlace(point))
            {
                Piece obstacle = GetPieceAt(point);
                if (obstacle.pieceType != PieceType.None)
                {
                    if (obstacle.isWhite != piece.isWhite && includeKills >= 0)
                        movesCoords.Add(point);
                }
                else if (includeKills <= 0)
                    movesCoords.Add(point);
            }
        };
        //Moves any piece in a straight line until it collides with another piece or the edge of the board, if will include the killing move if possible
        var straightMover = (int xChange, int yChange) =>
                {
                    for (int x = pieceCoords.x + xChange, y = pieceCoords.y + yChange; IsValidPlace((x, y)); x += xChange, y += yChange)
                    {
                        Piece obstacle = GetPieceAt((x, y));
                        if (obstacle.pieceType != PieceType.None)
                        {
                            if (piece.isWhite != obstacle.isWhite && includeKills >= 0)
                            {
                                movesCoords.Add((x, y));
                                break;
                            }
                            else break;
                        }
                        else if (includeKills <= 0)
                            movesCoords.Add((x, y));
                    }
                };

        switch (piece.pieceType)
        {
            case PieceType.Pawn:
                //The only change for white or black pawns is the vertical forward
                int forwardY = piece.isWhite ? pieceCoords.y + 1 : pieceCoords.y - 1;

                if (includeKills <= 0)
                {
                    (int x, int y) forward = (pieceCoords.x, forwardY);
                    if (GetPieceAt(forward).pieceType == PieceType.None)
                        movesCoords.Add(forward);
                }

                if (includeKills >= 0)
                {
                    (int x, int y) diagRight = (pieceCoords.x + 1, forwardY);
                    (int x, int y) diagLeft = (pieceCoords.x - 1, forwardY);

                    if (GetPieceAt(diagRight).pieceType != PieceType.None)
                        movesCoords.Add(diagRight);

                    if (GetPieceAt(diagLeft).pieceType != PieceType.None)
                        movesCoords.Add(diagLeft);
                }
                //TODO: en-passant
                break;
            case PieceType.Bishop:
                //Diagonal Up Right
                straightMover(1, 1);
                //Diagonal Up Left
                straightMover(-1, 1);
                //Diagonal Down Right
                straightMover(1, -1);
                //Diagonal Down Left
                straightMover(-1, -1);
                break;
            case PieceType.Rook:
                //Up
                straightMover(0, 1);
                //Right
                straightMover(1, 0);
                //Down
                straightMover(0, -1);
                //Left
                straightMover(-1, 0);
                break;
            case PieceType.Queen:
                //Diagonal Up Right
                straightMover(1, 1);
                //Diagonal Up Left
                straightMover(-1, 1);
                //Diagonal Down Right
                straightMover(1, -1);
                //Diagonal Down Left
                straightMover(-1, -1);
                //Up
                straightMover(0, 1);
                //Right
                straightMover(1, 0);
                //Down
                straightMover(0, -1);
                //Left
                straightMover(-1, 0);
                break;
            case PieceType.Knight:
                //Knight moves in all x;y combinations of 1, -1, 2, -2
                pointMover(1, 2);
                pointMover(-1, 2);
                pointMover(-1, -2);
                pointMover(1, -2);
                pointMover(2, 1);
                pointMover(-2, 1);
                pointMover(-2, -1);
                pointMover(2, -1);
                break;
            case PieceType.King:
                pointMover(1, 1);
                pointMover(0, 1);
                pointMover(-1, 1);
                pointMover(1, 0);
                pointMover(-1, 0);
                pointMover(1, -1);
                pointMover(0, -1);
                pointMover(-1, -1);

                //TODO: Castling
                break;
        }

        if (movesCoords.Count == 0)
            return new MoveData[] { };

        List<MoveData> possibleMoves = new List<MoveData>();
        if (piece.pieceType == PieceType.Pawn && movesCoords[0].y == (piece.isWhite ? 7 : 0))
        {
            string promotionPieces = "qrbn";
            foreach (var coord in movesCoords)
            {
                foreach (char prom in promotionPieces)
                    possibleMoves.Add(new MoveData(pieceCoords, coord, AlgebraicCharToPieceType(prom)));
            }
        }
        else
            possibleMoves.AddRange(movesCoords.Select(c => new MoveData(pieceCoords, c)));

        return possibleMoves.Where(data => !InCheckAfterMove(data, piece.isWhite)).ToArray();
    }
    public bool IsInStalemate(bool whiteStalemate)
    {
        var teamPieces = GetAllPiecesCoords(p => p.isWhite == whiteStalemate);

        //Check for stalemate
        foreach (var piece in teamPieces)
            if (GetPossibleMoves(piece).Length != 0)
                return false;

        return true;
    }
    public bool IsInCheck(bool whiteKing)
    {
        (int x, int y)[] enemyPieces = GetAllPiecesCoords(p => p.isWhite != whiteKing);
        (int x, int y) king = GetAllPiecesCoords(p => p.isWhite == whiteKing && p.pieceType == PieceType.King)[0];

        foreach (var piece in enemyPieces)
        {
            var killMoves = GetPossibleMoves(piece, 1);

            foreach (var kill in killMoves)
                if (kill.to == king)
                    return true;
        }

        return false;
    }
    public bool InCheckAfterMove(MoveData move, bool whiteKing)
    {
        Board copy = new Board(this);

        copy.MakeMove(move);

        return copy.IsInCheck(whiteKing);
    }
    public Piece[] GetAllPieces() => GetAllPieces(p => true);
    public Piece[] GetAllPieces(Func<Piece, bool> filter)
    {
        List<Piece> result = new List<Piece>();

        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                if (pieces[x, y].pieceType == PieceType.None)
                    continue;

                if (filter(pieces[x, y]))
                    result.Add(pieces[x, y]);
            }

        return result.ToArray();
    }
    public (int x, int y)[] GetAllPiecesCoords() => GetAllPiecesCoords(p => true);
    public (int x, int y)[] GetAllPiecesCoords(Func<Piece, bool> filter)
    {
        List<(int x, int y)> result = new List<(int x, int y)>();

        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                if (pieces[x, y].pieceType == PieceType.None)
                    continue;

                if (filter(pieces[x, y]))
                    result.Add((x, y));
            }

        return result.ToArray();
    }
    public void MakeMove(MoveData move)
    {
        pieces[move.to.x, move.to.y] = GetPieceAt(move.from);
        pieces[move.from.x, move.from.y] = Piece.None;

        if (move.promotion != PieceType.None)
            pieces[move.to.x, move.to.y].pieceType = move.promotion;

        moveHistory.Add(move);
    }
    public void MakeMove(string algebraicMove) => MakeMove(AlgebraicToMoveData(algebraicMove));
    public void UndoMove(int movesToUndo = 1)
    {
        MoveData[] undoneHistory = new MoveData[moveHistory.Count - movesToUndo];
        moveHistory.CopyTo(0, undoneHistory, 0, undoneHistory.Length);

        Board undone = new Board(undoneHistory);

        for (int x = 0; x < pieces.GetLength(0); x++)
            for (int y = 0; y < pieces.GetLength(1); x++)
                pieces[x, y] = undone.pieces[x, y];

        moveHistory = new List<MoveData>(undoneHistory);
    }
    public static string MoveDataToAlgebraic(MoveData data)
    {
        string alg = "";
        alg += CoordsToAlgebraic(data.from);
        alg += CoordsToAlgebraic(data.to);

        if (data.promotion != PieceType.None)
            alg += PieceTypeToAlgebraicChar(data.promotion);

        return alg;
    }
    public static string CoordsToAlgebraic((int x, int y) coords)
    {
        string alg = "";
        switch (coords.x)
        {
            case 0:
                alg += 'a';
                break;
            case 1:
                alg += 'b';
                break;
            case 2:
                alg += 'c';
                break;
            case 3:
                alg += 'd';
                break;
            case 4:
                alg += 'e';
                break;
            case 5:
                alg += 'f';
                break;
            case 6:
                alg += 'g';
                break;
            case 7:
                alg += 'h';
                break;
        }
        alg += coords.y + 1;

        return alg;
    }
    public static char PieceTypeToAlgebraicChar(PieceType type)
    {
        switch (type)
        {
            case PieceType.Bishop:
                return 'b';
            case PieceType.Queen:
                return 'q';
            case PieceType.Rook:
                return 'r';
            case PieceType.Pawn:
                return 'p';
            case PieceType.Knight:
                return 'n';
            case PieceType.King:
                return 'k';
            default:
                return ' ';
        }
    }
    public static MoveData AlgebraicToMoveData(string algebraic)
    {
        if (algebraic.Length < 4 || algebraic.Length > 5)
            throw new ArgumentException("Invalid algebraic expression");

        algebraic = algebraic.ToLower();

        int[] coords = algebraic.Substring(0, 4).Select(c => AlgebraicCharToIndex(c)).ToArray();
        if (coords.Any(n => n == -1))
            throw new ArgumentException("Invalid algebraic expression");

        // So far the only case of 5-length algebraic notation is when a pawn is promoted, this should be updated if other cases are discovered
        PieceType promotion = algebraic.Length == 4 ? PieceType.None : AlgebraicCharToPieceType(algebraic[5]);

        return new MoveData(
            algebraic,
            (coords[0], coords[1]),
            (coords[2], coords[3]),
            promotion);
    }
    public static PieceType AlgebraicCharToPieceType(char alg)
    {
        switch (alg)
        {
            case 'r':
                return PieceType.Rook;
            case 'n':
                return PieceType.Knight;
            case 'b':
                return PieceType.Bishop;
            case 'k':
                return PieceType.King;
            case 'q':
                return PieceType.Queen;
            case 'p':
                return PieceType.Pawn;
            default:
                return PieceType.None;
        }
    }
    public static int AlgebraicCharToIndex(char alg)
    {
        switch (alg)
        {
            case 'a':
            case '1':
                return 0;
            case 'b':
            case '2':
                return 1;
            case 'c':
            case '3':
                return 2;
            case 'd':
            case '4':
                return 3;
            case 'e':
            case '5':
                return 4;
            case 'f':
            case '6':
                return 5;
            case 'g':
            case '7':
                return 6;
            case 'h':
            case '8':
                return 7;
            default:
                return -1;
        }
    }
    public static bool IsValidPlace((int x, int y) place)
    => place.x >= 0 && place.x < 8 && place.y >= 0 && place.y < 8;
}