public class ShipStorage
{
    public ShipStorage(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }

    /// <summary>
    /// The relative X position of this storage within the ship.
    /// </summary>
    public int X { get; private set; }

    /// <summary>
    /// The relative Y position of this storage within the ship.
    /// </summary>
    public int Y { get; private set; }
}