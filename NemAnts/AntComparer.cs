using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NemAnts {
  public class AntComparer : IEqualityComparer<Ant> {
    public bool Equals(Ant x, Ant y) {
      if (x.Owner == y.Owner && x.Row == y.Row && x.Col == y.Col)
        return true;

      return false;
    }

    public int GetHashCode(Ant obj) {
      int hash = 0;

      unchecked {
        hash += obj.Owner.GetHashCode() * 2;
        hash += obj.Row.GetHashCode() * 3;
        hash += obj.Col.GetHashCode() * 5;
      }

      return hash;
    }
  }
}
