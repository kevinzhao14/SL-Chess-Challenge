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
    ulong[,] pstEnd = {
        {0x8080808080808080, 0x848383838480817e, 0x81827e80807e807d, 0x84837f7e7e7d8180, 0x8b8884827f818686, 0xa0a29d9793929c9d, 0xbdbbb6aeb2adb8c0, 0x8080808080808080},
        {0x766f787b797a6f6a, 0x72797d7e7f797871, 0x787f8085837f7979, 0x7a7e85898586817a, 0x7a8187878784837a, 0x78798383807d7a72, 0x777d777f7d77786e, 0x6c737c7675776b5e},
        {0x787d787e7d7b7e7a, 0x7b7a7e80817d7b77, 0x7c7f838384817e7b, 0x7e81848682837f7d, 0x7f83848385838181, 0x817d80807f828081, 0x7d7f827c7f7c7f7b, 0x7b797c7d7e7d7a78},
        {0x7d8181807e7c8179, 0x7e7e80817d7d7c7f, 0x7f807e807e7c7d7b, 0x818283817e7e7d7c, 0x8181848081808081, 0x82828282817f7e7f, 0x848484847f818381, 0x8483868584848382},
        {0x757679717e757972, 0x7978767b7b787475, 0x7b77858283868382, 0x7a8a86908b8c8d88, 0x8187888f938e938c, 0x79828391908c8683, 0x7a878b8e94898a80, 0x7d87878989868387},
        {0x6e74797c767b7871, 0x777c818485817e7a, 0x7a7f84878885827d, 0x7a7f87888988837c, 0x7d878889898b8981, 0x83868885878f8f84, 0x7c868586868d8884, 0x67747a7a7c85817a}
    };
    int[] pieceValues = {0, 32, 132, 142, 186, 400, 0};
    int[] pieceValuesEnd = {0, 32, 96, 101, 174, 319, 0};

    int nodes = 0;

    // best move, depth, score, type (exact, lower, upper)
    Dictionary<ulong, (Move, int, long, int)> table = new Dictionary<ulong, (Move, int, long, int)>();

    public Move Think(Board board, Timer timer)
    {
        Console.WriteLine("-------------------");
        nodes = 0;
        int depth = 4;
        var best = BestMove(board, depth, -999999999999, 99999999999, depth % 2 != 0, new Move[depth]);
        Console.WriteLine("Nodes checked: " + nodes);
        Console.WriteLine("Line: " + string.Join(", ", best.Item3));

        return best.Item1;
    }

    long Eval(Board board) {
        if (board.IsDraw()) {
            return 0;
        }

        long moveScore = 0;
        foreach (PieceList list in board.GetAllPieceLists()) {
            foreach(Piece piece in list) {
                Square sq = piece.Square;
                long score = pieceValues[(int) piece.PieceType];
                score += ((long) pst[(int) piece.PieceType - 1, piece.IsWhite ? sq.Rank : 7 - sq.Rank] >> (56 - 8 * sq.File) & 0xFF) - 128;
                moveScore += score * (piece.IsWhite == board.IsWhiteToMove ? 1 : -1);
            }
        }

        if (board.IsInCheckmate()) {
            moveScore -= 100000;
        }

        return moveScore;
    }

    (Move, long, Move[]) BestMove(Board board, int depth, long alpha, long beta, bool oddDepth, Move[] line) {
        long origAlpha = alpha;
        Move bestMove = new Move();
        long bestScore = -999999999;
        Move[] bestLine = {};

        (Move, int, long, int) cached;

        if (table.TryGetValue(board.ZobristKey, out cached)) {
            if (cached.Item2 >= depth) {
                line[line.Length - depth] = cached.Item1;
                if (cached.Item4 == 0) {
                    Console.WriteLine("Cache used " + depth + " " + cached.Item2 + " " + cached.Item4);
                    return (cached.Item1, cached.Item3, line);
                } else if (cached.Item4 == 1) {
                    alpha = Math.Max(alpha, cached.Item3);
                } else {
                    beta = Math.Min(beta, cached.Item3);
                }

                if (alpha >= beta) {
                    Console.WriteLine("Cache alpha-beta " + depth + " " + cached.Item2 + " " + cached.Item4);
                    return (cached.Item1, cached.Item3, line);
                }
            } else {
                Console.WriteLine("Move ordering cache " + depth + " " + cached.Item2);
                // bad depth, use for move-ordering
            }
        }

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
            Move[] moveLine = (Move[]) line.Clone();
            moveLine[moveLine.Length - depth] = move;

            long score = 0;
            
            board.MakeMove(move);

            if (depth == 1 || board.IsInCheckmate() || board.IsDraw()) {
                score = -Eval(board);

                // board.UndoMove(move);
                // bestScore = 100000;
                // bestMove = move;
                // bestLine = moveLine;

                // return (move, 100000, moveLine);
            } else {
                var (m, s, l) = BestMove(board, depth - 1, -beta, -alpha, oddDepth, moveLine);
                score = -s;
                moveLine = l;

                // if (score == 100000) {
                //     board.UndoMove(move);
                //     return (move, 100000, moveLine);
                // }
            }

            board.UndoMove(move);

            // if (depth == 4) {
            //     Console.WriteLine("m " + score + " " + string.Join(", ", moveLine));
            // }
            if (score > bestScore) {
                bestMove = move;
                bestScore = score;
                bestLine = moveLine;
            }

            if (score >= beta) {
                break;
                // return (move, score, moveLine);
            }
            

            alpha = Math.Max(alpha, score);
        }

        var entry = (bestMove, depth, bestScore, bestScore <= origAlpha ? 2 : (bestScore >= beta ? 1 : 0));

        if (table.TryGetValue(board.ZobristKey, out cached)) {
            table[board.ZobristKey] = entry;
        } else {
            table.Add(board.ZobristKey, entry);
        }

        return (bestMove, bestScore, bestLine);
    }
}