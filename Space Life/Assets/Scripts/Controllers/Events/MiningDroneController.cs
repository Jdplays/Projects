using Animation;
using Scheduler;
using System;
using UnityEngine;

public class MiningDroneController : MonoBehaviour
{
    public Vector3 LeavingCoordinates;
    public Vector3 LandingCoordinates;
    public float Speed;
    public float DestinationReachedThreshold = 0.1f;
    public bool DestinationReached;
    public bool DroppedOff;
    public Drone Drone;
    public SpritenameAnimation AnimationIdle;
    public SpritenameAnimation AnimationFlying;
    public SpriteRenderer Renderer;
    public NestedObject parentObject;

    private ScheduledEvent DroneRefuelEvent;


    public void FixedUpdate()
    {
        if (WorldController.Instance.IsPaused)
        {
            return;
        }

        Vector3 destination = LandingCoordinates;

        if (DestinationReached && !DroppedOff)
        {
            return;
        }

        if (DroppedOff)
        {
            destination = LeavingCoordinates;
        }

        float distance = Vector3.Distance(transform.position, destination);

        if (distance > DestinationReachedThreshold * TimeManager.Instance.TimeScale)
        {
            // rotate the model
            Vector3 vectorToTarget = destination - transform.position;
            float angle = (Mathf.Atan2(vectorToTarget.y, vectorToTarget.x) * Mathf.Rad2Deg) - 90;
            Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * Speed * TimeManager.Instance.TimeScale);

            // Direction to the next waypoint
            Vector3 dir = (destination - transform.position).normalized;
            dir *= Speed * Time.fixedDeltaTime * TimeManager.Instance.TimeScale;

            transform.position = transform.position + dir;
            AnimationFlying.Update(Time.fixedDeltaTime);
            ShowSprite(AnimationFlying.CurrentFrameName);
        }
        else
        {
            DestinationReached = true;
            if (DroppedOff)
            {
                WorldController.Instance.DroneController.ReturningToPlanet(parentObject);
                Destroy(this.gameObject);
            }
            else
            {
                WorldController.Instance.DroneController.DropOffItems(parentObject, LandingCoordinates);
                Refueling(parentObject);
                AnimationIdle.Update(Time.fixedDeltaTime);
                ShowSprite(AnimationIdle.CurrentFrameName);
            }
        }
    }

    public void Refueling(NestedObject parentObject)
    {
        parentObject.Status = "Refueling";

        Debug.Log("Refueling");
        DroneRefuelEvent = new ScheduledEvent(
           "EvaluateRefuelDrone",
           EvaluateRefuelDrone,
           (int)TimeSpan.FromMinutes(0.5).TotalSeconds,
           false,
           parentObject);

        Scheduler.Scheduler.Current.RegisterEvent(DroneRefuelEvent);

    }

    private void EvaluateRefuelDrone(ScheduledEvent scheduledEvent)
    {
        Debug.Log("EvaluateRefuelDrone");
        parentObject.Status = "Transporting";
        DroppedOff = true;
    }

    private void ShowSprite(string spriteName)
    {
        if (Renderer != null)
        {
            Renderer.sprite = SpriteManager.GetSprite("Drone", spriteName);
        }
    }
}
