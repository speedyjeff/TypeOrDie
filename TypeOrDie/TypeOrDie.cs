using engine.Common;
using engine.Winforms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TypeOrDie
{
    public partial class TypeOrDie : Form
    {
        public TypeOrDie()
        {
            InitializeComponent();

            this.Width = 800;
            this.Height = 512;
            this.Text = "Cats Hate Typings Mistakes";
            var icons = engine.Common.Embedded.LoadResource<Icon>(System.Reflection.Assembly.GetExecutingAssembly());
            if (icons.TryGetValue("full", out var icon)) this.Icon = icon;
            // setting a double buffer eliminates the flicker
            this.DoubleBuffered = true;

            // init
            LibraryLock = new ReaderWriterLockSlim();
            Stats = new Stats();
            var books = engine.Winforms.Resources.LoadText(System.Reflection.Assembly.GetExecutingAssembly());
            Library = Library.LoadFromText(books.Values.ToArray());
            Cat = new Cat(width: 75, height: 75);
            Ephemerials = new List<Ephemerial>();

            // creat the new board
            Board = new Board(new BoardConfiguration()
            {
                Width = this.Width,
                Height = this.Height,
                Rows = 1,
                Columns = 1,
                Background = RGBA.White
            });
            Board.OnKeyPressed += Board_OnKeyPressed;
            Board.OnTick += Board_OnTick;
            Board.OnResize += Board_OnResize;

            // link to this control
            UI = new UIHookup(this, Board);

            // initialize
            IsDone = true;
            ReadyToStartNextRound = true;
        }

        #region private
        private Board Board;
        private UIHookup UI;
        private Library Library;
        private Book CurrentBook;
        private Poem CurrentPoem;
        private Line[] Lines;
        private Stats Stats;
        private bool IsDone;
        private int CurrentLine;
        private ReaderWriterLockSlim LibraryLock;
        private List<Ephemerial> Ephemerials;
        private bool ReadyToStartNextRound;

        private Cat Cat;
        private float Penalty;

        private const string TitleFontName = "Arial";
        private const float TitleFontSize = 22f;

        private const string FontName = "Courier New";
        private const float FontSize = 16f;
        private const float FontWidth = 13.25f;

        // visuals
        private const int TextSpacing = 25;
        private const int TextXStart = 10;
        private const int TextYStart = 10;

        class Line
        {
            public string Text;
            public StringBuilder Input;

            public Line(int index, string text)
            {
                // init
                Text = text;
                Input = new StringBuilder();
            }
        }

        class Ephemerial
        {
            public string Text;
            public int Duration;
            public RGBA Color;
        }

        private static Ephemerial TypeToStart = new Ephemerial() { Text = "Start typing when you are ready", Color = RGBA.Black, Duration = 0 };

        class NextRoundEphemerial : Ephemerial { }

        // callbacks
        private void Board_OnKeyPressed(char chr)
        {
            // process the key
            ProcessKeyPressed(chr);

            // update image
            Board.UpdateCell(row: 0, col: 0, ImageUpdate);
        }

        private void Board_OnTick()
        {
            // check if we should start the next round
            if (ReadyToStartNextRound)
            {
                ReadyToStartNextRound = false;
                StartNextRound();
            }

            // advance the cat
            AdvanceCat();

            // draw
            Board.UpdateOverlay(OverlayUpdate);
        }

        private void Board_OnResize()
        {
            Board.UpdateCell(row: 0, col: 0, ImageUpdate);
            Board.UpdateOverlay(OverlayUpdate);
        }

        // paint callbacks
        private void OverlayUpdate(IImage img)
        {
            // clear
            img.Graphics.Clear(RGBA.White);
            img.MakeTransparent(RGBA.White);

            // draw the cat
            if (Stats.IsRunning) Cat.Draw(img);

            // draw any ephemerial elements
            lock(Ephemerials)
            {
                Ephemerial active = null;
                if (Ephemerials.Count > 0)
                {
                    active = Ephemerials[0];
                    active.Duration--;
                    // check if it should be removed
                    if (active.Duration <= 0)
                    {
                        Ephemerials.RemoveAt(0);
                        if (active is NextRoundEphemerial) ReadyToStartNextRound = true;
                    }
                }
                else
                {
                    // show a message to start typing
                    if (!IsDone && !Stats.IsRunning) active = TypeToStart;
                }

                if (active != null)
                {
                    img.Graphics.Text(active.Color, x: Board.Width - 400, y: Board.Height - 200, active.Text, fontsize: 12f);
                    if (active is NextRoundEphemerial)
                    {
                        img.Graphics.Text(RGBA.Black, x: Board.Width - 400, y: Board.Height - 200 + TextSpacing, string.Format($"Next round starts in {(active.Duration / 10) + 1}"), fontsize: 12f);
                    }
                }

            }
        }

        private void ImageUpdate(IImage img)
        {
            try
            {
                LibraryLock.EnterReadLock();

                img.Graphics.Clear(RGBA.White);

                // draw the title (relative to the typing text
                var titleX = TextXStart;
                var titleY = TextYStart;
                img.Graphics.Rectangle(new RGBA() { R = 200, G = 200, B = 200, A = 255 }, titleX, titleY, width: Board.Width - (TextXStart * 2) - 20f, height: 90, fill: true, border: false);
                img.Graphics.Text(RGBA.Black, titleX, titleY + (TextSpacing * 0f), string.Format($"{CurrentBook.Title} by {CurrentBook.Author}"), TitleFontSize, TitleFontName);
                img.Graphics.Text(RGBA.Black, titleX, titleY + (TextSpacing * 1.2f), CurrentPoem.Section, TitleFontSize, TitleFontName);
                img.Graphics.Text(RGBA.Black, titleX, titleY + (TextSpacing * 2.4f), CurrentPoem.Title, TitleFontSize, TitleFontName);

                // draw the stats (based on the window height
                var statsX = TextXStart;
                var statsY = Board.Height - 150f;
                img.Graphics.Rectangle(RGBA.Black, statsX, statsY, width: Board.Width - (TextXStart * 2) - 20f, height: 100, fill: true, border: true, thickness: 1f);
                img.Graphics.Text(RGBA.White, statsX + 10f, statsY + (TextSpacing * 0.35f), string.Format($"Round {Stats.Round}"), TitleFontSize, TitleFontName);
                img.Graphics.Text(RGBA.White, statsX + 10f, statsY + (TextSpacing * 2f), string.Format($"Words per minute {Stats.WordsPerMinute:f2} :: Wins {Stats.Wins} :: Losses {Stats.Losses}"), FontSize, FontName);
                img.Graphics.Text(RGBA.White, statsX + 10f, statsY + (TextSpacing * 3f), string.Format($"Correct characters {Stats.CorrectCharacters} :: Wrong characters {Stats.WrongCharacters}"), FontSize, FontName);

                // add the cursor
                if (CurrentLine < Lines.Length)
                {
                    var cursor = "_";
                    var x = TextXStart + (Lines[CurrentLine].Input.Length * FontWidth);
                    img.Graphics.Text(new RGBA() { R = 255, A = 255 }, x, TextYStart + (TextSpacing * (CurrentLine + 5)), cursor, FontSize, FontName);
                }

                // draw all the strings on the board
                for (int i = 0; i < Lines.Length; i++)
                {
                    img.Graphics.Text(RGBA.Black, TextXStart, TextYStart + (TextSpacing * (i + 5)), Lines[i].Text, FontSize, FontName);
                    if (Lines[i].Input.Length > 0)
                    {
                        img.Graphics.Text(RGBA.White, TextXStart, TextYStart + (TextSpacing * (i + 5)), Lines[i].Input.ToString(), FontSize, FontName);
                    }
                }
            }
            finally
            {
                LibraryLock.ExitReadLock();
            }
        }

        // game logic
        private void StartNextRound()
        {
            if (IsDone)
            {
                try
                {
                    LibraryLock.EnterWriteLock();

                    // reset the stats
                    Stats.Reset();
                    lock(Ephemerials)
                    {
                        Ephemerials.Clear();
                    }

                    // prepare a new poem
                    CurrentLine = 0;
                    CurrentBook = Library.Randomize();
                    CurrentPoem = Library.Randomize(CurrentBook);
                    var stanza = Library.Randomize(CurrentPoem, maxChars: 100, maxCharsPerLine: 20);
                    Lines = new Line[stanza.Count];
                    // construct the lines
                    var lengths = new int[stanza.Count];
                    for (int i = 0; i < Lines.Length; i++)
                    {
                        Lines[i] = new Line(i, stanza[i]);
                        lengths[i] = Lines[i].Text.Length;
                    }

                    // reset
                    Cat.Reset(lengths, xStart: TextXStart, yStart: TextYStart + (TextSpacing * 5) - (Cat.Height * 0.75f), xDelta: FontWidth, yDelta: TextSpacing);
                    IsDone = false;
                }
                finally
                {
                    LibraryLock.ExitWriteLock();
                }

                // start the game
                Board.UpdateCell(row: 0, col: 0, ImageUpdate);
                Board.UpdateOverlay(OverlayUpdate);
            }
        }

        private void ProcessKeyPressed(char chr)
        {
            if (chr == Constants.LeftMouse || chr == Constants.RightMouse) return;

            try
            {
                LibraryLock.EnterReadLock();

                bool displayWrongChar = false;

                // no more typing on this line when done
                if (Lines == null || Lines.Length == 0 || IsDone) return;

                // check if this is a line end
                if (Lines[CurrentLine].Text.Length == Lines[CurrentLine].Input.Length)
                {
                    if (chr == '\r' || chr == '\n' || chr == ' ')
                    {
                        // at the end of the line
                        CurrentLine++;
                        Stats.IncrementWords();
                    }
                    else
                    {
                        // incorrect
                        Stats.IncrementWrong();
                        displayWrongChar = true;
                    }
                }

                // check that this letter matches
                else if (Lines[CurrentLine].Text.Length <= Lines[CurrentLine].Input.Length ||
                    Lines[CurrentLine].Text[Lines[CurrentLine].Input.Length] != chr)
                {
                    Stats.IncrementWrong();
                    displayWrongChar = true;
                }

                // add the letter
                else
                {
                    Lines[CurrentLine].Input.Append(chr);
                    Stats.IncrementCorrect();
                    if (chr == ' ') Stats.IncrementWords();
                }

                if (displayWrongChar)
                {
                    // add a penalty to the cat
                    Penalty += 3f;

                    // this is not the right character
                    lock (Ephemerials)
                    {
                        Ephemerials.Add(
                            new Ephemerial()
                            {
                                Text = string.Format($"'{chr}' wrong character"),
                                Color = new RGBA() { R = 255, A = 255 },
                                Duration = 10
                            });
                    }
                }

                // check if we are done
                if (CurrentLine >= Lines.Length)
                {
                    IsDone = true;
                    Stats.Done(won: true);

                    lock (Ephemerials)
                    {
                        // clear the queue
                        Ephemerials.Clear();
                        // add the winning message
                        Ephemerials.Add(
                            new NextRoundEphemerial()
                            {
                                Text = string.Format($"You won this round!"),
                                Color = new RGBA() { G = 255, A = 255 },
                                Duration = 30
                            });
                    }
                }
            }
            finally
            {
                LibraryLock.ExitReadLock();
            }
        }

        private void AdvanceCat()
        {
            try
            {
                LibraryLock.EnterReadLock();

                // exit early if at the end
                if (Lines == null || Lines.Length == 0 || IsDone || !Stats.IsRunning) return;

                // advance the cat
                // calculate the characters per tick
                var cpt = Stats.CharactersPerSecond / (1000f / Constants.GlobalClock);
                // trim outliers
                if (Single.IsInfinity(cpt)
                    || (cpt > 1f && Stats.ElapsedMilliseconds < 10000)) cpt = 0f;
                // for the first 10 levels, apply a speed limiter
                if (Stats.Round < 10)
                {
                    // 70% reduction to 95%
                    var reduction = 0.7f + (0.25f * (float)(1f - Stats.Round) / 10f);
                    cpt *= reduction;
                }
                // advance
                Cat.Advance(cpt + Penalty);
                Penalty = 0f;
                // check if the cat should be angry (if behind)
                Cat.IsAngry = (Cat.Row == CurrentLine && Cat.Column < Lines[CurrentLine].Input.Length) || (Cat.Row < CurrentLine);
            }
            finally
            {
                LibraryLock.ExitReadLock();
            }

            // check if we are at the end
            if (Cat.Row >= Lines.Length)
            {
                IsDone = true;
                Stats.Done(won: false);

                lock (Ephemerials)
                {
                    // clear the queue
                    Ephemerials.Clear();
                    // add the lossing message
                    Ephemerials.Add(
                        new NextRoundEphemerial()
                        {
                            Text = string.Format($"You lost this round!"),
                            Color = new RGBA() { R = 255, A = 255 },
                            Duration = 30
                        });
                }

                // update image
                Board.UpdateCell(row: 0, col: 0, ImageUpdate);
            }
        }
        #endregion
    }
}
