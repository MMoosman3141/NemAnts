using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NemAnts {
  /// <summary>
  /// Entry point for the ant bot.
  /// </summary>
  class Program {
    /// <summary>
    /// Entry method for the ant bot.  Sets up a console listener for debugging purposes, and starts the bot.
    /// </summary>
    /// <param name="args"></param>
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
