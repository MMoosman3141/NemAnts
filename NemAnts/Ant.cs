using System;
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
    public static int ViewRadiusSqrd { get; set; }
    public static int AttackRadiusSqrd { get; set; }
    public static int SpawnRadiusSqrd { get; set; }
    public static Random _rnd = new Random();

    public AntActions AntAction { get; set; } = AntActions.None;
    public int Owner { get; set; }
    public int Row { get; set; }
    public int Col { get; set; }
    public Tuple<int, int> CurrentPos {
      get {
        return new Tuple<int, int>(Row, Col);
      }
    }
    public List<Tuple<int, int>> Route { get; set; }

    public void TakeAction() {
      List<Tuple<int, int>> possibleMoves = Map.GetAdjacentSquares(CurrentPos);

      //Find the valid moves in the possible moves.
      List<Tuple<int, int>> validMoves = (from possibleMove in possibleMoves
                                          where Map.IsAvailableSquare(possibleMove)
                                          select possibleMove).ToList();

      //If the ant is completely boxed in, don't try to move.  If we have a route delete it.
      if (validMoves.Count == 0) {
        if((Route?.Count ?? 0) > 0) {
          Route = null;
        }
        return;
      }

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

      
      //if I already have a route move along the route.
      if ((Route?.Count ?? 0) > 0) {
        //if my action is to actack an enemy or to follow a friend, and I have a route, move along that route regardless of the fact that the enemy will have moved on.
        if(AntAction == AntActions.AttackingEnemy || AntAction == AntActions.Following) {
          if(TryMove(CurrentPos, Route[0])) {
            Route.RemoveAt(0);
            return;
          }
        } else {
          //If the route is toward food or an enemy hill and the end of the route no longer matches that, remove the route.
          SquareTypes lastSquare = Map.GetGridSquare(Route.Last());
          if (lastSquare.HasFlag(SquareTypes.Food) || lastSquare.HasFlag(SquareTypes.EnemyHill)) {
            if (TryMove(CurrentPos, Route[0])) {
              Route.RemoveAt(0);
              return;
            }
          } else {
            Route = null;
          }
        }
      }

      //If we're short on time, just move randomly.
      if (NemBot.TurnStopWatch.ElapsedMilliseconds >= NemBot.TurnTime - 100) {
        TryMove(CurrentPos, validMoves[_rnd.Next(validMoves.Count)]);
        return;
      }

      //look for nearby food, set a route and head down that route.
      if (AttemptRoute(Map.Food, 10, 1, AntActions.MovingToFood, validMoves)) {
        return;
      }

      int distance = 20;

      //If there is an enemy hill nearby set a route toward it and start down the route.
      if (AttemptRoute(Map.EnemyHills, distance, 1, AntActions.AttackingHill, validMoves)) {
        return;
      }

      //if there is an enemy ant nearby move in that general direction
      List<Tuple<int, int>> enemyAnts = Map.EnemyAnts.Select(ant => ant.CurrentPos).ToList();
      if (AttemptRoute(enemyAnts, distance, 1, AntActions.AttackingEnemy, validMoves)) {
        return;
      }

      //look for nearby food, set a route and head down that route.
      if (AttemptRoute(Map.Food, distance, 1, AntActions.MovingToFood, validMoves)) {
        return;
      }

      //try to follow some other ant.
      List<Tuple<int, int>> activeAnts = Map.MyAnts.Where(ant => ant.AntAction != AntActions.None).Select(ant => new Tuple<int, int>(ant.Row, ant.Col)).ToList();
      if (AttemptRoute(activeAnts, 10, 1, AntActions.Following, validMoves)) {
        return;
      }

      //If we've gotten this far then we just want to try to move randomly.
      TryMove(CurrentPos, validMoves[_rnd.Next(validMoves.Count)]);
    }

    private bool TryMove(Tuple<int, int> currentPos, Tuple<int, int> destination) {
      if (!Map.GetGridSquare(currentPos).HasFlag(SquareTypes.Friendly)) {
        Trace.WriteLine("*****   Attempting to move illegal ant.");
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

      //If we're short on time, just move randomly.
      if (NemBot.TurnStopWatch.ElapsedMilliseconds >= NemBot.TurnTime - 100) {
        TryMove(CurrentPos, validMoves[_rnd.Next(validMoves.Count)]);
        return true;
      }

      return false;
    }
  }
}
