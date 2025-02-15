﻿using System;
using System.Collections.Generic;
using System.Linq;
using SpaceLife.Pathfinding;

namespace SpaceLife.State
{
    [System.Diagnostics.DebuggerDisplay("JobState: {job}")]
    public class JobState : State
    {
        private bool jobFinished = false;

        public JobState(Character character, Job job, State nextState = null)
            : base("Job", character, nextState)
        {
            this.Job = job;

            job.OnJobCompleted += OnJobCompleted;
            job.OnJobStopped += OnJobStopped;
            job.IsBeingWorked = true;

            DebugLog("created {0}", job.JobObjectType ?? "Unnamed Job");
        }

        public Job Job { get; private set; }

        public override void Update(float deltaTime)
        {
            if (jobFinished)
            {
                DebugLog(" - Update called on a finished job");
                Finished();
                return;
            }

            // If we are lacking material, then go deliver materials
            if (Job.MaterialNeedsMet() == false)
            {
                if (Job.IsRequiredInventoriesAvailable() == false)
                {
                    AbandonJob();
                    Finished();
                    return;
                }

                DebugLog(" - Next action: Haul material");
                character.SetState(new HaulState(character, Job, this));
            }
            else if (Job.IsTileAtJobSite(character.CurrTile) == false)
            {
                DebugLog(" - Next action: Go to job");
                List<Tile> path = Pathfinder.FindPathToTile(character.CurrTile, Job.tile, Job.adjacent);
                if (path != null && path.Count > 0)
                {
                    character.SetState(new MoveState(character, Job.IsTileAtJobSite, path, this));
                }
                else
                {
                    Interrupt();
                }
            }
            else
            {
                DebugLog(" - Next action: Work");

                if (Job.tile != character.CurrTile)
                {
                    // We aren't standing on the job spot itself, so make sure to face it.
                    character.FaceTile(Job.tile);
                }

                Job.DoWork(deltaTime);
            }
        }

        public override void Interrupt()
        {
            // If we still have a reference to a job, then someone else is stealing the state and we should put it back on the queue.
            if (Job != null)
            {
                AbandonJob();
            }

            base.Interrupt();
        }

        private void AbandonJob()
        {
            DebugLog(" - Job abandoned!");
            Debug.ULogChannel("Character", character.GetName() + " abandoned their job.");

            Job.OnJobCompleted -= OnJobCompleted;
            Job.OnJobStopped -= OnJobStopped;
            Job.IsBeingWorked = false;

            // Tell anyone else who cares that it was cancelled
            Job.CancelJob();

            if (Job.IsNeed)
            {
                return;
            }

            // Drops the priority a level.
            Job.DropPriority();

            // If the job gets abandoned because of pathing issues or something else, just return it to the queue
            World.Current.jobQueue.Enqueue(Job);
        }

        private void OnJobStopped(Job stoppedJob)
        {
            DebugLog(" - Job stopped");

            jobFinished = true;

            // Job completed (if non-repeating) or was cancelled.
            stoppedJob.OnJobCompleted -= OnJobCompleted;
            stoppedJob.OnJobStopped -= OnJobStopped;
            Job.IsBeingWorked = false;

            if (Job != stoppedJob)
            {
                Debug.ULogErrorChannel("Character", "Character being told about job that isn't his. You forgot to unregister something.");
                return;
            }
        }

        private void OnJobCompleted(Job finishedJob)
        {
            DebugLog(" - Job finished");

            jobFinished = true;

            finishedJob.OnJobCompleted -= OnJobCompleted;
            finishedJob.OnJobStopped -= OnJobStopped;

            if (Job != finishedJob)
            {
                Debug.ULogErrorChannel("Character", "Character being told about job that isn't his. You forgot to unregister something.");
                return;
            }
        }
    }
}
