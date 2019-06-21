using Random = UnityEngine.Random;

namespace SpaceLife.State
{
    [System.Diagnostics.DebuggerDisplay("Idle: ")]
    public class IdleState : State
    {
        private float totalIdleTime;
        private float timeSpentIdle;

        public IdleState(Character character, State nextState = null)
            : base("Idle", character, nextState)
        {
            timeSpentIdle = 0f;
            totalIdleTime = Random.Range(0.2f, 2.0f);
        }

        public override void Update(float deltaTime)
        {
            timeSpentIdle += deltaTime;
            if (timeSpentIdle >= totalIdleTime)
            {
                // We are done. Lets look for work.
                character.SetState(null);
            }
        }
    }
}