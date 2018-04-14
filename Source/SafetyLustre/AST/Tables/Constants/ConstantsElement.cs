using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.AST.Tables.Constants
{
    class ConstantsElement : Element
    {
        public List<ConstantElement> Children { get; set; }

        public ConstantsElement()
        {
            Children = new List<ConstantElement>();
        }
    }
}
