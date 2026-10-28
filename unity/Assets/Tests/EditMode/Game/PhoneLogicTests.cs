using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public class PhoneLogicTests
    {
        [Test]
        public void Glossary_IgnoresAccentsAndCase()
        {
            Assert.AreEqual("Tarjeta de crédito", FinanceGlossary.Search("Cómo pago mi TARJETA de crédito").Title);
            Assert.AreEqual("Tarjeta de crédito", FinanceGlossary.Search("tarjeta credito").Title);
        }

        [Test]
        public void Glossary_FindsTheTopicsTheGameTeaches()
        {
            Assert.AreEqual("Gota a gota", FinanceGlossary.Search("me ofrecen un gota a gota").Title);
            Assert.AreEqual("SINPE Móvil", FinanceGlossary.Search("como funciona sinpe").Title);
            Assert.AreEqual("Invertir en café o ganado", FinanceGlossary.Search("quiero invertir en ganado").Title);
        }

        [Test]
        public void Glossary_ReturnsNothingForNonsense()
        {
            Assert.IsNull(FinanceGlossary.Search("zzzz"));
            Assert.IsNull(FinanceGlossary.Search(string.Empty));
            Assert.IsNull(FinanceGlossary.Search(null));
        }

        [Test]
        public void Tetris_StartsEmptyWithAPieceInTheAir()
        {
            var board = new TetrisBoard(1);
            Assert.IsFalse(board.IsOver);
            Assert.AreEqual(0, board.Score);
            for (int x = 0; x < TetrisBoard.Width; x++)
            {
                for (int y = 0; y < TetrisBoard.Height; y++)
                {
                    Assert.AreEqual(0, board.Cells[x, y]);
                }
            }
        }

        [Test]
        public void Tetris_AHardDropSettlesThePieceOnTheFloor()
        {
            var board = new TetrisBoard(1);
            board.HardDrop();

            int filled = 0;
            for (int x = 0; x < TetrisBoard.Width; x++)
            {
                if (board.Cells[x, TetrisBoard.Height - 1] != 0 || board.Cells[x, TetrisBoard.Height - 2] != 0)
                {
                    filled++;
                }
            }

            Assert.Greater(filled, 0);
        }

        [Test]
        public void Tetris_PiecesStayInsideTheWalls()
        {
            var board = new TetrisBoard(3);
            for (int i = 0; i < 30; i++)
            {
                board.MoveSideways(-1);
            }

            var cells = board.PieceCells;
            for (int c = 0; c < 4; c++)
            {
                Assert.GreaterOrEqual(cells[c * 2], 0);
            }

            for (int i = 0; i < 30; i++)
            {
                board.MoveSideways(1);
            }

            cells = board.PieceCells;
            for (int c = 0; c < 4; c++)
            {
                Assert.Less(cells[c * 2], TetrisBoard.Width);
            }
        }

        [Test]
        public void Tetris_RotatingKeepsFourCellsInsideTheBoard()
        {
            var board = new TetrisBoard(5);
            for (int i = 0; i < 8; i++)
            {
                board.Rotate();
                var cells = board.PieceCells;
                for (int c = 0; c < 4; c++)
                {
                    Assert.GreaterOrEqual(cells[c * 2], 0);
                    Assert.Less(cells[c * 2], TetrisBoard.Width);
                }
            }
        }

        [Test]
        public void Tetris_TheGameEndsWhenThePileReachesTheTop()
        {
            var board = new TetrisBoard(7);
            for (int i = 0; i < 100 && !board.IsOver; i++)
            {
                board.HardDrop();
            }

            Assert.IsTrue(board.IsOver, "dropping pieces without moving them stacks up to the top");
        }
    }
}
