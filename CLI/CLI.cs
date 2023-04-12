using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InputHandler;
using Logger;
using CommandManager;

namespace CLI
{
    public class CLI
    {
        CLIPresenter _presenter;

        public CLI(CLIPresenter presenter)
        {
            _presenter = presenter;
        }

        public void UpdateView()
        {
            _resetConsoleBuffer();
            _displayView();
        }

        private void _resetConsoleBuffer()
        {
            Console.Clear();
            Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
        }

        private void _displayView()
        {
            List<CLIViewItem> textLines = _presenter.GetViewData();
            foreach (CLIViewItem textLine in textLines)
            {
                textLine.Display();
            }
        }
    }
}
