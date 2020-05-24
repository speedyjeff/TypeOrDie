using engine.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeOrDie
{
    class IndexRotation
    {
        public IndexRotation(int min, int max)
        {
            Min = min;
            Max = max;
            Reset();
        }

        public int Index { get; private set; }
        public int Increment()
        {
            Index += Inc;

            // cap to [min,max]
            if (Index < Min)
            {
                Index = Min;
                Inc = 1;
            }
            else if (Index > Max)
            {
                Index = Max;
                Inc = -1;
            }

            return Index;
        }
        public void Reset()
        {
            Inc = 1;
            Index = -1;
        }

        #region private
        private int Inc = 0;
        private int Min = 0;
        private int Max = 0;
        #endregion
    }

    class Cat
    {
        public Cat(int width, int height)
        {
            // setup indexes for the images
            TailIndex = new IndexRotation(min: 0, max: TailImages.Length - 1);
            FaceIndex = new IndexRotation(min: 0, max: FaceImages.Length - 1);
            LegIndexes = new IndexRotation[LegImages.Length];
            for(int i=0; i<LegIndexes.Length; i++) LegIndexes[i] = new IndexRotation(min: 0, max: LegImages[i].Length - 1);

            // init
            Width = width;
            Height = height;
        }

        static Cat()
        {
            // load the images from resources
            foreach (var res in engine.Common.Embedded.LoadResource<byte[]>(System.Reflection.Assembly.GetExecutingAssembly()))
            {
                if (res.Key.Length > 0 && res.Key[0] != '_')
                {
                    // adds to the ImageSource cache
                    var imgsrc = new ImageSource(res.Key, res.Value);
                }
            }
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool IsAngry { get; set; }
        public int Row { get; private set; }
        public float Column { get; private set; }
        public bool IsDone { get; private set; }

        public void Reset(int[] lengths, float xStart, float yStart, float xDelta, float yDelta)
        {
            if (lengths == null || lengths.Length == 0) throw new Exception("Invalid lengths");

            // init
            Lengths = lengths;
            IsAngry = false;
            Row = 0;
            Column = 0;
            IsDone = false;
            XStart = xStart;
            YStart = yStart;
            XDelta = xDelta;
            YDelta = yDelta;

            // reset indexs
            TailIndex.Reset();
            FaceIndex.Reset();
            for (int i = 0; i < LegIndexes.Length; i++) LegIndexes[i].Reset();
        }

        public bool Advance(float increment)
        {
            if (Lengths == null || Lengths.Length == 0 || Row >= Lengths.Length) return false;

            // advance
            Column += increment;

            // advance if necessary (the count quals or surpassed the max)
            var diff = Column - Lengths[Row];
            if (diff >= 0)
            {
                // advance
                Row++;
                Column = 0;
                // apply spill over diff
                if (diff > 0 && Row < Lengths.Length) Column += diff;
                // indicate we are done if we are past the end of the tracks
                if (Row >= Lengths.Length)
                {
                    Column = 0;
                    IsDone = true;
                }
            }
            
            return true;
        }

        public void Draw(IImage img)
        {
            // nothing to draw
            if (Lengths == null || Lengths.Length == 0) return;

            // draw paw prints (the tracks we have already been through
            for (int i = 0; i < Lengths.Length && i <= Row; i++)
            {
                var tx = XStart;
                var ty = YStart + (YDelta * i);
                var maxx = (i < Row) ? XStart + (Lengths[i] * XDelta) : XStart + (Column * XDelta) - Width;
                while (tx < maxx)
                {
                    // display paw-prints
                    img.Graphics.Image(PawPrintImage.Image, tx, ty, Width, Height);
                    tx += Width;
                }
            }

            // the cat reached the end, this should not be possible in a normal game
            if (IsDone) return;

            // set the cat x,y based on the current track
            var x = XStart + (XDelta * Column) - (Width *0.9f);
            var y = YStart + (Row * YDelta);
            var index = 0;

            // add the body
            img.Graphics.Image(BodyImage.Image, x, y, Width, Height);

            // draw legs
            for (int i = 0; i < LegIndexes.Length; i++)
            {
                index = LegIndexes[i].Increment();
                img.Graphics.Image(LegImages[i][index].Image, x, y, Width, Height);
            }

            // draw tail
            img.Graphics.Image(TailImages[TailIndex.Increment()].Image, x, y, Width, Height);

            // draw face
            index = FaceIndex.Increment();
            img.Graphics.Image(FaceImages[index].Image, x, y, Width, Height);
            if (index < 7)
            {
                // add a mood
                img.Graphics.Image(IsAngry ? AngryImage.Image : HappyImage.Image, x, y, Width, Height);
            }
        }

        #region private
        // position
        private int[] Lengths;
        private float XStart;
        private float YStart;
        private float XDelta;
        private float YDelta;

        // images
        private IndexRotation TailIndex;
        private IndexRotation FaceIndex;
        private IndexRotation[] LegIndexes;

        private static readonly ImageSource[] TailImages = new ImageSource[]
        {
            new ImageSource(@"tail_1"),
            new ImageSource(@"tail_2"),
            new ImageSource(@"tail_3"),
            new ImageSource(@"tail_4"),
            new ImageSource(@"tail_5"),
            new ImageSource(@"tail_6"),
            new ImageSource(@"tail_7"),
            new ImageSource(@"tail_8"),
            new ImageSource(@"tail_9")
        };
        private static readonly ImageSource[][] LegImages = new ImageSource[][]
        {
            new ImageSource[]
            {
                new ImageSource(@"leg_1_1"),
                new ImageSource(@"leg_1_2"),
                new ImageSource(@"leg_1_3")
            },
            new ImageSource[]
            {
                new ImageSource(@"leg_2_1"),
                new ImageSource(@"leg_2_2")
            },
            new ImageSource[]
            {
                new ImageSource(@"leg_3_1"),
                new ImageSource(@"leg_3_2"),
                new ImageSource(@"leg_3_3")
            },
            new ImageSource[]
            {
                new ImageSource(@"leg_4_1"),
                new ImageSource(@"leg_4_2"),
                new ImageSource(@"leg_4_3"),
                new ImageSource(@"leg_4_4")
            }
        };
        private static readonly ImageSource BodyImage = new ImageSource(@"body");
        private static readonly ImageSource[] FaceImages = new ImageSource[]
        {
            new ImageSource(@"face_front"),
            new ImageSource(@"face_front"),
            new ImageSource(@"face_front"),
            new ImageSource(@"face_front"),
            new ImageSource(@"face_front"),
            new ImageSource(@"face_front"),
            new ImageSource(@"face_front"),
            new ImageSource(@"face_forward"),
            new ImageSource(@"face_forward")
        };
        private static readonly ImageSource HappyImage = new ImageSource(@"happy");
        private static readonly ImageSource AngryImage = new ImageSource(@"angry");
        private static readonly ImageSource PawPrintImage = new ImageSource(@"paw_prints");
        #endregion
    }
}
