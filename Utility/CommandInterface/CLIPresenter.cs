using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility.CommandInterface
{
    public interface CLIPresenter
    {
        List<CLIViewItem> GetViewData();
        (int left, int top) GetCursorOffset();
    }
}
