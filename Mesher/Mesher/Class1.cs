using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mesher
{
    class labledLine: netDxf.Entities.HatchBoundaryPath.Line
    {
        // 0 for horizontal, 1 for vertical
        bool direction = false;

        int ID = 0;
    }
}
 