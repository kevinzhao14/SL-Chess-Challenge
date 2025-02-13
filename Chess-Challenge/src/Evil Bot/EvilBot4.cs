using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class EvilBot : IChessBot
{
    private ulong[,] pst = {
        {0x8080808080808080, 0x728078777a898f77, 0x767e7e7c81818d7b, 0x757f7e8587828476, 0x7b85828889858777, 0x7e838a8c99968a78, 0xa6b498a59bb18d7c, 0x8080808080808080},
        {0x5778697379757977, 0x756b7b7f80877b79, 0x777c858487878a7a, 0x7b8286858b87887d, 0x7c8787958e9b8789, 0x6e978e99a1b29c91, 0x64709c8e89988379, 0x3f5d736d985a7a56},
        {0x737f7b787b7b7178, 0x8286868083888d80, 0x80868686858b8784, 0x7e85858a8d858482, 0x7e8287948e8e837f, 0x7a8e91908e948e7f, 0x7686797b8c97876e, 0x758260727670837d},
        {0x797b808786837276, 0x6f7a787c80847e64, 0x6e767a7981807e73, 0x72767b80847d8277, 0x777c838a898e7d78, 0x7e878a8e87929886, 0x8b8c97989f9a8a91, 0x8c908c9499848c91},
        {0x80797c847a76746c, 0x727d848183867f80, 0x7b817c7f7e818582, 0x7c767c7c7f7e817f, 0x75757a7a80877f80, 0x7b7983838b969296, 0x77717e807a968b95, 0x75808b8597919192},
        {0x7a8e856b83758985, 0x80837d676f7a8483, 0x7b7b776e6f747a75, 0x6d8075716e6f736c, 0x79787b7574767b72, 0x7c89817a78828977, 0x8b80787d7d7e7175, 0x6789867a6a738185}
    };
    // ulong[,] pstEnd = {
    //     {0x8080808080808080, 0x848383838480817e, 0x81827e80807e807d, 0x84837f7e7e7d8180, 0x8b8884827f818686, 0xa0a29d9793929c9d, 0xbdbbb6aeb2adb8c0, 0x8080808080808080},
    //     {0x766f787b797a6f6a, 0x72797d7e7f797871, 0x787f8085837f7979, 0x7a7e85898586817a, 0x7a8187878784837a, 0x78798383807d7a72, 0x777d777f7d77786e, 0x6c737c7675776b5e},
    //     {0x787d787e7d7b7e7a, 0x7b7a7e80817d7b77, 0x7c7f838384817e7b, 0x7e81848682837f7d, 0x7f83848385838181, 0x817d80807f828081, 0x7d7f827c7f7c7f7b, 0x7b797c7d7e7d7a78},
    //     {0x7d8181807e7c8179, 0x7e7e80817d7d7c7f, 0x7f807e807e7c7d7b, 0x818283817e7e7d7c, 0x8181848081808081, 0x82828282817f7e7f, 0x848484847f818381, 0x8483868584848382},
    //     {0x757679717e757972, 0x7978767b7b787475, 0x7b77858283868382, 0x7a8a86908b8c8d88, 0x8187888f938e938c, 0x79828391908c8683, 0x7a878b8e94898a80, 0x7d87878989868387},
    //     {0x6e74797c767b7871, 0x777c818485817e7a, 0x7a7f84878885827d, 0x7a7f87888988837c, 0x7d878889898b8981, 0x83868885878f8f84, 0x7c868586868d8884, 0x67747a7a7c85817a}
    // };
    private int[] pieceValues = {0, 32, 132, 142, 186, 400, 0}, killerIndices;
    // int[] pieceValuesEnd = {0, 32, 96, 101, 174, 319, 0};

    // USED VARIABLES
    private bool timeUp;
    private int timeAlloc, nodes, searchDepth;
    private Timer time;
    // private int nodes;

    // DIAGNOSTIC/DEBUG VARIABLES
    // int leafNodes = 0;
    // int eval = 0;

    int totalNodes = 0; // #DEBUG
    int totalTime = 0; // #DEBUG
    // int totalLeafNodes = 0;


    // Transposition table
    // best move, depth, score, type (exact, lower, upper)
    // private Dictionary<ulong, (Move, int, long, byte, bool)> table = new();
    private const ulong tMask = 0x7FFFFF;
    private (Move, int, long, byte, bool, ulong)[] table = new (Move, int, long, byte, bool, ulong)[tMask + 1];

    // Killer moves
    private Move[,] killers;
    // private int[] killerIndices;

    public Move Think(Board board, Timer timer) {
        Console.WriteLine("\nEvil Bot############"); // #DEBUG

        // leafNodes = 0;
        // eval = 0;

        // nodes = 0;
        // timeUp = false;
        // time = timer;


        // killers = new Move[32, 2];
        // killerIndices = new int[32];

        int plies = board.PlyCount / 2 - 55;
        
        // adjust time spent/predicted moves remaining based on board evaluation - should spend
        // more time when losing/winning by a good amount to improve/not throw
        // double skillCheck = Math.Max(0.5, plies <= 10 ? Math.Abs(Eval(board, 0)) / -512.0 + 1 : 1);

        (
            nodes, 
            timeUp, 
            time, 
            killers, 
            killerIndices,
            timeAlloc
        ) = (
            0, 
            false, 
            timer, 
            new Move[64, 2], 
            new int[64],
            (int) (timer.MillisecondsRemaining 
                / (40 + plies * plies / (plies <= 0 ? 32 : 256)) 
                / Math.Max(0.25, plies <= 5 ? Math.Abs(Eval(board, 0)) / -256.0 + 1 : 1) 
                * 0.9)
        );

        Console.WriteLine("Skillcheck " + Eval(board, 0) + " " + Math.Max(0.25, plies <= 5 ? Math.Abs(Eval(board, 0)) / -256.0 + 1 : 1)); // #DEBUG

        // INFO: int movesLeft = 40 + plies * plies / (plies <= 0 ? 24 : 128);
        // timeAlloc = (int) (timer.MillisecondsRemaining / (40 + plies * plies / (plies <= 0 ? 24 : 128)) / skillCheck * 0.9);

        // Console.WriteLine("skillcheck " + skillCheck + " " + plies + " " + movesLeft + " " + timeAlloc + " " + timer.MillisecondsRemaining);

        // (Move, long, string[]) best = BestMove(board, 1, new string[1]);
        // var best = BestMove(board, 1);  // (Move, long) - bestMove, evalScore
        (Move, long) best = new(); // 1 less token than ^

        for (searchDepth = 2; searchDepth <= 32; searchDepth += 2) {
            Console.WriteLine("Running depth " + searchDepth); // #DEBUG

            // var result = BestMove(board, i, new string[i]);
            var result = BestMove(board, searchDepth); // (Move, long) - bestMove, evalScore

            if (!result.Item1.Equals(Move.NullMove)) {
                best = result;

                // finish on checkmate
                if (result.Item2 >= 100000)
                // if (result.Item2 >= 100000) {
                    // Console.WriteLine("Checkmate found");
                    break;
                // }
            } 

            else { // #DEBUG
                Console.WriteLine("Cancelled"); // #DEBUG
            } // #DEBUG

            // Console.WriteLine("- Best: " + best.Item2 + " " + string.Join(", ", best.Item3));
            Console.WriteLine("- Best: " + best.Item2 + " " + best.Item1); // #DEBUG

            if (timeUp) 
                break;
        }

        // Console.WriteLine("\nStats:");
        Console.WriteLine("Time: " + timeAlloc + " " + timer.MillisecondsElapsedThisTurn); // #DEBUG
        // Console.WriteLine("Nodes checked: " + nodes + " " + leafNodes + " " + eval);
        Console.WriteLine("Nodes checked: " + nodes); // #DEBUG
        // Console.WriteLine("TABLE: " + table.Count); // #DEBUG
        // Console.WriteLine("(" + best.Item2 / 32.0 + ") Line: " + string.Join(", ", best.Item3));
        Console.WriteLine("(" + best.Item2 / 32.0 + ") Line: " + best.Item1); // #DEBUG

        totalNodes += nodes; // #DEBUG
        totalTime += timer.MillisecondsElapsedThisTurn; // #DEBUG
        // totalLeafNodes += leafNodes;
        // Console.WriteLine("BF/NPS: " + (nodes - 1.0) / (nodes - leafNodes) + " " + (nodes / 1.0 / timer.MillisecondsElapsedThisTurn * 1000));
        // Console.WriteLine("Total BF/NPS: " + (totalNodes - 1.0) / (totalNodes - totalLeafNodes) + " " + (totalNodes / 1.0 / totalTime * 1000));
        Console.WriteLine("NPS: " + (nodes / 1.0 / timer.MillisecondsElapsedThisTurn * 1000)); // #DEBUG
        Console.WriteLine("Total NPS: " + (totalNodes / 1.0 / totalTime * 1000)); // #DEBUG

        return best.Item1;
    }

    private long Eval(Board board, int depth) {
        // eval++;

        if (board.IsDraw())
            return depth % 2 == 0 ? -64 : 64;

        if (board.IsInCheckmate())
            return -100000 - depth;
        

        long moveScore = 0;
        foreach (PieceList list in board.GetAllPieceLists()) {
            long score = list.Count * pieceValues[(int) list.TypeOfPieceInList];

            // PST position score
            foreach (Piece piece in list) {
                Square sq = piece.Square;
                score += ((long) pst[(int) piece.PieceType - 1, piece.IsWhite ? sq.Rank : 7 - sq.Rank] >> 56 - 8 * sq.File & 0xFF) - 128;
            }
            
            moveScore += score * (list.IsWhitePieceList == board.IsWhiteToMove ? 1 : -1);
        }

        return moveScore;
    }

    // (Move, long, string[]) BestMove(Board board, int depth, string[] line, long alpha=-999999999999, long beta=99999999999) {
    private (Move, long) BestMove(Board board, int depth, long alpha=-999999999, long beta=999999999) {
        nodes++;

        if ((nodes & 4095) == 0 && time.MillisecondsElapsedThisTurn >= timeAlloc) 
            // Console.WriteLine("Interrupt " + timeAlloc + " " + time.MillisecondsElapsedThisTurn);
            timeUp = true;
        
        if (timeUp) 
            // return (new Move(), 0, line);
            return new();

        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
            // leafNodes++;

            // return (Move.NullMove, Eval(board, depth), line);
            return (new(), Eval(board, depth));

        // long origAlpha = alpha;
        // bool inCheck = board.IsInCheck();

        // List<(Move, int)> moves = new();

        // var key = board.ZobristKey;

        var (
            origAlpha, 
            key, 
            bestMove, 
            bestScore,
            moves
        ) = (
            alpha, 
            board.ZobristKey, 
            Move.NullMove, 
            -999999999L,
            new List<(Move, int)>()
        );

        var cached = table[key & tMask];

        if (cached.Item6 == key) {
            if (cached.Item2 >= depth) {
                if (cached.Item4 == 0)
                    // line[line.Length - depth] = cached.Item1.ToString() + "_c0";
                    // leafNodes++;

                    // return (cached.Item1, cached.Item3, line);

                    if (!cached.Item5) {
                        return (cached.Item1, cached.Item3);
                    }
                else if (cached.Item4 == 1)
                    alpha = Math.Max(alpha, cached.Item3);
                else
                    beta = Math.Min(beta, cached.Item3);

                if (alpha >= beta)
                    // line[line.Length - depth] = cached.Item1.ToString() + "_c1";
                    // leafNodes++;

                    // return (cached.Item1, cached.Item3, line);
                    return (cached.Item1, cached.Item3);
                else
                    cached.Item1 = Move.NullMove;
                
            } else
                // bad depth, use for move-ordering
                moves.Add((cached.Item1, 100000));
            
        } else if (depth >= 4) {
            // internal iterative deepening
            // cached.Item1 = BestMove(board, 2, new string[2]).Item1;
            cached.Item1 = BestMove(board, 2).Item1;

            moves.Add((cached.Item1, 100000));
        }

        // if (board.GetLegalMoves().Length == 0) {
        //     Console.WriteLine("\n######### ERROR: no valid moves\n");
        // }

        // Span<Move> legalMoves = stackalloc Move[218];
        // board.GetLegalMovesNonAlloc(ref legalMoves);

        foreach (Move move in board.GetLegalMoves()) {
        // foreach (Move move in legalMoves) {
            if (cached.Item1.Equals(move))
                continue;
            
            if (move.Equals(killers[depth, 0]) || move.Equals(killers[depth, 1])) {
                moves.Add((move, 400));
                continue;
            }

            int score = 0;
            if (move.IsCapture) 
                score += pieceValues[(int) move.CapturePieceType] * 4 - pieceValues[(int) move.MovePieceType];
            
            if (move.IsPromotion)
                score += pieceValues[(int) move.PromotionPieceType] - 32;
            
            
            moves.Add((move, score));
        }

        moves.Sort((a, b) => b.Item2 - a.Item2);

        // Move bestMove = new();
        // long bestScore = -999999999;

        // string[] bestLine = {"empty"};

        // if (moves.Count == 0) {
        //     Console.WriteLine("\nNo moves " + string.Join(", ", line) + " " + board.GetLegalMoves().Length + " " + depth + " " + board.IsInCheckmate() + "\n");
        // }

        // foreach (var move in moves) {
        foreach (var movedata in moves) {
            Move move = movedata.Item1;

            // string[] moveLine = (string[]) line.Clone();
            // moveLine[moveLine.Length - depth] = move.ToString();

            board.MakeMove(move);

            // late move reduction
            // if (!(depth < 3 || movedata.Item2 >= 400 || move.IsCapture || move.IsPromotion || inCheck || board.IsInCheck())) {
            //     depthReduction = 2;
            // }

            // var (m, s, l) = BestMove(board, depth - depthReduction, moveLine, -beta, -alpha);
            long score = -BestMove(board, depth - 1, -beta, -alpha).Item2;

            board.UndoMove(move);

            if (timeUp)
                // return (bestMove, bestScore, bestLine);
                return (bestMove, bestScore);

            // if (depthReduction == 2 && -s > alpha) {
            //     (m, s, l) = BestMove(board, depth - 1, moveLine, -beta, -alpha);
            // }

            // long score = -s;
            // moveLine = l;

            if (score > bestScore) {
                bestMove = move;
                bestScore = score;
                // bestLine = moveLine;
            }

            alpha = Math.Max(alpha, score);

            if (alpha >= beta) {
                if (!killers[depth, 0].Equals(move) && !killers[depth, 1].Equals(move)) {
                    killers[depth, killerIndices[depth]++] = move;
                    killerIndices[depth] %= 2;
                }
                break;
            }
        }

        var entry = (bestMove, depth, bestScore, (byte) (bestScore <= origAlpha ? 2 : (bestScore >= beta ? 1 : 0)), depth == searchDepth, key);
        table[key & tMask] = entry;

        // return (bestMove, bestScore, bestLine);
        return (bestMove, bestScore);
    }
}