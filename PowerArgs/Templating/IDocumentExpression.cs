using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public interface IDocumentExpression
    {
        string Evaluate(DataContext context);
    }

 
}
