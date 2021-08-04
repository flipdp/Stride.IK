using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace IKTesting
{
    /// <summary>
    /// A script that allows to move and rotate an entity through keyboard, mouse and touch input to provide basic camera navigation.
    /// </summary>
    /// <remarks>
    /// The entity can be moved using W, A, S, D, Q and E, arrow keys, a gamepad's left stick or dragging/scaling using multi-touch.
    /// Rotation is achieved using the Numpad, the mouse while holding the right mouse button, a gamepad's right stick, or dragging using single-touch.
    /// </remarks>
    public class TargetMover : SyncScript
    {
        private const float MaximumPitch = MathUtil.PiOverTwo * 0.99f;

        private Vector3 upVector;
        private Vector3 translation;
        private float yaw;
        private float pitch;

        public Vector3 KeyboardMovementSpeed { get; set; } = new Vector3(5.0f);

        public float SpeedFactor { get; set; } = 5.0f;

        public Vector2 KeyboardRotationSpeed { get; set; } = new Vector2(3.0f);

        public Vector2 MouseRotationSpeed { get; set; } = new Vector2(1.0f, 1.0f);

        public override void Start()
        {
            base.Start();

            // Default up-direction
            upVector = Vector3.UnitY;
        }

        public override void Update()
        {
            DebugText.Print(Entity.Transform.Position.ToString(), Int2.One * 10);
            ProcessInput();
            UpdateTransform();
        }

        private void ProcessInput()
        {
            float deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            translation = Vector3.Zero;
            yaw = 0f;
            pitch = 0f;

            // Keyboard and Gamepad based movement
            {
                // Our base speed is: one unit per second:
                //    deltaTime contains the duration of the previous frame, let's say that in this update
                //    or frame it is equal to 1/60, that means that the previous update ran 1/60 of a second ago
                //    and the next will, in most cases, run in around 1/60 of a second from now. Knowing that,
                //    we can move 1/60 of a unit on this frame so that in around 60 frames(1 second)
                //    we will have travelled one whole unit in a second.
                //    If you don't use deltaTime your speed will be dependant on the amount of frames rendered
                //    on screen which often are inconsistent, meaning that if the player has performance issues,
                //    this entity will move around slower.
                float speed = 1f * deltaTime;

                Vector3 dir = Vector3.Zero;

                if (Input.HasKeyboard)
                {
                    // Move with keyboard
                    // Forward/Backward
                    if (Input.IsKeyDown(Keys.Up))
                    {
                        dir.Z += 1;
                    }
                    if (Input.IsKeyDown(Keys.Down))
                    {
                        dir.Z -= 1;
                    }

                    // Left/Right
                    if (Input.IsKeyDown(Keys.Left))
                    {
                        dir.X -= 1;
                    }
                    if (Input.IsKeyDown(Keys.Right))
                    {
                        dir.X += 1;
                    }

                    // Down/Up
                    if (Input.IsKeyDown(Keys.Y))
                    {
                        dir.Y -= 1;
                    }
                    if (Input.IsKeyDown(Keys.X))
                    {
                        dir.Y += 1;
                    }

                    // If the player pushes down two or more buttons, the direction and ultimately the base speed
                    // will be greater than one (vector(1, 1) is farther away from zero than vector(0, 1)),
                    // normalizing the vector ensures that whichever direction the player chooses, that direction
                    // will always be at most one unit in length.
                    // We're keeping dir as is if isn't longer than one to retain sub unit movement:
                    // a stick not entirely pushed forward should make the entity move slower.
                    if (dir.Length() > 1f)
                    {
                        dir = Vector3.Normalize(dir);
                    }
                }

                // Finally, push all of that to the translation variable which will be used within UpdateTransform()
                translation += dir * KeyboardMovementSpeed * speed;
            }
            
            // Keyboard and Gamepad based Rotation
            {
                // See Keyboard & Gamepad translation's deltaTime usage
                float speed = 1f * deltaTime;
                Vector2 rotation = Vector2.Zero;

                if (Input.HasKeyboard)
                {
                    if (Input.IsKeyDown(Keys.NumPad2))
                    {
                        rotation.X += 1;
                    }
                    if (Input.IsKeyDown(Keys.NumPad8))
                    {
                        rotation.X -= 1;
                    }

                    if (Input.IsKeyDown(Keys.NumPad4))
                    {
                        rotation.Y += 1;
                    }
                    if (Input.IsKeyDown(Keys.NumPad6))
                    {
                        rotation.Y -= 1;
                    }

                    // See Keyboard & Gamepad translation's Normalize() usage
                    if (rotation.Length() > 1f)
                    {
                        rotation = Vector2.Normalize(rotation);
                    }
                }

                // Modulate by speed
                rotation *= KeyboardRotationSpeed * speed;

                // Finally, push all of that to pitch & yaw which are going to be used within UpdateTransform()
                pitch += rotation.X;
                yaw += rotation.Y;
            }
        }

        private void UpdateTransform()
        {
            // Get the local coordinate system
            var rotation = Matrix.RotationQuaternion(Entity.Transform.Rotation);

            // Enforce the global up-vector by adjusting the local x-axis
            var right = Vector3.Cross(rotation.Forward, upVector);
            var up = Vector3.Cross(right, rotation.Forward);

            // Stabilize
            right.Normalize();
            up.Normalize();

            // Adjust pitch. Prevent it from exceeding up and down facing. Stabilize edge cases.
            var currentPitch = MathUtil.PiOverTwo - (float)Math.Acos(Vector3.Dot(rotation.Forward, upVector));
            pitch = MathUtil.Clamp(currentPitch + pitch, -MaximumPitch, MaximumPitch) - currentPitch;

            Vector3 finalTranslation = translation;
            finalTranslation.Z = -finalTranslation.Z;
            finalTranslation = Vector3.TransformCoordinate(finalTranslation, rotation);

            // Move in local coordinates
            Entity.Transform.Position += finalTranslation;

            // Yaw around global up-vector, pitch and roll in local space
            Entity.Transform.Rotation *= Quaternion.RotationAxis(right, pitch) * Quaternion.RotationAxis(upVector, yaw);
        }
    }
}
