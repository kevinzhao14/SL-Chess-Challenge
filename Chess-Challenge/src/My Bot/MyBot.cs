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
    int leafNodes = 0;
    int eval = 0;
    bool timeUp = false;
    int timeAlloc;
    Timer time;

    int totalNodes = 0;
    int totalLeafNodes = 0;
    int totalTime = 0;

    int searchDepth = 0;

    // best move, depth, score, type (exact, lower, upper)
    Dictionary<ulong, (Move, int, long, int)> table = new Dictionary<ulong, (Move, int, long, int)>();

    Move[,] killers;
    int[] killerIndices;

    public Move Think(Board board, Timer timer) {
        Console.WriteLine("-------------------" + board.GetFenString());

        nodes = 0;
        leafNodes = 0;
        eval = 0;
        timeUp = false;
        time = timer;

        // int depth = 6;
        killers = new Move[32, 2];
        killerIndices = new int[32];

        // int movesLeft = (int) Math.Round(board.PlyCount < 90 ? -board.PlyCount / 2 + 50 : 0.05 * board.PlyCount);
        int plies = board.PlyCount / 2;
        int movesLeft = plies <= 40 ? -plies + 50 : (5 + (int) (Math.Pow(plies - 60, 2) / (plies <= 60 ? 80 : 512)));

        double skillCheck = plies <= 60 ? -((int) Math.Abs(Eval(board, 0)) / 512.0) + 1.1 : 1;

        if (skillCheck < 0.5) {
            skillCheck = 0.5;
        }

        timeAlloc = (int) (timer.MillisecondsRemaining / movesLeft / skillCheck * 0.9);
        Console.WriteLine("skillcheck " + skillCheck + " " + plies + " " + movesLeft + " " + timeAlloc + " " + timer.MillisecondsRemaining);

        (Move, long, string[]) best = BestMove(board, 1, new string[]{"0"});

        for (int i = 2; i <= 32; i += 2) {
            // if (timer.MillisecondsElapsedThisTurn >= timeAlloc) {
            //     Console.WriteLine("Out of time " + timer.MillisecondsElapsedThisTurn + " " + timeAlloc);
            //     break;
            // }
            Console.WriteLine("Running depth " + i);
            searchDepth = i;
            var result = BestMove(board, i, new string[i]);

            if (!result.Item1.Equals(Move.NullMove)) {
                best = result;

                // finish on checkmate
                if (result.Item2 >= 100000) {
                    Console.WriteLine("Checkmate found");
                    break;
                }
            } else {
                Console.WriteLine("Cancelled");
            }

            Console.WriteLine("- Best: " + best.Item2 + " " + string.Join(", ", best.Item3));

            if (timeUp) {
                break;
            }
        }

        // var best = BestMove(board, depth, -999999999999, 99999999999, new Move[depth]);

        Console.WriteLine("\nStats:");
        Console.WriteLine("Time: " + timeAlloc + " " + timer.MillisecondsElapsedThisTurn);
        Console.WriteLine("Nodes checked: " + nodes + " " + leafNodes + " " + eval);
        Console.WriteLine("TABLE: " + table.Count);
        Console.WriteLine("(" + best.Item2 / 32.0 + ") Line: " + string.Join(", ", best.Item3));

        totalNodes += nodes;
        totalLeafNodes += leafNodes;
        totalTime += timer.MillisecondsElapsedThisTurn;
        Console.WriteLine("BF/NPS: " + (nodes - 1.0) / (nodes - leafNodes) + " " + (nodes / 1.0 / timer.MillisecondsElapsedThisTurn * 1000));
        Console.WriteLine("Total BF/NPS: " + (totalNodes - 1.0) / (totalNodes - totalLeafNodes) + " " + (totalNodes / 1.0 / totalTime * 1000));

        return best.Item1;
    }

    long Eval(Board board, int depth) {
        eval++;
        if (board.IsDraw()) {
            return 0;
        }
        if (board.IsInCheckmate()) {
            return -100000 - depth;
        }

        long moveScore = 0;
        foreach (PieceList list in board.GetAllPieceLists()) {
            long score = list.Count * pieceValues[(int) list.TypeOfPieceInList];

            // PST position score
            foreach(Piece piece in list) {
                Square sq = piece.Square;
                score += ((long) pst[(int) piece.PieceType - 1, piece.IsWhite ? sq.Rank : 7 - sq.Rank] >> (56 - 8 * sq.File) & 0xFF) - 128;
            }
            
            moveScore += score * (list.IsWhitePieceList == board.IsWhiteToMove ? 1 : -1);
        }

        return moveScore;
    }

    (Move, long, string[]) BestMove(Board board, int depth, string[] line, long alpha=-999999999999, long beta=99999999999) {
        nodes++;

        if ((nodes & 4095) == 0 && time.MillisecondsElapsedThisTurn >= timeAlloc) {
            Console.WriteLine("Interrupt " + timeAlloc + " " + time.MillisecondsElapsedThisTurn);
            timeUp = true;
        }
        
        if (timeUp) {
            return (new Move(), 0, line);
        }

        if (depth == 0 || board.IsInCheckmate() || board.IsDraw()) {
            leafNodes++;
            return (Move.NullMove, Eval(board, depth), line);
        }

        long origAlpha = alpha;
        bool inCheck = board.IsInCheck();

        (Move, int, long, int) cached;
        List<(Move, int)> moves = new List<(Move, int)>();

        if (table.TryGetValue(board.ZobristKey, out cached)) {
            if (cached.Item2 >= depth) {
                if (cached.Item4 == 0) {
                    line[line.Length - depth] = cached.Item1.ToString() + "_c0";
                    // Console.WriteLine("Cache used " + depth + " " + cached.Item2 + " " + cached.Item4);
                    leafNodes++;
                    return (cached.Item1, cached.Item3, line);
                } else if (cached.Item4 == 1) {
                    alpha = Math.Max(alpha, cached.Item3);
                } else {
                    beta = Math.Min(beta, cached.Item3);
                }

                if (alpha >= beta) {
                    line[line.Length - depth] = cached.Item1.ToString() + "_c1";
                    leafNodes++;
                    // Console.WriteLine("Cache alpha-beta " + depth + " " + cached.Item2 + " " + cached.Item4);
                    return (cached.Item1, cached.Item3, line);
                } else {
                    cached.Item1 = Move.NullMove;
                }
            } else {
                // Console.WriteLine("Move ordering cache " + depth + " " + cached.Item2);
                // bad depth, use for move-ordering
                moves.Add((cached.Item1, 100000));
            }
        } else if (depth >= 4) {
            // internal iterative deepening
            cached.Item1 = BestMove(board, 2, new string[2]).Item1;
            moves.Add((cached.Item1, 100000));
        }

        if (board.GetLegalMoves().Length == 0) {
            Console.WriteLine("\n######### ERROR: no valid moves\n");
        }

        foreach (Move move in board.GetLegalMoves()) {
            if (!cached.Equals(default) && cached.Item1.Equals(move)) {
                continue;
            }
            if (move.Equals(killers[depth, 0]) || move.Equals(killers[depth, 1])) {
                moves.Add((move, 400));
                continue;
            }
            int score = 0;
            if (move.IsCapture) {
                score += pieceValues[(int) move.CapturePieceType] * 4 - pieceValues[(int) move.MovePieceType];
            }
            if (move.IsPromotion) {
                score += pieceValues[(int) move.PromotionPieceType] - 32;
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

        Move bestMove = new Move();
        long bestScore = -999999999;
        string[] bestLine = {"empty"};

        if (moves.Count == 0) {
            Console.WriteLine("\nNo moves " + string.Join(", ", line) + " " + board.GetLegalMoves().Length + " " + depth + " " + board.IsInCheckmate() + "\n");
        }

        // foreach (var move in moves) {
        foreach (var movedata in moves) {
            Move move = movedata.Item1;
            string[] moveLine = (string[]) line.Clone();
            moveLine[moveLine.Length - depth] = move.ToString();

            long score = 0;
            
            board.MakeMove(move);

            int depthReduction = 1;

            // late move reduction
            // if (!(depth < 3 || movedata.Item2 >= 400 || move.IsCapture || move.IsPromotion || inCheck || board.IsInCheck())) {
            //     depthReduction = 2;
            // }


            // board.UndoMove(move);
            // bestScore = 100000;
            // bestMove = move;
            // bestLine = moveLine;

            // return (move, 100000, moveLine);
            var (m, s, l) = BestMove(board, depth - depthReduction, moveLine, -beta, -alpha);

            board.UndoMove(move);

            if (timeUp) {
                return (bestMove, bestScore, bestLine);
            }

            // if (depthReduction == 2 && -s > alpha) {
            //     (m, s, l) = BestMove(board, depth - 1, moveLine, -beta, -alpha);
            // }

            score = -s;
            moveLine = l;

            // if (score == 100000) {
            //     board.UndoMove(move);
            //     return (move, 100000, moveLine);
            // }
            


            // if (depth == 4) {
            //     Console.WriteLine("m " + score + " " + string.Join(", ", moveLine));
            // }
            if (score > bestScore) {
                bestMove = move;
                bestScore = score;
                bestLine = moveLine;
            }

            alpha = Math.Max(alpha, score);

            if (alpha >= beta) {
                if (!killers[depth, 0].Equals(move) && !killers[depth, 1].Equals(move)) {
                    killers[depth, killerIndices[depth]++] = move;
                    killerIndices[depth] %= 2;
                }
                break;
                // return (move, score, moveLine);
            }
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