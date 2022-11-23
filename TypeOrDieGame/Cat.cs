using engine.Common;
using System;
using System.Collections.Generic;

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

        public Dictionary<string, byte[]> GetImagesFromResources()
        {
            // load the images from resources
            var results = new Dictionary<string, byte[]>();
            foreach (var kvp in engine.Common.Embedded.LoadResource(System.Reflection.Assembly.GetExecutingAssembly()))
            {
                if (kvp.Key.Length > 0 && kvp.Key.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    // this is an image

                    // create short name
                    var parts = kvp.Key.Split('.');
                    var name = parts.Length < 2 ? kvp.Key : parts[parts.Length - 2];

                    // get image bytes
                    var bytes = new byte[kvp.Value.Length];
                    kvp.Value.Read(bytes, 0, bytes.Length);
                    results.Add(name, bytes);
                }
            }
            return results;
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

        public void Draw(IGraphics graphics)
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
                    graphics.Image(PawPrintImage.Image, tx, ty, Width, Height);
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
            graphics.Image(BodyImage.Image, x, y, Width, Height);

            // draw legs
            for (int i = 0; i < LegIndexes.Length; i++)
            {
                index = LegIndexes[i].Increment();
                graphics.Image(LegImages[i][index].Image, x, y, Width, Height);
            }

            // draw tail
            graphics.Image(TailImages[TailIndex.Increment()].Image, x, y, Width, Height);

            // draw face
            index = FaceIndex.Increment();
            graphics.Image(FaceImages[index].Image, x, y, Width, Height);
            if (index < 7)
            {
                // add a mood
                graphics.Image(IsAngry ? AngryImage.Image : HappyImage.Image, x, y, Width, Height);
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
            new ImageSource(@"tail-1"),
            new ImageSource(@"tail-2"),
            new ImageSource(@"tail-3"),
            new ImageSource(@"tail-4"),
            new ImageSource(@"tail-5"),
            new ImageSource(@"tail-6"),
            new ImageSource(@"tail-7"),
            new ImageSource(@"tail-8"),
            new ImageSource(@"tail-9")
        };
        private static readonly ImageSource[][] LegImages = new ImageSource[][]
        {
            new ImageSource[]
            {
                new ImageSource(@"leg-1-1"),
                new ImageSource(@"leg-1-2"),
                new ImageSource(@"leg-1-3")
            },
            new ImageSource[]
            {
                new ImageSource(@"leg-2-1"),
                new ImageSource(@"leg-2-2")
            },
            new ImageSource[]
            {
                new ImageSource(@"leg-3-1"),
                new ImageSource(@"leg-3-2"),
                new ImageSource(@"leg-3-3")
            },
            new ImageSource[]
            {
                new ImageSource(@"leg-4-1"),
                new ImageSource(@"leg-4-2"),
                new ImageSource(@"leg-4-3"),
                new ImageSource(@"leg-4-4")
            }
        };
        private static readonly ImageSource BodyImage = new ImageSource(@"body");
        private static readonly ImageSource[] FaceImages = new ImageSource[]
        {
            new ImageSource(@"face-front"),
            new ImageSource(@"face-front"),
            new ImageSource(@"face-front"),
            new ImageSource(@"face-front"),
            new ImageSource(@"face-front"),
            new ImageSource(@"face-front"),
            new ImageSource(@"face-front"),
            new ImageSource(@"face-forward"),
            new ImageSource(@"face-forward")
        };
        private static readonly ImageSource HappyImage = new ImageSource(@"happy");
        private static readonly ImageSource AngryImage = new ImageSource(@"angry");
        private static readonly ImageSource PawPrintImage = new ImageSource(@"paw-prints");
        #endregion
    }
}
