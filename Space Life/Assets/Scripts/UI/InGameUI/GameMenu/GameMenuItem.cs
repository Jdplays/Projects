using System;

public class GameMenuItem
{
    private Action callback;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainMenuItem"/> class.
    /// </summary>
    /// <param name="key">The menu item key.</param>
    /// <param name="actionCallback">The menu item callback. Called when is clicked.</param>
    public GameMenuItem(string key, Action actionCallback)
    {
        Key = key;
        callback = actionCallback;
    }

    /// <summary>
    /// Gets the menu item key. Also used for translations.
    /// </summary>
    /// <value>The menu item key.</value>
    public string Key { get; private set; }

    /// <summary>
    /// Calls the menu item callback.
    /// </summary>
    public void Trigger()
    {
        if (callback != null)
        {
            callback();
        }
    }
}
