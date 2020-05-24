using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TypeOrDie
{
    class Stats
    {
        public int CorrectCharacters { get; private set; }
        public int WrongCharacters { get; private set; }
        public int Words { get; private set; }
        public int Round { get; private set; }
        public int Wins { get; private set; }
        public int Losses { get; private set; }
        public long ElapsedMilliseconds
        {
            get
            {
                return Timer.ElapsedMilliseconds;
            }
        }
        public float WordsPerMinute
        {
            get
            {
                if (Timer.ElapsedMilliseconds == 0) return 0f;
                return (float)Words / ((Timer.ElapsedMilliseconds / 1000f) / 60f);
            }
        }
        public float CharactersPerSecond
        {
            get
            {
                if (Timer.ElapsedMilliseconds == 0) return 0f;
                return (float)(CorrectCharacters + WrongCharacters) / (Timer.ElapsedMilliseconds / 1000f);
            }
        }

        public bool IsRunning { get { return Timer.IsRunning; } }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementCorrect()
        {
            if (!Timer.IsRunning) Timer.Start();
            CorrectCharacters++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementWords()
        {
            if (!Timer.IsRunning) Timer.Start();
            Words++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementWrong()
        {
            if (!Timer.IsRunning) Timer.Start();
            WrongCharacters++;
        }

        public void Done(bool won)
        {
            Timer.Stop();
            if (won) Wins++;
            else Losses++;
        }

        public void Reset()
        {
            Timer.Stop();
            Round++;
        }

        #region private
        private Stopwatch Timer = new Stopwatch();
        #endregion
    }
}
