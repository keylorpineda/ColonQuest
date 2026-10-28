using System;

namespace ColonQuest.Game
{
    /// <summary>
    /// The rules of the phone's block game, with no Unity code: a board, falling pieces that move and turn,
    /// full lines that clear and a score. The screen only draws <see cref="Cells"/> and <see cref="PieceCells"/>.
    /// </summary>
    public sealed class TetrisBoard
    {
        public const int Width = 10;
        public const int Height = 18;

        // Each piece as a list of (x, y) cells in a 4 by 4 box; turning is done by rotating these cells.
        private static readonly int[][] Shapes =
        {
            new[] { 0, 1, 1, 1, 2, 1, 3, 1 }, // I
            new[] { 1, 0, 2, 0, 1, 1, 2, 1 }, // O
            new[] { 1, 0, 0, 1, 1, 1, 2, 1 }, // T
            new[] { 1, 0, 2, 0, 0, 1, 1, 1 }, // S
            new[] { 0, 0, 1, 0, 1, 1, 2, 1 }, // Z
            new[] { 0, 0, 0, 1, 1, 1, 2, 1 }, // J
            new[] { 2, 0, 0, 1, 1, 1, 2, 1 }  // L
        };

        private readonly Random random;
        private int[] piece;
        private int kind;
        private int originX;
        private int originY;

        /// <summary>The settled blocks: 0 is empty, 1 to 7 is the kind of piece that left it there.</summary>
        public int[,] Cells { get; } = new int[Width, Height];

        public int Score { get; private set; }
        public int Lines { get; private set; }
        public bool IsOver { get; private set; }

        public TetrisBoard(int seed)
        {
            random = new Random(seed);
            Spawn();
        }

        /// <summary>The kind (1 to 7) of the piece in the air, used to color it.</summary>
        public int PieceKind => kind + 1;

        /// <summary>The four cells of the falling piece on the board.</summary>
        public int[] PieceCells
        {
            get
            {
                var cells = new int[8];
                for (int i = 0; i < 4; i++)
                {
                    cells[i * 2] = originX + piece[i * 2];
                    cells[i * 2 + 1] = originY + piece[i * 2 + 1];
                }

                return cells;
            }
        }

        public bool MoveSideways(int dx)
        {
            return !IsOver && TryMove(dx, 0);
        }

        /// <summary>Turns the piece a quarter clockwise, nudging it off a wall when needed.</summary>
        public bool Rotate()
        {
            if (IsOver)
            {
                return false;
            }

            var turned = new int[8];
            for (int i = 0; i < 4; i++)
            {
                // (x, y) -> (3 - y, x) inside the 4 by 4 box.
                turned[i * 2] = 3 - piece[i * 2 + 1];
                turned[i * 2 + 1] = piece[i * 2];
            }

            foreach (int nudge in new[] { 0, -1, 1, -2, 2 })
            {
                if (Fits(turned, originX + nudge, originY))
                {
                    piece = turned;
                    originX += nudge;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Gravity: one row down. When the piece lands it settles, lines clear and the next one appears.</summary>
        public void Step()
        {
            if (IsOver || TryMove(0, 1))
            {
                return;
            }

            Settle();
        }

        /// <summary>Drops the piece all the way down at once.</summary>
        public void HardDrop()
        {
            while (!IsOver && TryMove(0, 1))
            {
                Score += 2;
            }

            Settle();
        }

        private bool TryMove(int dx, int dy)
        {
            if (!Fits(piece, originX + dx, originY + dy))
            {
                return false;
            }

            originX += dx;
            originY += dy;
            return true;
        }

        private bool Fits(int[] shape, int ox, int oy)
        {
            for (int i = 0; i < 4; i++)
            {
                int x = ox + shape[i * 2];
                int y = oy + shape[i * 2 + 1];
                if (x < 0 || x >= Width || y >= Height || (y >= 0 && Cells[x, y] != 0))
                {
                    return false;
                }
            }

            return true;
        }

        private void Settle()
        {
            for (int i = 0; i < 4; i++)
            {
                int x = originX + piece[i * 2];
                int y = originY + piece[i * 2 + 1];
                if (y >= 0)
                {
                    Cells[x, y] = kind + 1;
                }
            }

            ClearLines();
            Spawn();
        }

        private void ClearLines()
        {
            int cleared = 0;
            for (int y = Height - 1; y >= 0; y--)
            {
                bool full = true;
                for (int x = 0; x < Width; x++)
                {
                    if (Cells[x, y] == 0)
                    {
                        full = false;
                        break;
                    }
                }

                if (!full)
                {
                    continue;
                }

                for (int row = y; row > 0; row--)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Cells[x, row] = Cells[x, row - 1];
                    }
                }

                for (int x = 0; x < Width; x++)
                {
                    Cells[x, 0] = 0;
                }

                cleared++;
                y++; // the row that fell into this one has to be checked again
            }

            if (cleared > 0)
            {
                Lines += cleared;
                Score += new[] { 0, 100, 300, 500, 800 }[Math.Min(cleared, 4)];
            }
        }

        private void Spawn()
        {
            kind = random.Next(Shapes.Length);
            piece = (int[]) Shapes[kind].Clone();
            originX = Width / 2 - 2;
            originY = 0;
            if (!Fits(piece, originX, originY))
            {
                IsOver = true;
            }
        }
    }
}
