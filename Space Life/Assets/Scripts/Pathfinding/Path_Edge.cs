using System.Collections;
using UnityEngine;

public class Path_Edge<T>
{
    // Cost to traverse this edge (i.e. cost to ENTER the tile)
    public Tile tile;

    public float cost;

    public Path_Node<T> node;
}
