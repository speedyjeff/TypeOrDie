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
            // winforms init
            InitializeComponent();
            this.Width = 800;
            this.Height = 512;
            this.Text = "Cats Hate Typings Mistakes";
            var icons = engine.Common.Embedded.LoadResource(System.Reflection.Assembly.GetExecutingAssembly());
            if (icons.TryGetValue("full", out var icon)) this.Icon = new Icon(icons["full"]);
            // setting a double buffer eliminates the flicker
            this.DoubleBuffered = true;

            // game init
            Game = new TypeOrDieGame(this.Width, this.Height);

            // link to this control
            UI = new UIHookup(this, Game.Board);
        }

        #region private
        private TypeOrDieGame Game;
        private UIHookup UI;
        #endregion
    }
}
