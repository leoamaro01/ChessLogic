using System.Diagnostics.CodeAnalysis;
using static PixelDashCore.ChessLogic.Board;

namespace PixelDashCore.ChessLogic;

public struct MoveData
{
    public string algebraic;
    public (int x, int y) from;
    public (int x, int y) to;
    public (int x, int y)? _specialTarget;
    public PieceType promotion;

    // This is used for separating library-defined moves from user-defined ones.
    internal bool _safe = false;

    public MoveData(string algebraic, (int x, int y) from,
    (int x, int y) to)
    {
        this.algebraic = algebraic;
        this.from = from;
        this.to = to;
        this.promotion = PieceType.None;
        this._specialTarget = null;
    }
    public MoveData((int x, int y) from, (int x, int y) to)
    {
        this.algebraic = CoordsToAlgebraic(from) + CoordsToAlgebraic(to);
        this.from = from;
        this.to = to;
        this.promotion = PieceType.None;
        this._specialTarget = null;
    }
    internal MoveData((int x, int y) from, (int x, int y) to,
    (int x, int y) specialTarget)
    {
        this.algebraic = CoordsToAlgebraic(from) + CoordsToAlgebraic(to);

        this.from = from;
        this.to = to;
        this.promotion = PieceType.None;
        this._specialTarget = specialTarget;
    }
    public MoveData(string algebraic, (int x, int y) from, (int x, int y) to,
    PieceType promotion)
    {
        this.algebraic = algebraic;

        this.from = from;
        this.to = to;
        this.promotion = promotion;
        this._specialTarget = null;
    }
    internal MoveData(string algebraic, (int x, int y) from, (int x, int y) to,
    (int x, int y) specialTarget)
    {
        this.algebraic = algebraic;

        this.from = from;
        this.to = to;
        this.promotion = PieceType.None;
        this._specialTarget = specialTarget;
    }
    public MoveData((int x, int y) from, (int x, int y) to,
    PieceType promotion)
    {
        this.algebraic = CoordsToAlgebraic(from) + CoordsToAlgebraic(to);

        this.from = from;
        this.to = to;
        this.promotion = promotion;
        this._specialTarget = null;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj != null && obj.GetType() == typeof(MoveData))
        {
            MoveData other = (MoveData)obj;
            return algebraic == other.algebraic;
        }
        else
            return base.Equals(obj);
    }

    public static bool operator ==(MoveData lh, MoveData rh)
    => lh.Equals(rh);
    public static bool operator !=(MoveData lh, MoveData rh)
    => !lh.Equals(rh);

    public override int GetHashCode() => algebraic.GetHashCode();
}
