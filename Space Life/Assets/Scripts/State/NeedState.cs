﻿namespace SpaceLife.State
{
    public class NeedState : State
    {
        public NeedState(Character character, State nextState = null)
            : base("Need", character, nextState)
        {
        }

        public override void Update(float deltaTime)
        {
            float needPercent = 0f;
            Need biggestNeed = null;

            foreach (Need need in character.Needs)
            {
                need.Update(deltaTime);
            }

            // At this point we want to do something about the need, but we let the current state finish first
            if (needPercent > 50 && needPercent < 100 && biggestNeed.RestoreNeedObj != null)
            {
                if (World.Current.NestedObjectManager.CountWithType(biggestNeed.RestoreNeedObj.Type) > 0)
                {
                    Job job = new Job(null, biggestNeed.RestoreNeedObj.Type, biggestNeed.CompleteJobNorm, biggestNeed.RestoreNeedTime, null, Job.JobPriority.High, false, true, false);
                    character.QueueState(new JobState(character, job));
                }
            }

            // We must do something immediately, drop what we are doing.
            if (needPercent == 100 && biggestNeed != null && biggestNeed.CompleteOnFail)
            {
                Job job = new Job(character.CurrTile, null, biggestNeed.CompleteJobCrit, biggestNeed.RestoreNeedTime * 10, null, Job.JobPriority.High, false, true, true);
                character.InterruptState();
                character.ClearStateQueue();
                character.SetState(new JobState(character, job));
            }
        }
    }
}