using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NemAnts {
  /// <summary>
  /// Allows comparison of ants to determine if they are the same ant.
  /// </summary>
  public class AntComparer : IEqualityComparer<Ant> {
    /// <summary>
    /// Equals function
    /// </summary>
    /// <param name="x">First ant to compare.</param>
    /// <param name="y">Second ant to compare.</param>
    /// <returns>True if both ants are in the same coordinate and have the same owner.</returns>
    public bool Equals(Ant x, Ant y) {
      if (x.Owner == y.Owner && x.Row == y.Row && x.Col == y.Col)
        return true;

      return false;
    }

    /// <summary>
    /// Gets a hash code for an ant.
    /// </summary>
    /// <param name="obj">The ant to return the hash code for.</param>
    /// <returns>An integer hash value which is gauranteed to be the same for ants that are deemed equal by the equals function.</returns>
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
