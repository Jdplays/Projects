using System.Collections.Generic;
using UnityEngine;

public class JobSpriteController : BaseSpriteController<Job>
{
    // This bare-bones controller is mostly just going to piggyback
    // on NestedObjectSpriteController because we don't yet fully know
    // what our job system is going to look like in the end.
    private NestedObjectSpriteController nosc;
    private UtilitySpriteController usc;

    // Use this for initialization
    public JobSpriteController(World world, NestedObjectSpriteController nestedObjectSpriteController, UtilitySpriteController utilitySpriteController)
        : base(world, "Jobs")
    {
        nosc = nestedObjectSpriteController;
        usc = utilitySpriteController;
        world.jobQueue.OnJobCreated += OnCreated;

        foreach (Job job in world.jobQueue.PeekAllJobs())
        {
            OnCreated(job);
        }

        foreach (Character character in world.CharacterManager)
        {
            if (character.MyJob != null)
            {
                OnCreated(character.MyJob);
            }
        }
    }

    public override void RemoveAll()
    {
        world.jobQueue.OnJobCreated -= OnCreated;

        foreach (Job job in world.jobQueue.PeekAllJobs())
        {
            job.OnJobCompleted -= OnRemoved;
            job.OnJobStopped -= OnRemoved;
        }

        foreach (Character character in world.CharacterManager)
        {
            if (character.MyJob != null)
            {
                character.MyJob.OnJobCompleted -= OnRemoved;
                character.MyJob.OnJobStopped -= OnRemoved;
            }
        }

        base.RemoveAll();
    }

    protected override void OnCreated(Job job)
    {
        if (job.JobTileType == null && job.JobObjectType == null)
        {
            // This job doesn't really have an associated sprite with it, so no need to render.
            return;
        }

        // FIXME: We can only do NestedObject-building jobs.
        // TODO: Sprite
        if (objectGameObjectMap.ContainsKey(job))
        {
            return;
        }

        GameObject job_go = new GameObject();

        // Add our tile/GO pair to the dictionary.
        objectGameObjectMap.Add(job, job_go);

        job_go.name = "JOB_" + job.JobObjectType + "_" + job.tile.X + "_" + job.tile.Y + "_" + job.tile.Z;
        job_go.transform.SetParent(objectParent.transform, true);

        SpriteRenderer sr = job_go.AddComponent<SpriteRenderer>();
        if (job.JobTileType != null)
        {
            // This job is for building a tile.
            // For now, the only tile that could be is the floor, so just show a floor sprite
            // until the graphics system for tiles is fleshed out further.
            job_go.transform.position = job.tile.Vector3;
            sr.sprite = SpriteManager.GetSprite("Tile", "Solid");
            sr.color = new Color32(128, 255, 128, 192);
        }
        else if (job.JobDescription.Contains("Deconstructing"))
        {
            sr.sprite = SpriteManager.GetSprite("UI", "CursorCircle");
            sr.color = Color.red;
            job_go.transform.position = job.tile.Vector3;
        }
        else if (job.JobDescription.Contains("Mining"))
        {
            sr.sprite = SpriteManager.GetSprite("UI", "MiningIcon");
            sr.color = new Color(1, 1, 1, 0.25f);
            job_go.transform.position = job.tile.Vector3;
        }
        else
        {
            // If we get this far we need a buildable prototype, bail if we don't have one
            if (job.buildablePrototype == null)
            {
                return;
            }

            // This is a normal NestedObject job.
            if (job.buildablePrototype.GetType().ToString() == "NestedObject")
            {
                NestedObject nestedObjectToBuild = (NestedObject)job.buildablePrototype;
                sr.sprite = nosc.GetSpriteForNestedObject(job.JobObjectType);
                job_go.transform.position = job.tile.Vector3 + ImageUtils.SpritePivotOffset(sr.sprite, nestedObjectToBuild.Rotation);
                job_go.transform.Rotate(0, 0, nestedObjectToBuild.Rotation);
            }
            else if (job.buildablePrototype.GetType().ToString() == "Utility")
            {
                sr.sprite = usc.GetSpriteForUtility(job.JobObjectType);
                job_go.transform.position = job.tile.Vector3 + ImageUtils.SpritePivotOffset(sr.sprite);
            }

            sr.color = new Color32(128, 255, 128, 64);
        }

        sr.sortingLayerName = "Jobs";

        // FIXME: This hardcoding is not ideal!  <== Understatement
        if (job.JobObjectType == "Door")
        {
            // By default, the door graphic is meant for walls to the east & west
            // Check to see if we actually have a wall north/south, and if so
            // then rotate this GO by 90 degrees
            Tile northTile = world.GetTileAt(job.tile.X, job.tile.Y + 1, job.tile.Z);
            Tile southTile = world.GetTileAt(job.tile.X, job.tile.Y - 1, job.tile.Z);

            if (northTile != null && southTile != null && northTile.NestedObject != null && southTile.NestedObject != null &&
                northTile.NestedObject.HasTypeTag("Wall") && southTile.NestedObject.HasTypeTag("Wall"))
            {
                job_go.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }

        job.OnJobCompleted += OnRemoved;
        job.OnJobStopped += OnRemoved;
    }

    protected override void OnChanged(Job job)
    {
    }

    protected override void OnRemoved(Job job)
    {
        // This executes whether a job was COMPLETED or CANCELLED
        job.OnJobCompleted -= OnRemoved;
        job.OnJobStopped -= OnRemoved;

        GameObject job_go = objectGameObjectMap[job];
        objectGameObjectMap.Remove(job);
        GameObject.Destroy(job_go);
    }
}
