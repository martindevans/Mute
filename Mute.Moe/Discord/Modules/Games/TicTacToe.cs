using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

using Mute.Moe.Discord.Attributes;
using Mute.Moe.Extensions;

namespace Mute.Moe.Discord.Modules.Games
{
    [HelpGroup("games")]
    [Group("tictactoe")]
    public class TicTacToe
        : BaseModule
    {
        [Command, Summary("Challenge me to a game of tictactoe")]
        public async Task StartGame()
        {
            var rng = new Random();

            //Create new game and play in a random position
            var state = new GameBoard();
            state.Play((byte)rng.Next(3), (byte)rng.Next(3), CellState.X);

            while (true)
            {
                //Show board state
                await ReplyAsync($"```{state}```");

                //Get player turn
                var play = await GetPlay();
                if (play == null)
                    return;

                //Apply player turn
                if (!await ApplyPlay(state, play.Value, CellState.O, "That's not a legal move, I win!", "Game over, you win!", "Game over, it's a draw."))
                {
                    await ReplyAsync($"```{state}```");
                    return;
                }

                //Get AI turn
                var ai = await GetAiPlay(state, rng);

                //Apply AI turn
                if (!await ApplyPlay(state, ai, CellState.X, "Oh no I tried to make an illegal move, you win!", "Game over, I win!", "Game over, it was a draw."))
                {
                    await ReplyAsync($"```{state}```");
                    return;
                }
            }
        }

        private async Task<bool> ApplyPlay( GameBoard board, (byte, byte) position, CellState player, string illegal, string win, string draw)
        {
            var result = board.Play(position.Item1, position.Item2, player);

            //Check if player turn was illegal
            if (!result.HasValue)
            {
                await ReplyAsync(illegal);
                return false;
            }

            //Check if player made a winning move
            if (result.Value == GameState.OWins || result.Value == GameState.XWins)
            {
                await ReplyAsync(win);
                return false;
            }

            if (result.Value == GameState.Draw)
            {
                await ReplyAsync(draw);
                return false;
            }

            return true;
        }

        private static async Task<(byte, byte)> GetAiPlay( GameBoard board,  Random rng)
        {
                var moves = from x in Enumerable.Range(0, 3)
                            from y in Enumerable.Range(0, 3)
                            let cell = board.Cell((byte)x, (byte)y)
                            where cell == CellState.None
                            select ((byte)x, (byte)y);

                return moves.RandomNotNull(rng);
        }

        private async Task<(byte, byte)?> GetPlay()
        {
            while (true)
            {
                var play = await NextMessageAsync(true, true, TimeSpan.FromSeconds(10));
                if (play == null)
                {
                    await ReplyAsync("Too slow!");
                    return null;
                }

                const string plays = "abcdefghi";
                if (play.Content.Length != 1 || !plays.Contains(play.Content[0]))
                {
                    await ReplyAsync("Play must be a single letter from `a` to `i`");
                    continue;
                }

                var index = plays.IndexOf(play.Content[0]);
                return ((byte)(index % 3), (byte)(index / 3));
            }
        }

        private enum CellState
        {
            None,
            X,
            O
        }

        private enum GameState
        {
            InPlay,
            OWins,
            XWins,
            Draw
        }

        private class GameBoard
        {
            private readonly CellState[,] _state = new CellState[3, 3];

            
            public override string ToString()
            {
                string State(int x, int y)
                {
                    var state = _state[x, y];
                    if (state != CellState.None)
                        return state.ToString();

                    return "abcdefghi"[x + y * 3].ToString();
                }

                return $"{State(0, 0)}|{State(1, 0)}|{State(2, 0)}\n"
                     +  "-----\n"
                     + $"{State(0, 1)}|{State(1, 1)}|{State(2, 1)}\n"
                     +  "-----\n"
                     + $"{State(0, 2)}|{State(1, 2)}|{State(2, 2)}\n";
            }

            public CellState Cell(byte x, byte y)
            {
                if (x >= 3) throw new ArgumentOutOfRangeException(nameof(x));
                if (y >= 3) throw new ArgumentOutOfRangeException(nameof(y));
                return _state[x, y];
            }

            public GameState? Play(byte x, byte y, CellState cellState)
            {
                if (x >= 3 || y >= 3)
                    return null;

                if (_state[x, y] != CellState.None)
                    return null;

                _state[x, y] = cellState;

                return State();
            }

            private GameState State()
            {
                static GameState Winner(CellState cell)
                {
                    return cell switch {
                        CellState.X => GameState.XWins,
                        CellState.O => GameState.OWins,
                        CellState.None => throw new ArgumentOutOfRangeException(nameof(cell), cell, null),
                        _ => throw new ArgumentOutOfRangeException(nameof(cell), cell, null)
                    };
                }

                //Check horizontal lines
                for (byte i = 0; i < 3; i++)
                {
                    var exp = Cell(i, 0);
                    if (exp != CellState.None && exp == Cell(i, 1) && exp == Cell(i, 2))
                        return Winner(exp);
                }

                //Check vertical lines
                for (byte i = 0; i < 3; i++)
                {
                    var exp = Cell(0, i);
                    if (exp != CellState.None && exp == Cell(1, i) && exp == Cell(2, i))
                        return Winner(exp);
                }

                //Check diagonal \
                var da = Cell(0, 0);
                if (da != CellState.None && da == Cell(1, 1) && da == Cell(2, 2))
                    return Winner(da);

                //Check diagonal /
                var db = Cell(2, 0);
                if (db != CellState.None && db == Cell(1, 1) && db == Cell(0, 2))
                    return Winner(db);

                //Check if all cells are full
                var count = (from x in Enumerable.Range(0, 3)
                             from y in Enumerable.Range(0, 3)
                             let cell = Cell((byte)x, (byte)y)
                             where cell == CellState.None
                             select cell).Count();

                if (count == 0)
                    return GameState.Draw;

                return GameState.InPlay;
            }
        }
    }
}
