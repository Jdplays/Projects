using System.Collections;
using UnityEngine;

public class SoundController
{
    private float soundCooldown = 0;

    // Use this for initialization
    public SoundController(World world)
    {
        world.NestedObjectManager.Created += OnNestedObjectCreated;
        world.OnTileChanged += OnTileChanged;

        TimeManager.Instance.EveryFrame += Update;
    }
    
    // Update is called once per frame
    public void Update(float deltaTime)
    {
        soundCooldown -= deltaTime;
    }

    public void OnNestedObjectCreated(NestedObject nestedObject)
    {
        // FIXME
        if (soundCooldown > 0)
        {
            return;
        }

        AudioClip ac = AudioManager.GetAudio("Sound", nestedObject.Type + "_OnCreated");
        AudioSource.PlayClipAtPoint(ac, Camera.main.transform.position);
        soundCooldown = 0.1f;
    }

    private void OnTileChanged(Tile tileData)
    {
        // FIXME
        if (soundCooldown > 0)
        {
            return;
        }

        if (tileData.ForceTileUpdate)
        {  
            AudioClip ac = AudioManager.GetAudio("Sound", "Floor_OnCreated");
            AudioSource.PlayClipAtPoint(ac, Camera.main.transform.position);
            soundCooldown = 0.1f;
        }
    }
}
