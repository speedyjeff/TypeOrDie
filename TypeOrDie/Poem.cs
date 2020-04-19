using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeOrDie
{
    class Poem
    {
        public string Section;
        public string Title;
        public List<string> Lines { get; }

        public Poem()
        {
            Lines = new List<string>();
        }
    }
}
