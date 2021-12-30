namespace PixelDashCore.ChessLogic;

public class Board
{
    //  Standard white-side pieces formation rook-knight-bishop-queen-king-bishop-knight-rook
    const string standardWhiteFormation = "12345321";
    //  History of every move made in the game, this is modified automatically.
    private List<MoveData> moveHistory;
    private readonly Piece[,] pieces;

    public Board(Board other)
    {
        pieces = new Piece[8, 8];

        //  Copy source board
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                pieces[x, y] = other.pieces[x, y];

        //  Copy source moveHistory, making boards (games) identical.
        moveHistory = new List<MoveData>(other.moveHistory);
    }
    public Board(MoveData[] sourceHistory)
    {
        //  Create a new board from a source MoveData history. Creates a new board and performs
        //  every move in the provided history.
        pieces = GetDefaultBoard();
        moveHistory = new List<MoveData>();

        foreach (var move in sourceHistory)
            MakeMove(move);
    }
    public Board()
    {
        // Default constructor, initializes the default chess formation and creates a new empty MoveData history.
        pieces = GetDefaultBoard();
        moveHistory = new List<MoveData>();
    }
    public static Piece[,] GetDefaultBoard()
    {
        // This method initializes a board in default chess formation.
        Piece[,] pieces = new Piece[8, 8];

        // Setting white pieces
        for (int i = 0; i < 8; i++)
        {
            int value = int.Parse(standardWhiteFormation[i].ToString());
            pieces[i, 0] = new Piece((PieceType)value, true);
        }
        for (int i = 0; i < 8; i++)
            pieces[i, 1] = new Piece(PieceType.Pawn, true);

        //Setting black pieces
        for (int i = 0; i < 8; i++)
        {
            int value = int.Parse(standardWhiteFormation[i].ToString());
            pieces[i, 7] = new Piece((PieceType)value, false);
        }
        for (int i = 0; i < 8; i++)
            pieces[i, 6] = new Piece(PieceType.Pawn, false);

        // Filling empty spaces with None pieces, as Piece is non-nullable
        // and the default value for a piece is a black pawn for some reason.
        for (int x = 0; x < 8; x++)
            for (int y = 2; y < 6; y++)
                pieces[x, y] = Piece.None;

        return pieces;
    }
    public Piece GetPieceAt((int x, int y) coords)
    {
        // Returns the piece at `coords` if it is a valid place.
        AssertCoords(coords);

        return pieces[coords.x, coords.y];
    }

    // inlcudeKills Values:
    // > 0: Only include kills
    // = 0: Include kills as well as moves
    // < 0: Only include moves, no kills
    // checkCheck:
    // Whether to check if moves would leave their own team in check (useful to avoid creating stack overflows.)
    public MoveData[] GetPossibleMoves((int x, int y) pieceCoords, int includeKills = 0, bool checkCheck = true)
    {
        // This method returns every possible move a piece can make, including promotions. 
        // Kill inclusion is optional and customizable.

        // Checks if the provided coords are valid.
        AssertCoords(pieceCoords);

        // Throw an exception if there is no piece at the requested spot.
        Piece piece = GetPieceAt(pieceCoords);
        if (piece.pieceType == PieceType.None)
            throw new InvalidPlaceException($"There is no piece at ({pieceCoords.x}, {pieceCoords.y}), can't get possible moves from an empty spot");

        // These will be the resulting lists.
        List<(int x, int y)> movesCoords = new();
        List<MoveData> possibleMoves = new();

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
                    for (int x = pieceCoords.x + xChange, y = pieceCoords.y + yChange;
                    IsValidPlace((x, y));
                    x += xChange, y += yChange)
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

        // These will be useful later.
        int[] dirArrayX, dirArrayY;

        // Now we check for possible moves depending on the piece type.
        switch (piece.pieceType)
        {
            // In pawn movement management we:
            // -Check if we can move forward.
            //  -If we are on the initial position, check if we can move two steps forward.
            // -Check if we can make diagonal kills.
            // -Check for en-passant kills.
            case PieceType.Pawn:
                // The only change for white or black pawns is the vertical forward
                int forwardY = piece.isWhite ? pieceCoords.y + 1 : pieceCoords.y - 1;
                int forwardYPlus2 = piece.isWhite ? pieceCoords.y + 2 : pieceCoords.y - 2;

                if (includeKills <= 0)
                {
                    (int x, int y) forward = (pieceCoords.x, forwardY);
                    if (GetPieceAt(forward).pieceType == PieceType.None)
                    {
                        movesCoords.Add(forward);

                        // Initial move
                        if ((piece.isWhite && pieceCoords.y == 1) || (!piece.isWhite && pieceCoords.y == 6))
                        {
                            (int x, int y) forwardPlus2 = (pieceCoords.x, forwardYPlus2);
                            if (GetPieceAt(forwardPlus2).pieceType == PieceType.None)
                                movesCoords.Add(forwardPlus2);
                        }
                    }
                }
                // Here are all the diagonal kill moves and en-passant.
                if (includeKills >= 0)
                {
                    var diagRight = (pieceCoords.x + 1, forwardY);
                    var diagLeft = (pieceCoords.x - 1, forwardY);
                    var left = (pieceCoords.x - 1, pieceCoords.y);
                    var right = (pieceCoords.x + 1, pieceCoords.y);

                    if (IsValidPlace(diagRight) && GetPieceAt(diagRight).pieceType != PieceType.None)
                    {
                        if (GetPieceAt(diagRight).isWhite != piece.isWhite)
                            movesCoords.Add(diagRight);
                    }
                    else if (IsValidPlace(diagRight) && pieceCoords.y == (piece.isWhite ? 4 : 3))
                    {
                        if (CanEnPassant(pieceCoords, diagRight))
                            // En-passant targets are added as special targets to MoveData structures.
                            // The move can be made anyway without the special target, but it would
                            // make the whole en-passant check all over again.
                            possibleMoves.Add(new MoveData(pieceCoords, diagRight, right));
                    }

                    if (IsValidPlace(diagLeft) && GetPieceAt(diagLeft).pieceType != PieceType.None)
                    {
                        if (GetPieceAt(diagLeft).isWhite != piece.isWhite)
                            movesCoords.Add(diagLeft);
                    }
                    else if (IsValidPlace(diagLeft) && pieceCoords.y == (piece.isWhite ? 4 : 3))
                    {
                        // En-passant check on the other side.
                        if (CanEnPassant(pieceCoords, diagLeft))
                            possibleMoves.Add(new MoveData(pieceCoords, diagLeft, left));
                    }
                }

                // If we are a pawn, and we are moving into the last rank, promotion must occur.
                if (movesCoords.Count > 0 && movesCoords[0].y == (piece.isWhite ? 7 : 0))
                {
                    // Manually create promotion moves, while clearing the moves from the moveCoords
                    // array, effectively skipping the (useless) automatic process.
                    string promotionPieces = "qrbn";
                    foreach (var coord in movesCoords)
                        foreach (char prom in promotionPieces)
                            possibleMoves.Add(new MoveData(pieceCoords, coord, promotion: AlgebraicCharToPieceType(prom)));

                    movesCoords.Clear();
                }
                break;
            // Working with the bishop, rook and queen is way simpler, create a couple 
            // directional arrays and move the piece in a straight line using the straightMover
            // we created earlier.
            case PieceType.Bishop:
                dirArrayX = new int[] { 1, -1, 1, -1 };
                dirArrayY = new int[] { 1, 1, -1, -1 };

                for (int i = 0; i < dirArrayX.Length; i++)
                    straightMover(dirArrayX[i], dirArrayY[i]);
                break;
            case PieceType.Rook:
                dirArrayX = new int[] { 0, 1, 0, -1 };
                dirArrayY = new int[] { 1, 0, -1, 0 };

                for (int i = 0; i < dirArrayX.Length; i++)
                    straightMover(dirArrayX[i], dirArrayY[i]);
                break;
            case PieceType.Queen:
                dirArrayX = new int[] { 1, -1, 1, -1, 0, 1, 0, -1 };
                dirArrayY = new int[] { 1, 1, -1, -1, 1, 0, -1, 0 };

                for (int i = 0; i < dirArrayX.Length; i++)
                    straightMover(dirArrayX[i], dirArrayY[i]);
                break;
            // Same thing with the knight and king except we'll be using pointMover
            // instead of straightMover.
            case PieceType.Knight:
                dirArrayX = new int[] { 1, 1, -1, -1, 2, 2, -2, -2 };
                dirArrayY = new int[] { 2, -2, 2, -2, 1, -1, 1, -1 };

                for (int i = 0; i < dirArrayX.Length; i++)
                    pointMover(dirArrayX[i], dirArrayY[i]);
                break;
            case PieceType.King:
                dirArrayX = new int[] { -1, -1, -1, 0, 0, 1, 1, 1 };
                dirArrayY = new int[] { 1, 0, -1, 1, -1, 1, 0, -1 };

                for (int i = 0; i < dirArrayX.Length; i++)
                    pointMover(dirArrayX[i], dirArrayY[i]);

                // Now we check for castling conditions!
                if (includeKills <= 0)
                {
                    // If castling is possible we add the castling move directly to possibleMoves
                    // and set the rook as a special target so the system can easily recognize castling
                    // and avoid making all the weird checks
                    if (CanCastle(piece.isWhite, true))
                        possibleMoves.Add(new MoveData(pieceCoords, (6, pieceCoords.y), (7, pieceCoords.y)));
                    if (CanCastle(piece.isWhite, false))
                        possibleMoves.Add(new MoveData(pieceCoords, (2, pieceCoords.y), (0, pieceCoords.y)));
                }
                break;
        }

        // Transform move coords to MoveData structures.
        if (movesCoords.Count != 0)
            possibleMoves.AddRange(movesCoords.Select(c => new MoveData(pieceCoords, c)));

        MoveData[] movesArray;
        // Moves shouldn't be possible if they would get us into check.
        if (checkCheck)
            movesArray = possibleMoves.Where(data => !InCheckAfterMove(data, piece.isWhite)).ToArray();
        else
            movesArray = possibleMoves.ToArray();

        // [Internal] Mark every move as safe, they should be performed in a high-performance way,
        // avoiding unnecessary checks.
        for (int i = 0; i < movesArray.Length; i++)
            movesArray[i]._safe = true;

        return movesArray;
    }
    public bool IsInStalemate(bool whiteStalemate)
    {
        // Stalemate happens when a team can't perform any legal moves, provoking instant draw.
        var teamPieces = GetAllPiecesCoords(p => p.isWhite == whiteStalemate);

        //Check for stalemate
        foreach (var piece in teamPieces)
            if (GetPossibleMoves(piece).Length != 0)
                return false;

        return true;
    }
    // Checkmate is when a team is in check and can't escape it.
    public bool IsInCheckMate(bool whiteTeam) =>
        IsInCheck(whiteTeam) && IsInStalemate(whiteTeam);

    public bool IsInCheck(bool whiteKing)
    {
        // To check for if a team is in check, we will see wether any enemy piece can kill the king.
        (int x, int y)[] enemyPieces = GetAllPiecesCoords(p => (p.isWhite != whiteKing) && (p.pieceType != PieceType.None));
        (int x, int y) king = GetAllPiecesCoords(p => p.isWhite == whiteKing && p.pieceType == PieceType.King)[0];

        foreach (var piece in enemyPieces)
        {
            // checkCheck is made false here, otherwise a stack overflow would occur, because the GetPossibleMoves would be calling
            // IsInCheck, which in turn would be calling GetPossibleMoves, et cetera.
            var killMoves = GetPossibleMoves(piece, 1, false);

            foreach (var kill in killMoves)
                if (kill.to == king)
                    return true;
        }

        return false;
    }
    public bool InCheckAfterMove(MoveData move, bool whiteKing)
    {
        // Simple, create a copy of this board, make the move and check if that board is in check.

        Board copy = new(this);

        copy.MakeMove(move);

        return copy.IsInCheck(whiteKing);
    }
    public Piece[] GetAllPieces() => GetAllPieces(p => true);
    public Piece[] GetAllPieces(Func<Piece, bool> filter)
    {
        // Returns every piece in the board that passes the provided filter.

        List<Piece> result = new();

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
        // Same as before but with coords.

        List<(int x, int y)> result = new();

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
    internal void MakeMove_safe(MoveData move)
    {
        Piece source = GetPieceAt(move.from);

        if (move._specialTarget.HasValue)
        {
            // En-passant   
            if (source.pieceType == PieceType.Pawn)
            {
                // Move Pawn
                pieces[move.to.x, move.to.y] = source;
                pieces[move.from.x, move.from.y] = Piece.None;
                // Kill En-Passant target
                pieces[move._specialTarget.Value.x, move._specialTarget.Value.y].pieceType = PieceType.None;
            }
            // Castling
            else if (source.pieceType == PieceType.King)
            {
                var rook = GetPieceAt(move._specialTarget.Value);
                // Move Rook
                pieces[(move.from.x + move.to.x) / 2, move.from.y] = rook;
                pieces[move._specialTarget.Value.x, move._specialTarget.Value.y] = Piece.None;
                // Move King
                pieces[move.to.x, move.to.y] = source;
                pieces[move.from.x, move.from.y] = Piece.None;
            }
        }
        else
        {
            pieces[move.to.x, move.to.y] = source;
            pieces[move.from.x, move.from.y] = Piece.None;

            if (move.promotion != PieceType.None)
                pieces[move.to.x, move.to.y].pieceType = move.promotion;
        }

        moveHistory.Add(move);
    }
    public bool CanEnPassant((int x, int y) pawnCoords, (int x, int y) diagonalTarget)
    {
        AssertCoords(pawnCoords); AssertCoords(diagonalTarget);

        // En-passant can't possibly be the first move in a game.
        // This check is made because the en-passant check relies on past moves, so
        // if the moveHistory is corrupt this would avoid exceptions.
        if (moveHistory.Count == 0)
            return false;

        Piece pawn = GetPieceAt(pawnCoords);

        // Check for bad use of the method.
        if (pawn.pieceType != PieceType.Pawn)
            throw new InvalidPlaceException("There is no pawn in that spot");
        if (pawnCoords.x == diagonalTarget.x || pawnCoords.y == diagonalTarget.y)
            throw new InvalidPlaceException("The taregt you specified is not diagonal to the pawn");

        (int x, int y) side = (diagonalTarget.x, pawnCoords.y);

        // You can only perform en-passant to a pawn
        if (GetPieceAt(side).pieceType != PieceType.Pawn)
            return false;

        // The enemy pawn must have made the last move!
        if (moveHistory[^1].to != side)
            return false;

        // And it must have been an Initial move.
        if (moveHistory[^1].from.y != (!pawn.isWhite ? 1 : 6))
            return false;

        return true;
    }
    public bool CanCastle(bool whiteKing, bool kingSide)
    {
        // You can't castle out of check.
        if (IsInCheck(whiteKing))
            return false;

        (int x, int y) kingCoords = (4, whiteKing ? 0 : 7);
        (int x, int y) rookCoords = (kingSide ? 7 : 0, kingCoords.y);

        int castleDirection = kingSide ? 1 : -1;

        // Check if both pieces are at the right position.
        if (GetPieceAt(kingCoords).pieceType != PieceType.King || GetPieceAt(rookCoords).pieceType != PieceType.Rook)
            return false;

        // Check history to see if either the king or the rook has moved.
        foreach (var move in moveHistory)
            if (move.from == kingCoords || move.from == rookCoords)
                return false;

        // Check for pieces in between.
        for (int x = kingCoords.x + castleDirection; x != rookCoords.x - castleDirection; x += castleDirection)
            if (GetPieceAt((x, kingCoords.y)).pieceType != PieceType.None)
                return false;

        // Last but not least, lets check if we would get in check mid-move.
        if (InCheckAfterMove(new MoveData(kingCoords, (kingCoords.x + castleDirection, kingCoords.y)), whiteKing))
            return false;

        return true;
    }
    public void MakeMove(MoveData move)
    {
        // If the move was generated by the library we could avoid performing
        // extra safety checks.
        if (move._safe)
        {
            MakeMove_safe(move);
            return;
        }

        Piece source = GetPieceAt(move.from);
        Piece target = GetPieceAt(move.to);

        // Throw exceptions for stupid moves.
        if (target.pieceType != PieceType.None)
        {
            if (target.pieceType == PieceType.King)
                throw new InvalidMoveException("You can't kill a king.");
            if (target.isWhite == source.isWhite)
                throw new InvalidMoveException("You can't kill a piece of your own color.");
        }

        // En-Passant
        if (source.pieceType == PieceType.Pawn
        && move.from.x != move.to.x)
        {
            if (CanEnPassant(move.from, move.to))
            {
                (int x, int y) enPassantTarget = (move.to.x, move.from.y);

                pieces[move.to.x, move.to.y] = GetPieceAt(move.from);
                pieces[enPassantTarget.x, enPassantTarget.y] = Piece.None;
                move._specialTarget = enPassantTarget;

                moveHistory.Add(move);
                return;
            }
        }
        // Castling
        if (source.pieceType == PieceType.King
        && Math.Abs(move.to.x - move.from.x) == 2)
        {
            if (CanCastle(source.isWhite, move.to.x > move.from.x))
            {
                (int x, int y) rookCoords = (Math.Clamp((move.to.x - move.from.x) * 4, 0, 7), move.from.y);

                Piece rook = GetPieceAt(rookCoords);
                pieces[move.to.x, move.to.y] = source;
                pieces[move.from.x, move.from.y] = Piece.None;
                pieces[(move.from.x + move.to.x) / 2, move.from.y] = rook;
                pieces[rookCoords.x, rookCoords.y] = Piece.None;

                moveHistory.Add(move);
                return;
            }
        }
        pieces[move.to.x, move.to.y] = source;
        pieces[move.from.x, move.from.y] = Piece.None;

        if (move.promotion != PieceType.None)
            pieces[move.to.x, move.to.y].pieceType = move.promotion;

        moveHistory.Add(move);
    }
    public void MakeMove(string algebraicMove) => MakeMove(AlgebraicToMoveData(algebraicMove.ToLower()));
    public void UndoMove(int movesToUndo = 1)
    {
        if (moveHistory.Count < movesToUndo)
            throw new InvalidOperationException("You can't undo more moves than have been made");

        MoveData[] undoneHistory = new MoveData[moveHistory.Count - movesToUndo];
        moveHistory.CopyTo(0, undoneHistory, 0, undoneHistory.Length);

        Board undone = new(undoneHistory);

        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                pieces[x, y] = undone.pieces[x, y];

        moveHistory = new List<MoveData>(undoneHistory);
    }
    public static string MoveDataToAlgebraic(MoveData data)
    {
        if (data.algebraic != "")
            return data.algebraic;

        string alg = "";
        alg += CoordsToAlgebraic(data.from);
        alg += CoordsToAlgebraic(data.to);

        if (data.promotion != PieceType.None)
            alg += PieceTypeToAlgebraicChar(data.promotion);

        return alg;
    }
    public static (int x, int y) AlgebraicPairToCoords(string algebraic)
    {
        if (algebraic.Length != 2)
            throw new ArgumentException("Algebraic Pair must be of length 2");

        (int x, int y) = (AlgebraicCharToIndex(algebraic[0]), AlgebraicCharToIndex(algebraic[1]));

        if (x == -1 || y == -1)
            throw new ArgumentException($"Invalid algebraic expression '{algebraic}'");

        return (x, y);
    }
    public static string CoordsToAlgebraic((int x, int y) coords)
    {
        AssertCoords(coords);

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
    => type switch
    {
        PieceType.Bishop => 'b',
        PieceType.Queen => 'q',
        PieceType.King => 'k',
        PieceType.Pawn => 'p',
        PieceType.Knight => 'n',
        PieceType.Rook => 'r',
        PieceType.None or _ => ' '
    };
    public static MoveData AlgebraicToMoveData(string algebraic)
    {
        if (algebraic.Length < 4 || algebraic.Length > 5)
            throw new ArgumentException("Invalid algebraic expression");

        algebraic = algebraic.ToLower();

        // Let's transform the algebraic expression to coordinates and check for validity
        int[] coords = algebraic[0..4].Select(c => AlgebraicCharToIndex(c)).ToArray();
        if (coords.Any(n => n == -1))
            throw new ArgumentException("Invalid algebraic expression");

        // So far the only case of 5-length algebraic notation is when a pawn is promoted, 
        // this should be updated if other cases are discovered
        PieceType promotion = algebraic.Length == 4 ? PieceType.None : AlgebraicCharToPieceType(algebraic[5]);

        return new MoveData(
            algebraic,
            (coords[0], coords[1]),
            (coords[2], coords[3]),
            promotion: promotion);
    }
    public static PieceType AlgebraicCharToPieceType(char alg)
    => alg.ToString().ToLower()[0] switch
    {
        'r' => PieceType.Rook,
        'n' => PieceType.Knight,
        'b' => PieceType.Bishop,
        'k' => PieceType.King,
        'q' => PieceType.Queen,
        'p' => PieceType.Pawn,
        _ => PieceType.None,
    };

    public static int AlgebraicCharToIndex(char alg)
    => alg.ToString().ToLower()[0] switch
    {
        'a' or '1' => 0,
        'b' or '2' => 1,
        'c' or '3' => 2,
        'd' or '4' => 3,
        'e' or '5' => 4,
        'f' or '6' => 5,
        'g' or '7' => 6,
        'h' or '8' => 7,
        _ => -1
    };
    public static bool IsValidPlace((int x, int y) place)
    => place.x >= 0 && place.x < 8 && place.y >= 0 && place.y < 8;
    public static void AssertCoords((int x, int y) coords)
    {
        // Checks whether the provided coordinates are within acceptable ranges,
        // if not an exception is thrown.
        if (!IsValidPlace(coords))
            throw new InvalidPlaceException("Invalid Coordinates. Axes must be 0-indexed and up to 7.");
    }
}