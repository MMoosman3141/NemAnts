﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NemAnts {
  public enum AntActions {
    None,
    MovingToFood,
    AttackingHill,
    AttackingEnemy,
    Following
  }

  public class Ant {
    /// <summary>
    /// The radius squared that each ant can see around itself.
    /// </summary>
    public static int ViewRadiusSqrd { get; set; }

    /// <summary>
    /// The radius squared that ants attack each other at.
    /// </summary>
    public static int AttackRadiusSqrd { get; set; }

    /// <summary>
    /// The radius squared at which food is collected.
    /// </summary>
    public static int SpawnRadiusSqrd { get; set; }

    private static Random _rnd = new Random();

    /// <summary>
    /// What is this ant currently trying to do.
    /// </summary>
    public AntActions AntAction { get; set; } = AntActions.None;

    /// <summary>
    /// Which player owns this ant.  Player 0 is this app.
    /// </summary>
    public int Owner { get; set; }

    /// <summary>
    /// What row in the Map.Grid is the ant located at.
    /// </summary>
    public int Row { get; set; }

    /// <summary>
    /// What column in the Map.Grid is the ant located at.
    /// </summary>
    public int Col { get; set; }

    /// <summary>
    /// Returns back a Tuple cooridinate of the Row and Col for the position of the ant.
    /// </summary>
    public Tuple<int, int> CurrentPos {
      get {
        return new Tuple<int, int>(Row, Col);
      }
    }

    /// <summary>
    /// A route which the ant may be on to collect food, attack an enemy, or follow another ant.
    /// </summary>
    public List<Tuple<int, int>> Route { get; set; }

    /// <summary>
    /// Take action for the ant.  This includes doing things like locating routes to food, enemies, or other ants, and moving.
    /// </summary>
    public void TakeAction() {
      Stopwatch actionTimer = new Stopwatch();
      actionTimer.Start();

      List<Tuple<int, int>> possibleMoves = Map.GetAdjacentSquares(CurrentPos);
      Trace.WriteLine($"*****    1 - {actionTimer.ElapsedMilliseconds}");

      //Find the valid moves in the possible moves.
      List<Tuple<int, int>> validMoves = (from possibleMove in possibleMoves
                                          where Map.IsAvailableSquare(possibleMove)
                                          select possibleMove).ToList();

      Trace.WriteLine($"*****    2 - {actionTimer.ElapsedMilliseconds}");

      //If the ant is completely boxed in, don't try to move.  If we have a route delete it.
      if (validMoves.Count == 0) {
        if((Route?.Count ?? 0) > 0) {
          Route = null;
        }
        return;
      }

      Trace.WriteLine($"*****    3 - {actionTimer.ElapsedMilliseconds}");

      //If there is only one valid move, move there.  Adjust route if that move is along the route, othewise delete the route.
      if (validMoves.Count == 1) {
        if (TryMove(CurrentPos, validMoves[0])) {
          if ((Route?.Count ?? 0) > 0 && validMoves[0].Equals(Route[0])) {
            Route.RemoveAt(0);
          } else if((Route?.Count ?? 0) > 0) {
            Route = null;
          }
          return;
        }
      }

      Trace.WriteLine($"*****    4 - {actionTimer.ElapsedMilliseconds}");

      //If an adjacent square is an enemy hill or is food take appropriate action.
      for (int moveIndex = 0; moveIndex < possibleMoves.Count; moveIndex++) {
        Tuple<int, int> possibleMove = possibleMoves[moveIndex];
        if (Map.GetGridSquare(possibleMove).HasFlag(SquareTypes.EnemyHill)) {
          if (TryMove(CurrentPos, possibleMove)) {
            Map.EnemyHills.Remove(possibleMove);
            Route = null;
            return;
          }
        }
        if (Map.GetGridSquare(possibleMove).HasFlag(SquareTypes.Food)) {
          Route = null;
          return;
        }
      }

      Trace.WriteLine($"*****    5 - {actionTimer.ElapsedMilliseconds}");

      //If we're short on time, just move randomly.
      if (NemBot.TurnStopWatch.ElapsedMilliseconds >= NemBot.TurnTime - 100) {
        TryMove(CurrentPos, validMoves[_rnd.Next(validMoves.Count)]);
        return;
      }

      Trace.WriteLine($"*****    6 - {actionTimer.ElapsedMilliseconds}");

      if((Route?.Count ?? 0) > 0) {
        if(TryMove(CurrentPos, Route[0])) {
          Route.RemoveAt(0);
          return;
        }
      }

      Trace.WriteLine($"*****    7 - {actionTimer.ElapsedMilliseconds}");

      int distance = (int)Math.Sqrt((double)ViewRadiusSqrd);
      int attackDist = Math.Min(Map.Rows, Map.Cols) / 2;

      Trace.WriteLine($"*****    8 - {actionTimer.ElapsedMilliseconds}");

      //If there is an enemy hill nearby set a route toward it and start down the route.
      if (AttemptRoute(Map.EnemyHills, attackDist, 1, AntActions.AttackingHill, validMoves)) {
        return;
      }

      Trace.WriteLine($"*****    9 - {actionTimer.ElapsedMilliseconds}");

      //if there is an enemy ant nearby move in that general direction
      List<Tuple<int, int>> enemyAnts = Map.EnemyAnts.Select(ant => ant.CurrentPos).ToList();
      if (AttemptRoute(enemyAnts, distance * 4, 1, AntActions.AttackingEnemy, validMoves)) {
        return;
      }

      Trace.WriteLine($"*****    10 - {actionTimer.ElapsedMilliseconds}");

      //look for nearby food, set a route and head down that route.
      if (AttemptRoute(Map.Food, distance * 2, 1, AntActions.MovingToFood, validMoves)) {
        return;
      }

      Trace.WriteLine($"*****    11 - {actionTimer.ElapsedMilliseconds}");

      //try to follow some other ant.
      List<Tuple<int, int>> activeAnts = Map.MyAnts.Where(ant => ant.AntAction != AntActions.None).Select(ant => new Tuple<int, int>(ant.Row, ant.Col)).ToList();
      if (AttemptRoute(activeAnts, distance * 2, 1, AntActions.Following, validMoves)) {
        return;
      }

      Trace.WriteLine($"*****    12 - {actionTimer.ElapsedMilliseconds}");

      //If we've gotten this far then we just want to try to move randomly.
      TryMove(CurrentPos, validMoves[_rnd.Next(validMoves.Count)]);

      Trace.WriteLine($"*****    13 - {actionTimer.ElapsedMilliseconds}");
    }

    private bool TryMove(Tuple<int, int> currentPos, Tuple<int, int> destination) {
      if (!Map.GetGridSquare(currentPos).HasFlag(SquareTypes.Friendly)) {
        Trace.WriteLine("***** Attempting to move illegal ant.");
        return false;
      }

      bool validMove = Map.IsAvailableSquare(destination);

      if (validMove) {
        char move;
        if (destination.Equals(Map.GetNorthPoint(currentPos))) {
          move = 'N';
        } else if (destination.Equals(Map.GetSouthPoint(currentPos))) {
          move = 'S';
        } else if (destination.Equals(Map.GetWestPoint(currentPos))) {
          move = 'W';
        } else { //East
          move = 'E';
        }

        Map.SetGridSquare(currentPos, SquareTypes.PreviouslyAnt, 0);
        Map.SetGridSquare(destination, SquareTypes.Friendly, 0);

        Console.WriteLine($"o {currentPos.Item1} {currentPos.Item2} {move}");

        Row = destination.Item1;
        Col = destination.Item2;
      }

      return validMove;
    }

    private bool AttemptRoute(List<Tuple<int, int>> optionPoints, int distance, int limit, AntActions actionValue, List<Tuple<int, int>> validMoves) {
      //If we're short on time, just move randomly.
      if (NemBot.TurnStopWatch.ElapsedMilliseconds >= NemBot.TurnTime - 250) {
        TryMove(CurrentPos, validMoves[_rnd.Next(validMoves.Count)]);
        return true;
      }

      if ((optionPoints?.Count ?? 0) == 0)
        return false;

      List<Tuple<int, int>> route = Map.GetShortestRoute(CurrentPos, optionPoints, distance, limit);

      if ((route?.Count ?? 0) > 0) {
        AntAction = actionValue;

        Route = route;

        if (TryMove(CurrentPos, Route[0])) {
          Route.RemoveAt(0);
          return true;
        }
      }

      return false;
    }
  }
}
