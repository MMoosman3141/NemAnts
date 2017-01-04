using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NemAnts {
  [Flags]
  public enum SquareTypes {
    Unknown = 0x01,
    Ground = 0x02,
    Water = 0x04,
    Food = 0x08,
    FriendlyHill = 0x10,
    Friendly = 0x20,
    EnemyHill = 0x40,
    Enemy = 0x80,
    DeadAnt = 0x100,
    PreviouslyAnt = 0x200
  }

  public enum Directions {
    North,
    South,
    West,
    East
  }

  /// <summary>
  /// The Map object represents a game map complete with as much information as has been discovered.
  ///   ? = unknown square of the map.
  ///   . = unoccupied land
  ///   % = water
  ///   * = food
  ///   ! = dead ant or ants
  ///   a-j = ant (letter corresponds to the team.  a = this team)
  ///   A-J = ant on its own hill
  ///   0-9 = hill (number corresponds to the team. 0 = this team)
  /// </summary>
  public static class Map {
    private static int _rows = -1;
    private static int _cols = -1;
    private static List<Ant> _myAnts;
    private static List<Ant> _myAntRefresh;
    private static List<Tuple<int, int>> _food;
    private static List<Tuple<int, int>> _myHills;
    private static List<Tuple<int, int>> _enemyHills;
    private static List<Ant> _enemyAnts;
    private static string _ownerLetters = "abcdefghij";
    private static string _ownerNumbers = "0123456789";

    /// <summary>
    /// The number of rows in the grid.
    /// </summary>
    public static int Rows {
      get {
        return _rows;
      }
      set {
        _rows = value;

        if (_rows > -1 && _cols > -1 && Grid == null) {
          SetupGrid();
        }
      }
    }

    /// <summary>
    /// The number of columns in the grid.
    /// </summary>
    public static int Cols {
      get {
        return _cols;
      }
      set {
        _cols = value;

        if (_rows > -1 && _cols > -1 && Grid == null) {
          SetupGrid();
        }
      }
    }

    /// <summary>
    /// The grid as it is currently understood.
    /// </summary>
    public static char[,] Grid { get; private set; }

    /// <summary>
    /// A list of ants owned by this app.
    /// </summary>
    public static List<Ant> MyAnts {
      get {
        return _myAnts;
      }
      set {
        _myAnts = value;
      }
    }

    /// <summary>
    /// A list of ants used to refresh the existance of ants owned by this app.
    /// </summary>
    public static List<Ant> MyAntRefresh {
      get {
        return _myAntRefresh;
      }
      private set {
        _myAntRefresh = value;
      }
    }

    /// <summary>
    /// A list of food currently seen by ants owned by this app.
    /// </summary>
    public static List<Tuple<int, int>> Food {
      get {
        return _food;
      }
      private set {
        _food = value;
      }
    }

    /// <summary>
    /// A list of ant hills owned by this app.
    /// </summary>
    public static List<Tuple<int, int>> MyHills {
      get {
        return _myHills;
      }
      private set {
        _myHills = value;
      }
    }

    /// <summary>
    /// A list of enemy ant hills.
    /// </summary>
    public static List<Tuple<int, int>> EnemyHills {
      get {
        return _enemyHills;
      }
      private set {
        _enemyHills = value;
      }
    }

    /// <summary>
    /// A list of enemy ants.
    /// </summary>
    public static List<Ant> EnemyAnts {
      get {
        return _enemyAnts;
      }
      set {
        _enemyAnts = value;
      }
    }

    private static void SetupGrid() {
      Grid = new char[Rows, Cols];

      for(int row = 0; row < Rows; row++) {
        for(int col = 0; col < Cols; col++) {
          Grid[row, col] = '.';
        }
      }
    }

    /// <summary>
    /// Clear the current grid of temporary items.  Reset or create lists.
    /// </summary>
    public static void ClearGrid() {
      if (MyAnts == null) {
        MyAnts = new List<Ant>();
      }
      if(EnemyHills == null) {
        EnemyHills = new List<Tuple<int, int>>();
      }
      MyAntRefresh = new List<Ant>();
      Food = new List<Tuple<int, int>>();
      MyHills = new List<Tuple<int, int>>();
      EnemyAnts = new List<Ant>();

      for(int row = 0; row < Rows; row++) {
        for(int col = 0; col < Cols; col++) {
          SetGridSquare(row, col, SquareTypes.Ground);
        }
      }
    }

    /// <summary>
    /// Add water to the grid.
    /// </summary>
    /// <param name="row">The row coordinate of the water.</param>
    /// <param name="col">The column coordinate of the water.</param>
    public static void AddWater(int row, int col) {
      SetGridSquare(row, col, SquareTypes.Water);
    }

    /// <summary>
    /// Add food to the grid.
    /// </summary>
    /// <param name="row">The row coordinate of the food.</param>
    /// <param name="col">The column coordinate of the food.</param>
    public static void AddFood(int row, int col) {
      Food.Add(new Tuple<int, int>(row, col));
      SetGridSquare(row, col, SquareTypes.Food);
    }

    /// <summary>
    /// Add an ant hill to the grid.
    /// </summary>
    /// <param name="row">The row coordinate of the hill.</param>
    /// <param name="col">The column coordinate of the hill.</param>
    /// <param name="owner">The owner of the hill.  0 represents a hill owned by this app.</param>
    public static void AddHill(int row, int col, int owner) {
      if (owner == 0) {
        MyHills.Add(new Tuple<int, int>(row, col));
        SetGridSquare(row, col, SquareTypes.FriendlyHill, 0);
      } else {
        Tuple<int, int> hill = new Tuple<int, int>(row, col);

        if (!EnemyHills.Contains(hill)) {
          EnemyHills.Add(new Tuple<int, int>(row, col));
        }
        SetGridSquare(row, col, SquareTypes.EnemyHill, owner);
      }
    }

    /// <summary>
    /// Add an ant to the grid.
    /// </summary>
    /// <param name="row">The row coordinate of the ant.</param>
    /// <param name="col">The column coordinate of the ant.</param>
    /// <param name="owner">The owner of the ant.  0 represents ants owned by this app.</param>
    public static void AddAnt(int row, int col, int owner) {
      Ant newAnt = new Ant() { Row = row, Col = col, Owner = owner };
      if (owner == 0) {
        MyAntRefresh.Add(newAnt);
        SetGridSquare(row, col, SquareTypes.Friendly, owner);
      } else {
        EnemyAnts.Add(newAnt);
        SetGridSquare(row, col, SquareTypes.Enemy, owner);
      }
    }

    /// <summary>
    /// Add a dead ant to the grid.
    /// </summary>
    /// <param name="row">The row coordinate of the dead ant.</param>
    /// <param name="col">The column coordinate of the dead ant.</param>
    /// <param name="owner">The owner of the dead ant.  0 represents dead ants owned by this app.</param>
    public static void AddDeadAnt(int row, int col, int owner) {
      SetGridSquare(row, col, SquareTypes.DeadAnt);
    }

    /// <summary>
    /// Set the point in the grid according to the square type and owner.
    /// </summary>
    /// <param name="row">The coordinate row to set.</param>
    /// <param name="col">The coordinate column to set.</param>
    /// <param name="type">The type of value to store at the point.</param>
    /// <param name="owner">For ants and hills, who owns this point.  0 represents points owned by this app.</param>
    public static void SetGridSquare(int row, int col, SquareTypes type, int owner = 0) {
      //Get whatever already exists in that square if there is anything.
      SquareTypes square = GetGridSquare(row, col);

      //Never replace an existing water square with something else.  This is simply illegal.
      if (square.HasFlag(SquareTypes.Water))
        return;

      //Hills are also not removable, but can coexist with Ants.  Set a flag so we don't lose track of hills.
      bool keepHill = false;
      if(square.HasFlag(SquareTypes.EnemyHill) || square.HasFlag(SquareTypes.FriendlyHill)) {
        keepHill = true;
      }

      if (type == SquareTypes.DeadAnt) {
        Grid[row, col] = '!';
      } else if (type == SquareTypes.Food) {
        Grid[row, col] = '*';
      } else if (type == SquareTypes.Ground) {
        Grid[row, col] = '.';
      } else if (type == SquareTypes.PreviouslyAnt) {
        Grid[row, col] = 'z';
      } else if (type == SquareTypes.Unknown) {
        //If we already know that the square is ground don't overwrite with an unknown.
        if (!square.HasFlag(SquareTypes.Ground)) {
          Grid[row, col] = '?';
        } else {
          Grid[row, col] = '.';
        }
      } else if(type == SquareTypes.Water) {
        Grid[row, col] = '%';
      } else if (type == SquareTypes.Enemy) {
        if (owner != 0) { //An enemy ant can't be 0, but since this is a default we need to check.
          if (!keepHill) {
            Grid[row, col] = _ownerLetters[owner];
          } else {
            Grid[row, col] = _ownerLetters.ToUpper()[owner];
          }
        } else {
          if (!keepHill) {
            Grid[row, col] = 'b'; //If the owner is 0 for an enemy ant set a generic default value.
          } else {
            Grid[row, col] = 'B';
          }
        }
      } else if(type == SquareTypes.EnemyHill) {
        if(owner != 0) {
          if(GetGridSquare(row, col) == SquareTypes.Enemy) { //A little different if there's also an Ant here.
            Grid[row, col] = _ownerLetters.ToUpper()[owner];
          } else {
            Grid[row, col] = _ownerNumbers[owner];
          }
        } else {
          if (GetGridSquare(row, col) == SquareTypes.Enemy) { //A little different if there's also an Ant here.
            Grid[row, col] = 'B';
          } else {
            Grid[row, col] = '1';
          }
        }
      } else if(type == SquareTypes.Friendly) {
        if (!keepHill) {
          Grid[row, col] = 'a';
        } else {
          Grid[row, col] = 'A';
        }
      } else if(type == SquareTypes.FriendlyHill) {
        if (GetGridSquare(row, col) == SquareTypes.Friendly) { //A little different if there's also an Ant here.
          Grid[row, col] = 'A';
        } else {
          Grid[row, col] = '0';
        }
      }

      if(keepHill && !GetGridSquare(row, col).HasFlag(SquareTypes.EnemyHill) && !GetGridSquare(row, col).HasFlag(SquareTypes.FriendlyHill)) {
        if(square.HasFlag(SquareTypes.EnemyHill)) {
          if (owner != 0) {
            Grid[row, col] = _ownerNumbers[owner];
          } else {
            Grid[row, col] = '1';
          }
        } else if(square.HasFlag(SquareTypes.FriendlyHill)) {
          Grid[row, col] = '0';
        }
      }

    }

    /// <summary>
    /// Set the point in the grid according to the square type and owner.
    /// </summary>
    /// <param name="point">The tuple coordinate representing the point in the grid.</param>
    /// <param name="type">The type of value to store at the point.</param>
    /// <param name="owner">For ants and hills, who owns this point.  0 represents points owned by this app.</param>
    public static void SetGridSquare(Tuple<int, int> point, SquareTypes type, int owner = 0) {
      SetGridSquare(point.Item1, point.Item2, type, owner);
    }

    /// <summary>
    /// Gets what is stored at the point of the grid.
    /// </summary>
    /// <param name="row">The row coordinated to check.</param>
    /// <param name="col">The column coordinate to check.</param>
    /// <returns>A SquareTypes value with bit flags representing the various things stored in the grid at the specified point.</returns>
    public static SquareTypes GetGridSquare(int row, int col) {
      char square = Grid[row, col];
      SquareTypes squareValue = SquareTypes.Unknown;

      if(square == '?') {
        squareValue = SquareTypes.Unknown;
      } else if (square == '.') {
        squareValue = SquareTypes.Ground;
      } else if (square == '%') {
        squareValue = SquareTypes.Water;
      } else if (square == '*') {
        squareValue = SquareTypes.Food | SquareTypes.Ground;
      } else if (square == '!') {
        squareValue = SquareTypes.DeadAnt | SquareTypes.Ground;
      } else if (square == 'a') {
        squareValue = SquareTypes.Friendly | SquareTypes.Ground;
      } else if (square >= 'b' && square <= 'j') {
        squareValue = SquareTypes.Enemy | SquareTypes.Ground;
      } else if (square == 'A') {
        squareValue = SquareTypes.Friendly | SquareTypes.FriendlyHill;
      } else if (square >= 'B' && square <= 'J') {
        squareValue = SquareTypes.Enemy | SquareTypes.EnemyHill;
      } else if (square == '0') {
        squareValue = SquareTypes.FriendlyHill;
      } else if (square >= '1' && square <= '9') {
        squareValue = SquareTypes.EnemyHill;
      } else if(square == 'z') {
        squareValue = SquareTypes.PreviouslyAnt | SquareTypes.Ground;
      } else if(square == 'Z') {
        squareValue = SquareTypes.PreviouslyAnt | SquareTypes.FriendlyHill;
      }

      return squareValue;
    }

    /// <summary>
    /// Gets what is stored at the point of the grid.
    /// </summary>
    /// <param name="point">A tuple representing the point in the grid.</param>
    /// <returns>A SquareTypes value with bit flags representing the various things stored in the grid at the specified point.</returns>
    public static SquareTypes GetGridSquare(Tuple<int, int> point) {
      return GetGridSquare(point.Item1, point.Item2);
    }

    /// <summary>
    /// Get the coordinate information directly above the specified point
    /// </summary>
    /// <param name="point">A Tuple representing a point in the grid.</param>
    /// <returns>A Tuple representing the point directly above the specified point.</returns>
    public static Tuple<int, int> GetNorthPoint(Tuple<int, int> point) {
      return new Tuple<int, int>(point.Item1 - 1 < 0 ? Map.Rows - 1 : point.Item1 - 1, point.Item2);
    }

    /// <summary>
    /// Get the coordinate information directly below the specified point
    /// </summary>
    /// <param name="point">A Tuple representing a point in the grid.</param>
    /// <returns>A Tuple representing the point directly below the specified point.</returns>
    public static Tuple<int, int> GetSouthPoint(Tuple<int, int> point) {
      return new Tuple<int, int>(point.Item1 + 1 == Map.Rows ? 0 : point.Item1 + 1, point.Item2);
    }

    /// <summary>
    /// Get the coordinate information directly left of the specified point
    /// </summary>
    /// <param name="point">A Tuple representing a point in the grid.</param>
    /// <returns>A Tuple representing the point directly left of the specified point.</returns>
    public static Tuple<int, int> GetWestPoint(Tuple<int, int> point) {
      return new Tuple<int, int>(point.Item1, point.Item2 - 1 < 0 ? Map.Cols - 1 : point.Item2 - 1);
    }

    /// <summary>
    /// Get the coordinate information directly right of the specified point
    /// </summary>
    /// <param name="point">A Tuple representing a point in the grid.</param>
    /// <returns>A Tuple representing the point directly right of the specified point.</returns>
    public static Tuple<int, int> GetEastPoint(Tuple<int, int> point) {
      return new Tuple<int, int>(point.Item1, point.Item2 + 1 == Map.Cols ? 0 : point.Item2 + 1);
    }

    /// <summary>
    /// Gets the four points around the specified point.
    /// </summary>
    /// <param name="square">A Tuple representing a point in the grid.</param>
    /// <returns>A List of Tuple values representing the four points around the specified point.</returns>
    public static List<Tuple<int, int>> GetAdjacentSquares(Tuple<int, int> square) {
      List<Tuple<int, int>> squares = new List<Tuple<int, int>>();
      squares.Add(GetNorthPoint(square));
      squares.Add(GetSouthPoint(square));
      squares.Add(GetWestPoint(square));
      squares.Add(GetEastPoint(square));

      return squares;
    }

    /// <summary>
    /// Get all the routes in the grid from point a to various other points.
    /// </summary>
    /// <param name="here">The starting point for the routes.</param>
    /// <param name="theres">Various ending points for the routes.</param>
    /// <param name="maxLookDistance">The maximum distance to look for a route.  Routes can not be longer than this value.</param>
    /// <param name="limit">The number of routes to look for.</param>
    /// <returns>Returns a List of routes from here to the various points in the theres.</returns>
    public static List<List<Tuple<int, int>>> GetRoutes(Tuple<int, int> here, List<Tuple<int, int>> theres, int maxLookDistance, int limit = int.MaxValue) {
      List<List<Tuple<int, int>>> routes = new List<List<Tuple<int, int>>>();

      int foundCount = 0;
      for(int index = 0; index < theres.Count; index++) {
        Tuple<int, int> there = theres[index];
        List<Tuple<int, int>> route = Map.ShortestRoute(here, there, maxLookDistance);

        if (route == null)
          continue;

        routes.Add(route);
        foundCount++;

        if (foundCount >= limit)
          break;
      }

      return routes;

      //return theres.Select(point => Map.ShortestRoute(here, point, maxLookDistance)).Where(route => route != null).ToList();
    }

    /// <summary>
    /// Gets the shortest route from point A (here) to various other points (theres).
    /// </summary>
    /// <param name="here">The starting point for all the routes evaluated.</param>
    /// <param name="theres">The various ending points for all the routes evaluated.</param>
    /// <param name="maxLookDistance">The maximum distance a route can be.</param>
    /// <param name="limit">A limit on the number of routes to look for.</param>
    /// <returns>The shortest of various routes.</returns>
    public static List<Tuple<int, int>> GetShortestRoute(Tuple<int, int> here, List<Tuple<int, int>> theres, int maxLookDistance, int limit = int.MaxValue) {
      List<List<Tuple<int, int>>> routes = GetRoutes(here, theres, maxLookDistance, limit);

      if (routes.Count == 0) {
        return null;
      }

      List<Tuple<int, int>> shortest = routes.OrderBy(route => route.Count).First();

      return shortest;
    }

    /// <summary>
    /// Checks if a square is a valid point to move to.
    /// </summary>
    /// <param name="point">A Tuple point to check.</param>
    /// <returns>True if the point is a valid point to move to.  False otherwise.</returns>
    public static bool IsAvailableSquare(Tuple<int, int> point) {
      SquareTypes destSquare = Map.GetGridSquare(point);

      if (destSquare.HasFlag(SquareTypes.Water)) {
        return false;
      }
      if (destSquare.HasFlag(SquareTypes.Friendly)) {
        return false;
      }
      if (destSquare.HasFlag(SquareTypes.FriendlyHill)) {
        return false;
      }
      if (destSquare.HasFlag(SquareTypes.Food)) {
        return false;
      }
      if (destSquare.HasFlag(SquareTypes.PreviouslyAnt)) {
        return false;
      }
      if (destSquare.HasFlag(SquareTypes.Unknown)) {
        return false;
      }

      return true;
    }

    /// <summary>
    /// Find the shortest route from point A to point B.
    /// </summary>
    /// <param name="pointA">The starting point.</param>
    /// <param name="pointB">The ending point.</param>
    /// <param name="maxDistance">The maximum number of steps allowed.</param>
    /// <returns>A List of Tuple points representing the shortest way to get from point A to point B.</returns>
    public static List<Tuple<int, int>> ShortestRoute(Tuple<int, int> pointA, Tuple<int, int> pointB, int maxDistance) {
      int[,] grid = new int[Rows, Cols];

      for(int row = 0; row < Rows; row++) {
        for(int col = 0; col < Cols; col++) {
          if (!IsAvailableSquare(new Tuple<int, int>(row, col)))
            grid[row, col] = -1;
          else
            grid[row, col] = int.MaxValue;
        }
      }
      grid[pointA.Item1, pointA.Item2] = 0;

      MarkDistance(pointA, pointB, grid, 1, maxDistance);

      List<Tuple<int, int>> route = new List<Tuple<int, int>>();

      Tuple<int, int> point = pointB;
      while(!point.Equals(pointA)) {
        List<Tuple<int, int>> adjacentPoints = GetAdjacentSquares(point);

        point = (from adjacentPoint in adjacentPoints
                 where grid[adjacentPoint.Item1, adjacentPoint.Item2] != -1
                 orderby grid[adjacentPoint.Item1, adjacentPoint.Item2]
                 select adjacentPoint).FirstOrDefault();

        if (point == null || grid[point.Item1, point.Item2] == int.MaxValue)
          break;

        if(!point.Equals(pointA))
          route.Insert(0, point);
      }

      if (route.Count == 0)
        return null;

      route.Add(pointB);

      return route;
    }

    /// <summary>
    /// Marks the distance in a grid showing the distance values from point A to the destination.
    /// This function runs recursively in order to fill out the grid with distance values allowing the determination of the shortest route from one point to another.
    /// </summary>
    /// <param name="pointA">The starting point.</param>
    /// <param name="destination">The ending point.</param>
    /// <param name="grid">The grid in which distances are measured.</param>
    /// <param name="distance">The current distance value.  Each recursive call increments this value to put into the grid.</param>
    /// <param name="maxDistance">The furthest distance to mark up in the grid.</param>
    private static void MarkDistance(Tuple<int, int> pointA, Tuple<int, int> destination, int[,] grid, int distance, int maxDistance) {
      if (distance > maxDistance)
        return;

      Tuple<int, int> north = GetNorthPoint(pointA);
      if (north.Equals(destination)) {
        return;
      }

      Tuple<int, int> south = GetSouthPoint(pointA);
      if (south.Equals(destination)) {
        return;
      }

      Tuple<int, int> west = GetWestPoint(pointA);
      if (west.Equals(destination)) {
        return;
      }

      Tuple<int, int> east = GetEastPoint(pointA);
      if (east.Equals(destination)) {
        return;
      }

      List<Tuple<int, int>> points = new List<Tuple<int, int>>();
      points.Add(north);
      points.Add(south);
      points.Add(west);
      points.Add(east);

      for(int index = 0; index < points.Count; index++) {
        Tuple<int, int> point = points[index];
      //foreach(Tuple<int, int> point in points) {
        if (grid[point.Item1, point.Item2] == -1 || grid[point.Item1, point.Item2] == 0 || grid[point.Item1, point.Item2] <= distance) {
          continue;
        }

        grid[point.Item1, point.Item2] = distance;
        MarkDistance(point, destination, grid, distance + 1, maxDistance);
      }
    }
  }
}
