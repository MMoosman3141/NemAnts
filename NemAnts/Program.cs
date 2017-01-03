using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NemAnts {
  class Program {
    public static void Main(string[] args) {
      ConsoleTraceListener consoleListener = new ConsoleTraceListener(true);
      Trace.Listeners.Add(consoleListener);
      Trace.AutoFlush = true;

      try {
        NemBot bot = new NemBot();
        bot.Start().Wait();
      } catch(Exception err) {
        Trace.WriteLine(err.ToString());
      }
    }
  }
}
