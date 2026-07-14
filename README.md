# ChessLogic

A self-contained C# (.NET 6) chess library: a complete implementation of the rules of chess that any program — GUI, server, bot — can drop in to run a fully capable game.

## Features

- **Full legal move generation** — `GetPossibleMoves` returns every legal move for a piece, accounting for pins, checks, and all special rules.
- **Complete rules support** — castling (with full legality checks), en passant, pawn promotion.
- **Game-state detection** — check, checkmate, and stalemate.
- **Algebraic notation** — parse and serialize moves as strings, with helpers to convert between board coordinates and algebraic squares.
- **Move history & undo** — every move is recorded; roll back any number of moves, or reconstruct a whole board from a move list.
- **Value-type primitives** — `Piece` and `MoveData` are lightweight structs with proper equality semantics.

## Quick example

```csharp
using ChessLogic;

var board = new Board(); // standard starting position

// All legal moves for the piece on e2
MoveData[] moves = board.GetPossibleMoves(Board.AlgebraicPairToCoords("e2"));

// Play moves (as MoveData or algebraic strings)
board.MakeMove("e2e4");
board.MakeMove("e7e5");

// Query game state
bool blackInCheck = board.IsInCheck(whiteKing: false);
bool blackMated   = board.IsInCheckMate(false);
bool stalemate    = board.IsInStalemate(false);

// Take it back
board.UndoMove();

// Clone the game, or rebuild one from a recorded move list
var copy   = new Board(board);
var replay = new Board(recordedMoves); // MoveData[]
```

## API overview

| Type | Purpose |
|------|---------|
| `Board` | The game: piece placement, move generation, move execution, undo, state queries, notation conversion |
| `MoveData` | An immutable move: origin, destination, promotion piece, algebraic form |
| `Piece` / `PieceType` | Lightweight piece representation |
| `InvalidMoveException` / `InvalidPlaceException` | Thrown on illegal moves or invalid coordinates |

## Repository layout

- `ChessLogic/` — the library itself (no external dependencies)
- `ChessTest/` — a console harness that exercises the library, including a simple TCP-based two-player test (EasyTcp4)
