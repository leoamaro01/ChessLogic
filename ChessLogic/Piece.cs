namespace PixelDashCore.ChessLogic;

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
