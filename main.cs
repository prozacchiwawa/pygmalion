using System;
using System.IO;
using System.Reflection;
using System.Text;
using pygmalion;

namespace RunJS
{
    class Console
    {
        TextWriter writer;
        public void log(string s) { writer.WriteLine(s); }
        public Console(TextWriter tw) { writer = tw; }
    }

    public class Evaluator
    {
        public static void Main(string []args) {
            jsexec exec = new jsexec();
            exec.GLOBAL.jobject.SetItem(exec.GLOBAL, "console", new JSSimpleProperty("console", new JSInstanceWrapper(exec.GLOBAL, new Console(System.Console.Out))));
            exec.GLOBAL.jobject.SetItem(exec.GLOBAL, "Assembly", new JSSimpleProperty("Assembly", new JSClassWrapper(exec.GLOBAL, typeof(System.Reflection.Assembly))));
            foreach (string arg in args) {
                using (TextReader tr = File.OpenText(arg)) {
                    string jsfile = tr.ReadToEnd();
                    exec.eval(jsfile, arg, 1);
                }
            }
        }
    }
}
