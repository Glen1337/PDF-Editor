using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFConverter.Model
{
    interface ILogger
    {
        void Log(String error);
        void Dispose();
        void Dispose(Boolean _Bool);
    }
}
