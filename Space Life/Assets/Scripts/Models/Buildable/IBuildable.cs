using System.Collections;
using UnityEngine;

public interface IBuildable
{
    /// <summary>
    /// Gets the width of the buildable.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the height of the buildable.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the BASE tile of the buildable.
    /// </summary>
    /// <value>The BASE tile of the buildable.</value>
    Tile Tile { get; }

    /// <summary>
    /// Details if this is tasked for destruction.
    /// </summary>
    bool IsBeingDestroyed { get; }

    /// <summary>
    /// Checks whether the buildable has a certain tag.
    /// </summary>
    /// <param name="typeTag">Tag to check for.</param>
    /// <returns>True if buildable has specified tag.</returns>
    bool HasTypeTag(string typeTag);
}
