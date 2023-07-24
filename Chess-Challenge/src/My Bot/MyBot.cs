using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    ulong[,] pst = {
        {0x8080808080808080, 0x728078777a898f77, 0x767e7e7c81818d7b, 0x757f7e8587828476, 0x7b85828889858777, 0x7e838a8c99968a78, 0xa6b498a59bb18d7c, 0x8080808080808080},
        {0x5778697379757977, 0x756b7b7f80877b79, 0x777c858487878a7a, 0x7b8286858b87887d, 0x7c8787958e9b8789, 0x6e978e99a1b29c91, 0x64709c8e89988379, 0x3f5d736d985a7a56},
        {0x737f7b787b7b7178, 0x8286868083888d80, 0x80868686858b8784, 0x7e85858a8d858482, 0x7e8287948e8e837f, 0x7a8e91908e948e7f, 0x7686797b8c97876e, 0x758260727670837d},
        {0x797b808786837276, 0x6f7a787c80847e64, 0x6e767a7981807e73, 0x72767b80847d8277, 0x777c838a898e7d78, 0x7e878a8e87929886, 0x8b8c97989f9a8a91, 0x8c908c9499848c91},
        {0x80797c847a76746c, 0x727d848183867f80, 0x7b817c7f7e818582, 0x7c767c7c7f7e817f, 0x75757a7a80877f80, 0x7b7983838b969296, 0x77717e807a968b95, 0x75808b8597919192},
        {0x7a8e856b83758985, 0x80837d676f7a8483, 0x7b7b776e6f747a75, 0x6d8075716e6f736c, 0x79787b7574767b72, 0x7c89817a78828977, 0x8b80787d7d7e7175, 0x6789867a6a738185}
    };
    int[] pieceValues = {0, 32, 98, 107, 180, 304, 0};

    int nodes = 0;

    public Move Think(Board board, Timer timer)
    {
        Console.WriteLine("-------------------");
        nodes = 0;
        int depth = 4;
        Move best = BestMove(board, depth, -999999999999, 99999999999, depth % 2 != 0, new Move[depth]).Item1;
        Console.WriteLine("Nodes checked: " + nodes);

        return best;

        // return bestMove;
    }

    long Eval(Board board) {
        long moveScore = 0;
        foreach (PieceList list in board.GetAllPieceLists()) {
            foreach(Piece piece in list) {
                Square sq = piece.Square;
                long score = pieceValues[(int) piece.PieceType];
                score += ((long) pst[(int) piece.PieceType - 1, piece.IsWhite ? sq.Rank : 7 - sq.Rank] >> (56 - 8 * sq.File) & 0xFF) - 128;
                moveScore += score * (piece.IsWhite == board.IsWhiteToMove ? 1 : -1);
            }
        }
        return moveScore;
    }

    (Move, long, Move[]) BestMove(Board board, int depth, long alpha, long beta, bool oddDepth, Move[] line) {
        Move bestMove = new Move();
        long bestScore = -999999999;
        Move[] bestLine = {};

        // Move[] moves = board.GetLegalMoves();

        List<(Move, int)> moves = new List<(Move, int)>();

        foreach (Move move in board.GetLegalMoves()) {
            int score = 0;
            if (move.IsCapture) {
                score += pieceValues[(int) move.CapturePieceType] - pieceValues[(int) move.MovePieceType];
            }
            // if (move.IsPromotion) {
            //     score += pieceValues[(int) move.PromotionPieceType] / 8;
            // }
            // if (move.IsCastles) {
            //     score += 8;
            // }

            moves.Add((move, score));
        }

        // if (depth == 4) Console.WriteLine("moves " + string.Join(", ", moves));

        moves.Sort((a, b) => b.Item2 - a.Item2);
        

        // foreach (var move in moves) {
        foreach (var movedata in moves) {
            nodes++;

            Move move = movedata.Item1;
            board.MakeMove(move);
            Move[] moveLine = (Move[]) line.Clone();
            moveLine[moveLine.Length - depth] = move;

            long score = 0;

            if (board.IsInCheckmate()) {
                board.UndoMove(move);
                return (move, 100000, moveLine);
            } else if (board.IsDraw()) {
                score = 0;
            } else if (depth > 1) {
                var (m, s, l) = BestMove(board, depth - 1, -beta, -alpha, oddDepth, moveLine);
                score = -s;
                moveLine = l;

                if (score == 100000) {
                    board.UndoMove(move);
                    return (move, 100000, moveLine);
                }
            } else {
                score = Eval(board) * (oddDepth ? 1 : -1);
            }

            board.UndoMove(move);

            // if (depth == 4) {
            //     Console.WriteLine("m " + score + " " + string.Join(", ", moveLine));
            // }

            if (score > beta) {
                return (move, score, moveLine);
            }
            
            if (score > bestScore) {
                bestMove = move;
                bestScore = score;
                bestLine = moveLine;
            }

            if (score > alpha) {
                alpha = score;
            }

        }

        return (bestMove, bestScore, bestLine);
    }
}