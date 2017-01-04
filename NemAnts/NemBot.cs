using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NemAnts {
  public enum GameStates {
    None,
    Setup,
    Update
  }

  public class NemBot {
    private static int _turnTime;
    private static Stopwatch _stopwatch;

    /// <summary>
    /// A timer which can be checked against to see if the turn is nearing completion.
    /// </summary>
    public static Stopwatch TurnStopWatch {
      get {
        return _stopwatch;
      }
      private set {
        _stopwatch = value;
      }
    }

    /// <summary>
    /// The amount of time allowed to perform load functions.
    /// </summary>
    public int LoadTime { get; set; }

    /// <summary>
    /// The amount of time allowed to take our turn.
    /// </summary>
    public static int TurnTime {
      get {
        return _turnTime;
      }
      set {
        _turnTime = value;
      }
    }

    /// <summary>
    /// The current turn in the game.
    /// </summary>
    public int CurrentTurn { get; set; }

    /// <summary>
    /// The maximum number of turns in the game.
    /// </summary>
    public int MaxTurns { get; set; }

    /// <summary>
    /// A seed value which can be used in order to duplicate games where ants move randomly.
    /// </summary>
    public Int64 PlayerSeed { get; set; }

    /// <summary>
    /// The state the game is currently in.
    /// </summary>
    public GameStates GameState { get; set; } = GameStates.None;

    /// <summary>
    /// Get the bot going.  Checks for messages, performs set up, updates, and turns.
    /// </summary>
    /// <returns></returns>
    public Task Start() {
      return Task.Run(() => {
        NemBot.TurnStopWatch = new Stopwatch();

        bool done = false;
        do {
          List<string> input = Console.ReadLine().Split(default(string[]), StringSplitOptions.RemoveEmptyEntries).ToList();
          string type = input[0];
          string value = input.Count >= 2 ? string.Join(" ", input.GetRange(1, input.Count - 1)) : "";

          switch (type) {
            case "turn":
              CurrentTurn = int.Parse(value);
              if(CurrentTurn == 0) {
                GameState = GameStates.Setup;
              } else {
                GameState = GameStates.Update;
                Map.ClearGrid();
              }
              break;
            case "ready":
              Go(); //Send a go message back.
              break;
            case "go":
              NemBot.TurnStopWatch.Restart();
              ReconcileAnts();
              TakeTurn();
              Go(); //End with a go message.
              break;
            case "end":
              Trace.WriteLine("Game Debug: end");
              done = true;
              break;
            default:
              if(GameState == GameStates.Setup) {
                ReadSetup(type, value);
              } else if(GameState == GameStates.Update) {
                UpdateInformation(type, value);
              }
              break;
          }

        } while (!done);
      });
    }

    /// <summary>
    /// Read set up values.
    /// </summary>
    /// <param name="type">The type of value.</param>
    /// <param name="value">The value to set.</param>
    private void ReadSetup(string type, string value) {
      switch(type) {
        case "loadtime":
          LoadTime = int.Parse(value);
          break;
        case "turntime":
          TurnTime = int.Parse(value);
          break;
        case "rows":
          Map.Rows = int.Parse(value);
          break;
        case "cols":
          Map.Cols = int.Parse(value);
          break;
        case "turns":
          MaxTurns = int.Parse(value);
          break;
        case "viewradius2":
          Ant.ViewRadiusSqrd = int.Parse(value);
          break;
        case "attackradius2":
          Ant.AttackRadiusSqrd = int.Parse(value);
          break;
        case "spawnradius2":
          Ant.SpawnRadiusSqrd = int.Parse(value);
          break;
        case "player_seed":
          PlayerSeed = Int64.Parse(value);
          break;
      }
    }

    /// <summary>
    /// Update information.
    /// </summary>
    /// <param name="type">The type of the value.</param>
    /// <param name="value">The value to set.</param>
    private void UpdateInformation(string type, string value) {
      string[] values = value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);

      int row = -1;
      int col = -1;
      int owner = -1;

      if (values.Length >= 1)
        row = int.Parse(values[0]);
      if(values.Length >= 2)
        col = int.Parse(values[1]);
      if (values.Length == 3)
        owner = int.Parse(values[2]);

      switch(type) {
        case "w":
          Map.AddWater(row, col);
          break;
        case "f":
          Map.AddFood(row, col);
          break;
        case "h":
          Map.AddHill(row, col, owner);
          break;
        case "a":
          Map.AddAnt(row, col, owner);
          break;
        case "d":
          Map.AddDeadAnt(row, col, owner);
          break;
      }
    }

    /// <summary>
    /// Reconcile updated information about ants with existing information about ants.
    /// </summary>
    private void ReconcileAnts() {
      AntComparer antComparer = new AntComparer();
      IEnumerable<Ant> routedAnts = Map.MyAnts.Where(ant => (ant.Route?.Count ?? 0) > 0);

      List<Ant> existing = routedAnts.Intersect(Map.MyAntRefresh, antComparer).ToList();

      Map.MyAnts = existing.Union(Map.MyAntRefresh, antComparer).ToList();
    }

    /// <summary>
    /// Take my turn.  Try to end a little early if we're approaching our end of turn time.
    /// </summary>
    private void TakeTurn() {
      for (int index = 0; index < Map.MyAnts.Count; index++) {
        Ant ant = Map.MyAnts[index];
        ant.TakeAction();

        if (NemBot.TurnStopWatch.ElapsedMilliseconds >= NemBot.TurnTime - 100) {
          break;
        }
      }
    }

    /// <summary>
    /// Send a go message to the ants game.
    /// </summary>
    private void Go() {
      GameState = GameStates.None;
      Console.WriteLine("go");
    }
  }
}
