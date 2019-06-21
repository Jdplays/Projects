using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceLife.PowerNetwork
{
    public class PowerNetwork
    {
        private readonly HashSet<Grid> powerGrids;
        private readonly float secondsToTick = 1.0f;
        private float secondsPassed;

        public PowerNetwork()
        {
            powerGrids = new HashSet<Grid>();
        }

        public bool IsEmpty
        {
            get { return powerGrids.Count == 0; }
        }

        public bool CanPlugIn(Connection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            return powerGrids.Any(grid => grid.CanPlugIn(connection));
        }

        public bool PlugIn(Connection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (IsEmpty)
            {
                powerGrids.Add(new Grid());
            }

            Grid powerGrid = powerGrids.FirstOrDefault(grid => grid.CanPlugIn(connection));
            return PlugIn(connection, powerGrid);
        }

        public bool PlugIn(Connection connection, Grid grid)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            return grid != null && grid.PlugIn(connection);
        }

        public bool IsPluggedIn(Connection connection, out Grid grid)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (IsEmpty)
            {
                grid = null;
                return false;
            }

            grid = powerGrids.FirstOrDefault(powerGrid => powerGrid.IsPluggedIn(connection));
            return grid != null;
        }

        public void Unplug(Connection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            Grid grid;
            IsPluggedIn(connection, out grid);
            if (grid == null)
            {
                return;
            }

            Unplug(connection, grid);
        }

        public void Unplug(Connection connection, Grid grid)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (grid == null)
            {
                throw new ArgumentNullException("grid");
            }

            grid.Unplug(connection);
        }

        public bool HasPower(Connection connection)
        {
            Grid grid;
            IsPluggedIn(connection, out grid);
            return grid != null && grid.IsOperating;
        }

        public void Update(float deltaTime)
        {
            secondsPassed += deltaTime;
            if (secondsPassed < secondsToTick)
            {
                return;
            }

            secondsPassed = 0.0f;
            Tick();
        }

        private void Tick()
        {
            if (IsEmpty)
            {
                return;
            }

            powerGrids.RemoveWhere(grid => grid.IsEmpty);
            foreach (Grid powerGrid in powerGrids)
            {
                powerGrid.Tick();
            }
        }
    }
}
