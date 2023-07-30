using ChessChallenge.API;
using System;
namespace ChessChallenge.Example;

public class EvilBot : IChessBot
{
    Board testBoard;
    public Move Think(Board board, Timer timer)
    {
        string fen = "r3k2r/1ppb1ppp/2n1p3/1q6/3PN3/2PPp3/PpQ2PPP/R3K2R w KQkq - 0 16";
        testBoard = Board.CreateBoardFromFEN(fen);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Search(5);
        sw.Stop();
        Console.WriteLine(sw.ElapsedMilliseconds + " ms.");
        return board.GetLegalMoves()[0];
    }

    void Search(int depthRemaining)
    {
        if (depthRemaining == 0)
        {
            return;
        }

        Span<Move> moves = stackalloc Move[218];
        testBoard.GetLegalMovesNonAlloc(ref moves);

        foreach (var m in moves)
        {
            testBoard.MakeMove(m);
            Search(depthRemaining - 1);
            testBoard.UndoMove(m);
        }
    }
}