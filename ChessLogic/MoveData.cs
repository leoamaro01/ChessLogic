using static PixelDashCore.ChessLogic.Board;

namespace PixelDashCore.ChessLogic;

public struct MoveData
{
    public string algebraic;
    public (int x, int y) from;
    public (int x, int y) to;
    public (int x, int y)? specialTarget;
    public PieceType promotion;

    public MoveData(string algebraic, (int x, int y) from,
    (int x, int y) to, (int x, int y)? specialTarget = null, PieceType promotion = PieceType.None)
    {
        this.algebraic = algebraic;
        this.from = from;
        this.to = to;
        this.promotion = promotion;
        this.specialTarget = specialTarget;
    }
    public MoveData((int x, int y) from, (int x, int y) to,
    (int x, int y)? specialTarget = null, PieceType promotion = PieceType.None)
    {
        this.algebraic = CoordsToAlgebraic(from) + CoordsToAlgebraic(to);
        if (promotion != PieceType.None)
            this.algebraic += PieceTypeToAlgebraicChar(promotion);

        this.from = from;
        this.to = to;
        this.promotion = promotion;
        this.specialTarget = specialTarget;
    }
}
