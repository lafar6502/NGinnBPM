using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.ProcessModel
{
    public interface IValidate
    {
        bool Validate(List<string> problemsFound);
    }
}
