using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeOrDie
{
    static class BookTests
    {
        public static void All()
        {
            HeadTest();
            Braces();
            Transforms();
        }

        public static void HeadTest()
        {
            var lines = new string[]
            {
                "<head>",
                "#Title: Beowulf",
                "       An Anglo-Saxon Epic Poem, Translated From The Heyne-Socin",
                "       Text by Lesslie Hall",
                "",
                "#Author: me of course",
                "</ head >",
                "<book>",
                "#section",
                "##title",
                "mybook",
                "</book>"
            };

            var book = Book.ParseMd(lines);

            if (!book.Title.Equals("Beowulf")) throw new Exception("Failed to get title");
            if (!book.Author.Equals("me of course")) throw new Exception("Failed to get author");
        }

        public static void Braces()
        {
            var lines = new string[]
                {
                    "<book>",
                    "#Section",
                    "##Title",
                    "[The famous race of Spear-Danes.]",
                    "this is a test",
                    "{Scyld, their mighty king, in honor of whom they are often called",
                    "Scyldings. He is the great-grandfather of Hrothgar, so prominent in the",
                    "poem.}",
                    "the end",
                    "stuff at the front {remove me} and end {remove me too}",
                    "around [remove",
                    "all of this",
                    "and this] but not this",
                    "</book>"
                };

            var book = Book.ParseMd(lines);

            if (book.Poems.Count != 1) throw new Exception("Wrong number of poems");
            if (book.Poems[0].Lines.Count != 5) throw new Exception("wrong poem lines");
            if (!book.Poems[0].Lines[0].Equals(lines[4])) throw new Exception("Text does not match");
            if (!book.Poems[0].Lines[1].Equals(lines[8])) throw new Exception("Text does not match");
            if (!book.Poems[0].Lines[2].Equals("stuff at the front and end")) throw new Exception("Text does not match");
            if (!book.Poems[0].Lines[3].Equals("around")) throw new Exception("Text does not match");
            if (!book.Poems[0].Lines[4].Equals("but not this")) throw new Exception("Text does not match");
        }

        //        15 [1]That reaved of their rulers they wretched had erstwhile[2]

        public static void Transforms()
        {
            var lines = new string[]
            {
                "<book>",
                "#section",
                "##title",
                " 100 ’ “ ” _ &mdash; è æ",
                "",
                "45",
                "' this is a comment",
                "</book>"
            };

            var book = Book.ParseMd(lines);

            if (book.Poems.Count != 1) throw new Exception("Incorrect number of poems");
            if (book.Poems[0].Lines.Count != 1) throw new Exception("Wrong number of lines");
            if (!book.Poems[0].Lines[0].Equals("' \" \"   - e a")) throw new Exception("Wrong line transforms");
        }
    }
}
