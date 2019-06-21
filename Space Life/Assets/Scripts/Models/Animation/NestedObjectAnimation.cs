using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animation
{
    /// <summary>
    /// Animations for NestedObject. Can have several "states" that can be switched using SetState.
    /// </summary>
    public class NestedObjectAnimation
    {
        // current shown frame
        private int prevFrameIndex;

        // Holds the actual animation sprites from the spritesheet
        private Sprite[] sprites;

        // Collection of animations
        private Dictionary<string, SpritenameAnimation> animations;
        private SpritenameAnimation currentAnimation;
        private string currentAnimationState;

        public NestedObjectAnimation()
        {            
            animations = new Dictionary<string, SpritenameAnimation>();
        }

        public SpriteRenderer Renderer { get; set; }

        public NestedObjectAnimation Clone()
        {
            NestedObjectAnimation newFA = new NestedObjectAnimation();
            newFA.sprites = sprites;
            newFA.animations = new Dictionary<string, SpritenameAnimation>();

            foreach (KeyValuePair<string, SpritenameAnimation> entry in animations)
            {
                newFA.animations.Add(entry.Key, (SpritenameAnimation)entry.Value.Clone());
            }

            newFA.currentAnimationState = currentAnimationState;
            newFA.currentAnimation = newFA.animations[currentAnimationState];
            newFA.prevFrameIndex = 0;
            return newFA;
        }

        public void Update(float deltaTime)
        {
            currentAnimation.Update(deltaTime);
            CheckFrameChange();
        }

        /// <summary>
        /// NestedObject has changed, so make sure the sprite is updated.
        /// </summary>
        public void OnNestedObjectChanged()
        {
            ShowSprite(GetSpriteName());
        }

        /// <summary>
        /// Set the animation frame depending on a value. The currentvalue percent of the maxvalue will determine which frame is shown.
        /// </summary>
        public void SetProgressValue(float percent)
        {
            currentAnimation.SetProgressValue(percent);
            CheckFrameChange();
        }

        /// <summary>
        /// Set the animation state. Will only have an effect if stateName is different from current animation stateName.
        /// </summary>
        public void SetState(string stateName)
        {
            if (animations.ContainsKey(stateName) == false)
            {
                return;
            }

            if (stateName == currentAnimationState)
            {
                return;
            }

            currentAnimationState = stateName;
            currentAnimation = animations[currentAnimationState];
            currentAnimation.Play();
            ShowSprite(currentAnimation.CurrentFrameName);                       
        }

        /// <summary>
        /// Get spritename from the current animation.
        /// </summary>
        public string GetSpriteName()
        {            
            return currentAnimation.CurrentFrameName;
        }

        /// <summary>
        /// Add animation to NestedObject. First animation added will be default for sprites e.g. ghost image when placing NestedObject.
        /// </summary>
        public void AddAnimation(string state, List<string> spriteNames, float fps, bool looping, bool valueBased)
        {
            animations.Add(state, new SpritenameAnimation(state, spriteNames.ToArray(), 1 / fps, looping, false, valueBased));

            // set default state to first state entered - most likely "idle"
            if (string.IsNullOrEmpty(currentAnimationState))
            {
                currentAnimationState = state;
                currentAnimation = animations[currentAnimationState];
                prevFrameIndex = 0;
            }
        }

        // check if time or value requires us to show a new animationframe
        private void CheckFrameChange()
        {
            if (prevFrameIndex != currentAnimation.CurrentFrame)
            {
                ShowSprite(currentAnimation.CurrentFrameName);
                prevFrameIndex = currentAnimation.CurrentFrame;
            }
        }
        
        private void ShowSprite(string spriteName)
        {
            if (Renderer != null)
            {
                Renderer.sprite = SpriteManager.GetSprite("NestedObject", spriteName);
            }
        }
    }
}