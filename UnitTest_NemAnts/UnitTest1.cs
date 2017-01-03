using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NemAnts;
using System.Collections.Generic;

namespace UnitTest_NemAnts {
  [TestClass]
  public class UnitTest1 {
    [TestMethod]
    public void TestMapFindShortestRoute() {
      Map.Rows = 10;
      Map.Cols = 10;

      Map.Grid[1, 5] = '%';
      Map.Grid[1, 6] = '%';
      Map.Grid[1, 7] = '%';
      Map.Grid[1, 8] = '%';

      Map.Grid[2, 8] = '%';

      Map.Grid[3, 3] = 'a';
      Map.Grid[3, 4] = 'a';
      Map.Grid[3, 5] = 'a';
      Map.Grid[3, 6] = 'a';
      Map.Grid[3, 8] = '%';

      Map.Grid[4, 3] = 'a';
      Map.Grid[4, 6] = '%';
      Map.Grid[4, 8] = '%';

      Map.Grid[5, 3] = 'a';
      Map.Grid[5, 6] = '%';
      Map.Grid[5, 8] = '%';

      Map.Grid[6, 0] = '%';
      Map.Grid[6, 1] = 'a';
      Map.Grid[6, 2] = 'a';
      Map.Grid[6, 3] = 'a';
      Map.Grid[6, 4] = '%';
      Map.Grid[6, 5] = '%';
      Map.Grid[6, 6] = '%';
      Map.Grid[6, 8] = '%';

      Map.Grid[7, 0] = '%';
      Map.Grid[7, 8] = '%';

      Map.Grid[8, 0] = '%';
      Map.Grid[8, 1] = '%';
      Map.Grid[8, 2] = '%';
      Map.Grid[8, 3] = '%';
      Map.Grid[8, 4] = '%';
      Map.Grid[8, 5] = '%';
      Map.Grid[8, 6] = '%';
      Map.Grid[8, 7] = '%';
      Map.Grid[8, 8] = '%';

      List<Tuple<int, int>> route = Map.ShortestRoute(new Tuple<int, int>(2, 5), new Tuple<int, int>(7, 1), 20);

      Assert.IsTrue(route[0].Item1 == 2 && route[0].Item2 == 6);
      Assert.IsTrue(route[1].Item1 == 2 && route[1].Item2 == 7);
      Assert.IsTrue(route[2].Item1 == 3 && route[2].Item2 == 7);
      Assert.IsTrue(route[3].Item1 == 4 && route[3].Item2 == 7);
      Assert.IsTrue(route[4].Item1 == 5 && route[4].Item2 == 7);
      Assert.IsTrue(route[5].Item1 == 6 && route[5].Item2 == 7);
      Assert.IsTrue(route[6].Item1 == 7 && route[6].Item2 == 7);
      Assert.IsTrue(route[7].Item1 == 7 && route[7].Item2 == 6);
      Assert.IsTrue(route[8].Item1 == 7 && route[8].Item2 == 5);
      Assert.IsTrue(route[9].Item1 == 7 && route[9].Item2 == 4);
      Assert.IsTrue(route[10].Item1 == 7 && route[10].Item2 == 3);
      Assert.IsTrue(route[11].Item1 == 7 && route[11].Item2 == 2);
      Assert.IsTrue(route[12].Item1 == 7 && route[12].Item2 == 1);

    }
  }
}
