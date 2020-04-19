using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TypeOrDie
{
    // http://www.gutenberg.org/cache/epub/43863/pg43863.txt
    // http://www.gutenberg.org/cache/epub/8378/pg8378.txt
    // http://www.gutenberg.org/cache/epub/12135/pg12135.txt

    // peoms(search: http://www.gutenberg.org/ebooks/search/?query=Poems+)
    // http://www.gutenberg.org/cache/epub/37810/pg37810.txt
    // http://www.gutenberg.org/files/61286/61286-0.txt
    // http://www.gutenberg.org/files/61070/61070-0.txt
    // http://www.gutenberg.org/files/60252/60252-0.txt

    class Book
    {
        public Book()
        {
            Poems = new List<Poem>();
        }

        public static Book ParseFile(string path)
        {
            return ParseMd(File.ReadAllLines(path));
        }

        public static Book ParseMd(string[] lines)
        {
            // spec:
            //   <head>
            //   #Title: Title
            //   #Author: Author
            //   </head>
            //   <book>
            //   ' comment
            //   #Section Header
            //   #Name
            //   </book>
            // transforms:
            //   remove:
            //      {...}, [...] (may be multiline, and more than 1 per line)
            //      completely whitespace
            //      leading and trailing whitespace
            //      numbers (eg. line numbers)
            //   replace:
            //      ’, “, ”, &mdash;, è, _

            var book = new Book();
            var poems = new Dictionary<string /*section*/, Dictionary<string /*title*/, Poem>>();
            var delineators = new Tuple<char, char>[] { new Tuple<char, char>('{', '}'), new Tuple<char, char>('[', ']') };
            var state = ParseState.None;
            var section = "";
            var title = "";
            var validCharacters = new Regex("^[\\\" -abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789,:;]+$");
            for (int i=0; i<lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                // trim leading and trailing whitespace
                lines[i] = lines[i].Trim();

                // skip comments
                if (lines[i].StartsWith("'")) continue;

                // remove leading numbers
                var index = -1;
                while ((index+1) < lines[i].Length && char.IsDigit(lines[i][index+1])) index++;
                if (index >= 0) lines[i] = lines[i].Substring(index + 1).Trim();

                // check state
                if (lines[i].Equals("<head>", StringComparison.OrdinalIgnoreCase)) { state = ParseState.Head; continue; }
                else if (lines[i].Equals("</head>", StringComparison.OrdinalIgnoreCase)) state = ParseState.None;
                else if (lines[i].Equals("<book>", StringComparison.OrdinalIgnoreCase)) { state = ParseState.Book; continue; }
                else if (lines[i].Equals("</book>", StringComparison.OrdinalIgnoreCase)) state = ParseState.None;

                if (state == ParseState.None) continue;

                // transform
                lines[i] = lines[i].Replace("’", "'")
                                    .Replace("‘", "'")
                                    .Replace("“", "\"")
                                    .Replace("”", "\"")
                                    .Replace("  ", " ")
                                    .Replace("&mdash;", "-")
                                    .Replace("_", " ")
                                    .Replace("è", "e")
                                    .Replace("á", "a")
                                    .Replace("ü", "u")
                                    .Replace("ē", "e")
                                    .Replace("é", "e")
                                    .Replace("æ", "a");

                // remove {...} [...]
                foreach (var m in delineators)
                {
                    // find the start and end of these delineators
                    var lineStart = -1;
                    var lineEnd = -1;
                    var start = lines[i].IndexOf(m.Item1);
                    var end = -1;
                    var escape = 10;
                    while (start >= 0)
                    {
                        lineStart = lineEnd = i;
                        // find the end
                        do
                        {
                            end = lines[lineEnd].IndexOf(m.Item2);
                            if (end < 0) lineEnd++;
                        }
                        while (end < 0 && lineEnd < lines.Length && --escape >= 0);

                        // hit the limit
                        if (escape == 0) throw new Exception("Failed to find closing delineator");

                        // single line remove
                        if (lineEnd == i)
                        {
                            var line = "";
                            if (start > 0) line = lines[i].Substring(0, start - 1);
                            if ((end + 1) < lines[i].Length) line += lines[i].Substring(end + 1);
                            lines[i] = line.Trim();
                        }
                        // multi-line
                        else
                        {
                            // first line
                            if (start > 0) lines[lineStart] = lines[lineStart].Substring(0, start - 1).TrimEnd();
                            else lines[lineStart] = "";

                            // middlelines
                            lineStart++;
                            while(lineStart < lineEnd)
                            {
                                lines[lineStart++] = "";
                            }

                            // last line
                            if ((end + 1) < lines[lineEnd].Length) lines[lineEnd] = lines[lineEnd].Substring(end + 1).TrimStart();
                            else lines[lineEnd] = "";
                        }

                        // check again
                        start = lines[i].IndexOf(m.Item1);
                    }
                }

                // remove empty lines
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                // parse
                if (state == ParseState.Head)
                {
                    if (lines[i].ToLower().StartsWith("#title:"))
                    {
                        book.Title = lines[i].Substring(7).Trim();
                    }
                    else if (lines[i].ToLower().StartsWith("#author:"))
                    {
                        book.Author = lines[i].Substring(8).Trim();
                    }
                }
                else if (state == ParseState.Book)
                {
                    if (lines[i].StartsWith("##"))
                    {
                        title = lines[i].Substring(2).Trim();
                    }
                    else if (lines[i].StartsWith("#"))
                    {
                        section = lines[i].Substring(1).Trim();
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(title)) throw new Exception("Empty names");

                        // add poem
                        Dictionary<string, Poem> bySection = null;
                        if (!poems.TryGetValue(section, out bySection))
                        {
                            bySection = new Dictionary<string, Poem>();
                            poems.Add(section, bySection);
                        }
                        Poem poem = null;
                        if (!bySection.TryGetValue(title, out poem))
                        {
                            poem = new Poem() { Section = section, Title = title };
                            bySection.Add(title, poem);
                            book.Poems.Add(poem);
                        }

                        // sanity check the line content
                        if (!validCharacters.IsMatch(lines[i])) throw new Exception("Failed on : " + lines[i]);

                        // add lines to the poem
                        poem.Lines.Add(lines[i].Trim());
                    }
                }
                else
                {
                    throw new Exception("Unknown state");
                }
            }

            if (book.Poems.Count == 0) throw new Exception("Failed to parse poems");

            return book;
        }

        public List<Poem> Poems;
        public string Title;
        public string Author;

        #region private
        private const bool IsDebug = true;
        private enum ParseState { None, Head, Book };

        #endregion
    }
}
