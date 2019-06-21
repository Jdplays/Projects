using System.Collections.Generic;
using System.Xml;

public class GameEventManager
{
    private List<GameEvent> currentEvents;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameEventManager"/> class.
    /// </summary>
    public GameEventManager()
    {
        currentEvents = new List<GameEvent>();

        GameEvent initialEvent = PrototypeManager.GameEvent[0].Clone();
        currentEvents.Add(initialEvent);
    }

    /// <summary>
    /// Update the current game events.
    /// </summary>
    /// <param name="deltaTIme">Delta time.</param>
    public void Update(float deltaTime)
    {
        foreach (GameEvent gameEvent in currentEvents)
        {
            gameEvent.Update(deltaTime);
        }
    }
}
