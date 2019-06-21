using System;
using UnityEngine;

public static class ImageUtils
{
    public static Vector3 SpritePivotOffset(Sprite sprite, float rotation = 0f)
    {
        Vector3 offset;

        if (Math.Abs(rotation) == 90 || Math.Abs(rotation) == 270)
        {
            offset = new Vector3((sprite.pivot.y / sprite.pixelsPerUnit) - 0.5f, (sprite.pivot.x / sprite.pixelsPerUnit) - 0.5f, 0);
        }
        else
        {
            offset = new Vector3((sprite.pivot.x / sprite.pixelsPerUnit) - 0.5f, (sprite.pivot.y / sprite.pixelsPerUnit) - 0.5f, 0);
        }

        return offset;
    }
}
