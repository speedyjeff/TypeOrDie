using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeOrDie
{
    class Library
    {
        public Library()
        {
            Books = new List<Book>();
            Random = new Random();
        }

        public static Library LoadFromText(string[] text)
        {
            // load all the books and return the library
            var library = new Library();

            foreach (var t in text)
            {
                library.Books.Add(Book.ParseMd(t.Replace("\r", "").Split('\n') ));
            }

            return library;
        }

        public static Library Load(string[] paths)
        {
            // load all the books and return the library
            var library = new Library();

            foreach(var path in paths) library.Books.Add(Book.ParseFile(path));

            return library;
        }

        public Book Randomize()
        {
            // pick a book
            if (Books.Count == 0) throw new Exception("Must have a library of books first");
            return Books[Random.Next() % Books.Count];
        }

        public Poem Randomize(Book book)
        {
            // pick a random poem
            if (book == null || book.Poems.Count == 0) throw new Exception("Must have at least 1 poem");
            return book.Poems[Random.Next() % book.Poems.Count];
        }

        public List<string> Randomize(Poem poem, int maxChars, int maxCharsPerLine)
        {
            // pick a random set of lines within this poem
            if (poem == null || poem.Lines.Count == 0 || maxChars <= 0 || maxCharsPerLine <= 0) throw new Exception("Must provide a valid poem");

            var lines = new List<string>();
            var start = 0;

            // get all the lengths so that a random start can be calculated
            // get the index of which we can still satisfy the 'maxChars' requirement
            // determine a random starting index
            var lengthSum = 0;
            for(int i=poem.Lines.Count-1; i>= 0; i--)
            {
                lengthSum += poem.Lines[i].Length;
                if (lengthSum >= maxChars && i > 0)
                {
                    start = Random.Next() % i;
                    break;
                }
            }

            // grab 'maxChars' from the poem with a local maximum of 'maxCharsPerLine'
            lengthSum = maxChars;
            for(int i=start; lengthSum > 0 && i < poem.Lines.Count; i++)
            {
                var text = poem.Lines[i];
                if (lengthSum - poem.Lines[i].Length < 0)
                {
                    // this is the last line and it is too long
                    var length = lengthSum;
                    // find the end of the current word
                    while (length < text.Length && text[length] != ' ') length++;
                    text = poem.Lines[i].Substring(0, length);
                }

                // add lines that do not exceed 'maxCharsPerLine'
                var index = 0;
                while (index < text.Length)
                {
                    var length = (text.Length - index) > maxCharsPerLine ? maxCharsPerLine : (text.Length - index);
                    // ensure to break on a space
                    while ((index + length) > 0 && (index + length) != text.Length && text[index + length - 1] != ' ') length--;
                    if (length <= 0) throw new Exception("Failed to find a space");
                    lines.Add(text.Substring(index, length).Trim());
                    index += length;
                }

                lengthSum -= poem.Lines[i].Length;
            }

            return lines;
        }

        #region private
        private Random Random;
        private List<Book> Books;
        #endregion
    }
}
