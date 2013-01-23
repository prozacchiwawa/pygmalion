using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pygmalion
{
    class Activation : JSObject
    {
        public Activation(ExecutionContext GLOBAL, Node f, JSObjectBase a)
        {
            int i, j;
            for (i = 0, j = f.fparams.Count; i < j; i++)
            {
                JSProperty ares = a.GetItem(GLOBAL, i.ToString());
                this.SetItem(GLOBAL, f.fparams[i].ToString(), new JSSimpleProperty(f.fparams[i].ToString(), ares != null ? ares.GetValue(GLOBAL) : JSUndefined.Undefined, true));
            }
            this.SetItem(GLOBAL, "arguments", new JSSimpleProperty("arguments", a, true));
        }
    }
}
