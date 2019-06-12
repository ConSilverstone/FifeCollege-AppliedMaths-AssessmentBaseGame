using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Assessment
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        int displaywidth = 800;
        int displayheight = 600;
        float aspectratio;
        object3d player = new object3d();
        object3d rock = new object3d();
        object3d bullet = new object3d();
        camera3d gamecam = new camera3d();
        directionalLightSource sunlight;
        Random randomiser = new Random();
        BoundingBox TriggerBoxDoorOpen;
        BoundingBox TriggerBoxRockFall;
        bool rockFalling = false;
        bool doorOpening = false;
        Vector3 acceleration = new Vector3();
        basicCuboid door;
        basicCuboid[] walls = new basicCuboid[20];
        float doorSequenceTimer;
        float doorSequenceFinalTime = 2500;

        // Task 2 Initalised variables //
        Vector3 acceleration_old = Vector3.Zero;
        Vector3 velocity_old = Vector3.Zero;
        Vector3 position_older = Vector3.Zero;
        Vector3 position_old = Vector3.Zero;
        // Task 4 Initalised variables //
        Vector3 initialPosition = Vector3.Zero;
        float fallStart = 0f;

        // Add a variable to keep track of when the rock started falling
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            graphics.PreferredBackBufferWidth = displaywidth;
            graphics.PreferredBackBufferHeight = displayheight;
            graphics.ApplyChanges();

            aspectratio = (float)displaywidth / (float)displayheight;
            gamecam.position = new Vector3(50, 50, 50);
            gamecam.target = new Vector3(0, 0, 0);
            gamecam.fieldOfView = MathHelper.ToRadians(90);
            gamecam.whichWayIsUp = Vector3.Up;
            gamecam.nearPlane = 1f;
            gamecam.farPlane = 50000f;

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            BoundingRenderer.InitializeGraphics(graphics.GraphicsDevice);
            spriteBatch = new SpriteBatch(GraphicsDevice);
            player.LoadModel(Content, "Ship");
            player.rotation = new Vector3(0f, 0f, 0f);
            player.position.X = 0;
            player.position.Y = 0;
            player.position.Z = 0;
            player.scale = 0.1f;
            rock.LoadModel(Content, "Meteor");
            rock.scale = 0.1f;
            rock.position = new Vector3(25, 60, -50);
            for (int c = 0; c < walls.Length; c++)
            {
                walls[c] = new basicCuboid(GraphicsDevice);
                walls[c].LoadContent(Content, "WallTexture");
                walls[c].scale = new Vector3(5, 30, 60);
                if (c < 5)
                    walls[c].SetUpVertices(new Vector3(-70, 0, 60 * (c + 1)));
                else if (c < 10)
                    walls[c].SetUpVertices(new Vector3(-70, 0, -60 * (c - 4)));
                else
                {
                    walls[c].scale = new Vector3(60, 30, 5);
                    walls[c].SetUpVertices(new Vector3(-70 + (c - 10) * 60, 0, -300));
                }
            }

            door = new basicCuboid(GraphicsDevice);
            door.LoadContent(Content, "WallTexture");
            door.scale = new Vector3(5, 30, 60);
            door.SetUpVertices(new Vector3(-70, 0, 0));
            TriggerBoxDoorOpen = new BoundingBox(new Vector3(-95, 0, 0), new Vector3(-
            45, 10, 60));
            TriggerBoxRockFall = new BoundingBox(new Vector3(-5, -5, -55), new
            Vector3(55, 5, -45));
            sunlight.diffuseColor = new Vector3(10);
            sunlight.specularColor = new Vector3(1f, 1f, 1f);
            sunlight.direction = Vector3.Normalize(new Vector3(1.5f, -1.5f, -1.5f));
        }

        public enum IntegrationMethod { ForwardEuler, LeapFrog, Verlet };
        IntegrationMethod currentIntegrationMethod = IntegrationMethod.ForwardEuler;

        private void MovePlayer(int dt)
        {
            switch (currentIntegrationMethod)
            {
                case IntegrationMethod.ForwardEuler:
                    //// This method is deprecated due to stability issues.
                    player.position += player.velocity * dt;
                    player.velocity += acceleration * dt;

                    break;

                // CODE FOR TASK 2 SHOULD BE ENTERED HERE ///

                case IntegrationMethod.LeapFrog:
                    // calculate the velocity at half way through the frame, using last frame's acceleration.
                    Vector3 velocity_half = velocity_old + (acceleration_old * dt) * 0.5f;
                    // calculate positions using this halfway velocity
                    player.position = position_old + velocity_half * dt;
                    // calculate the new velocity for this frame's acceleration and half a time step
                    player.velocity = velocity_half + (acceleration * dt) * 0.5f;

                    // Apply drag to the movement (friction)
                    player.velocity *= 0.99f;
                    break;
                case IntegrationMethod.Verlet:

                    // Update acceleration (friction)
                    if (position_older != position_old)
                    {
                        Vector3 drag = position_older - position_old;
                        drag.Normalize();
                        drag *= 0.0001f;
                        acceleration += drag;
                    }

                    player.position = 2 * position_old - position_older + acceleration * dt * dt;
                    break;
            }
            // Move current values to old values;
            acceleration_old = acceleration;
            velocity_old = player.velocity;
            position_older = position_old;
            position_old = player.position;
        }


        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            int dt = gameTime.ElapsedGameTime.Milliseconds;
            base.Update(gameTime);
            player.storedPos = player.position;
            Vector3 storedAcc = acceleration;
            acceleration = new Vector3(0, 0, 0);
            if (Keyboard.GetState().IsKeyDown(Keys.Left)) player.rotation.Y += 0.1f;
            if (Keyboard.GetState().IsKeyDown(Keys.Right)) player.rotation.Y -= 0.1f;
            player.velocity *= 0.9f; // friction
            if (Keyboard.GetState().IsKeyDown(Keys.Up))
            {
                acceleration.X = (float)Math.Sin(player.rotation.Y) * -0.001f;
                acceleration.Z = (float)Math.Cos(player.rotation.Y) * -0.001f;
            }
            // camera follow
            gamecam.position = new Vector3(50, 50, 50) + player.position;
            gamecam.target = player.position;
            MovePlayer(dt);
            foreach (basicCuboid WallSegment in walls)
            {
                if (player.hitBox.Intersects(WallSegment.collisionbox))
                {
                    ElasticCollision(WallSegment);
                }
            }
            if (player.hitBox.Intersects(door.collisionbox))
            {
                ElasticCollision(door);
            }
            if (player.hitBox.Intersects(TriggerBoxRockFall) && !rockFalling)
            {
                rockFalling = true;
                // Assign rock fall start time
                fallStart = (float)gameTime.TotalGameTime.TotalSeconds;
            }
            if (rockFalling)
            {
                Vector3 gravity = new Vector3(0, -10f, 0);
                ////////////////////////////////////////////
                // CODE FOR TASK 4 SHOULD BE ENTERED HERE //
                ////////////////////////////////////////////
                initialPosition = new Vector3(25, 60, -50);

                float timeSincefallStart = (float)gameTime.TotalGameTime.TotalSeconds - fallStart;

                // Calculate time since rocks started falling
                // Calculate rock's new y position using the derived equation
                rock.position.Y = gravity.Y * timeSincefallStart * timeSincefallStart / 2f + initialPosition.Y;

                if (rock.position.Y < 0f)
                {
                    // Stop when the rock reaches the ground (0)
                    rock.position.Y = 0;
                    fallStart = 0f;
                }
            }
            if (player.hitBox.Intersects(TriggerBoxDoorOpen))
            {
                doorOpening = true;
            }
            if (doorOpening)
            {
                Vector3 newPos = new Vector3();
                Vector3 doorStartPoint = new Vector3(-70, 0, 0);
                Vector3 doorEndPoint = new Vector3(-70, 30, 0);

                ////////////////////////////////////////////
                // CODE FOR TASK 5 SHOULD BE ENTERED HERE //
                ////////////////////////////////////////////

                doorSequenceTimer += (float)gameTime.ElapsedGameTime.Milliseconds;
                if (doorSequenceTimer >= doorSequenceFinalTime)
                {
                    // Now the animation is done so we need to set it up
                    // so that I can run in the other direction
                    doorSequenceTimer = doorSequenceFinalTime;
                }
                newPos = QuadEaseOut((float)doorSequenceTimer, (float)doorSequenceFinalTime, doorStartPoint, doorEndPoint);
                    door.SetUpVertices(newPos);
            }


            base.Update(gameTime);
        }

        private void ElasticCollision(basicCuboid w)
        {
            /////////////////////
            // CODE FOR TASK 7 //
            /////////////////////
            // Need the perpendicular vector to the face of the box we hit
            // To do this, we need TWO vectors ON the face of the box we hit
            Vector3 faceVector1;
            Vector3 faceVector2;

            // Get the corner of the box we hit so that we can calculate the face vectors
            Vector3[] corners = w.collisionbox.GetCorners();
            // returns the corners of the box faces that are perpendicular to the z axis (facing along the z axis)
            // 0-3 is the near, 4-7 is the far face
            // Start upper left, then upper right, then lower right, then lower left (clockwise)

            // Move back our player to their previous position (so they aren't inside the box)
            player.position = player.storedPos;

            // If the player's new position overlappin in the X direction
            if ((player.hitBox.Min.X - player.velocity.X) > w.collisionbox.Max.X || (player.hitBox.Max.X - player.velocity.X) < w.collisionbox.Min.X)
            {
                // Overlapping from right or left!
                // Line from back bottom right going to the front top right
                faceVector1 = corners[1] - corners[6];
                // line from back bottom right going to the front bottom right
                faceVector2 = corners[2] - corners[6];
            }
            else // If we are not overlapping right or left, we are overlapping front/back (z axis)
            {
                // Overlapping front or back!
                // Line from front top left going to the front top right
                faceVector1 = corners[1] - corners[0];
                // line from front top left going to the front bottom right
                faceVector2 = corners[2] - corners[0];
            }
            // (we ignore the possibility of a y direction

            // Get a cross product between these two vectors to define a normal perpendicular to the plane (face)
            Vector3 normal = Vector3.Cross(faceVector1, faceVector2);
            // Make it a unit vector (length 1)
            normal.Normalize();

            // Use this normal vector to reflect the player's velocity
            // (this uses a dot product equation internally)
            player.velocity = Vector3.Reflect(player.velocity, normal);
        }

        ////////////////////////////////////////////
        // CODE FOR TASK 6 SHOULD BE ENTERED HERE //
        ////////////////////////////////////////////

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // I decided not to use CubicInterpolation, instead used QuadEaseOut at the bottom of game1.cs //
        /////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector3 CubicInterpolation(Vector3 initialPos, Vector3 endPos, float
        time)
        {
            return new Vector3(0, 0, 0);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            spriteBatch.End();
            graphics.GraphicsDevice.BlendState = BlendState.Opaque; // set up 3d rendering so its not transparent
            graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            player.Draw(gamecam, sunlight);
            rock.Draw(gamecam, sunlight);
            door.Draw(gamecam.ViewMatrix(), gamecam.ProjectionMatrix());
            foreach (basicCuboid w in walls)
                w.Draw(gamecam.ViewMatrix(), gamecam.ProjectionMatrix());

            bullet.Draw(gamecam, sunlight);
            BoundingRenderer.RenderBox(player.hitBox, gamecam.ViewMatrix(),
            gamecam.ProjectionMatrix(), Color.White);
            BoundingRenderer.RenderBox(rock.hitBox, gamecam.ViewMatrix(),
            gamecam.ProjectionMatrix(), Color.White);
            BoundingRenderer.RenderBox(TriggerBoxDoorOpen, gamecam.ViewMatrix(),
            gamecam.ProjectionMatrix(), player.hitBox.Intersects(TriggerBoxDoorOpen) ? Color.White
            : Color.CornflowerBlue);
            BoundingRenderer.RenderBox(TriggerBoxRockFall, gamecam.ViewMatrix(),
            gamecam.ProjectionMatrix(), player.hitBox.Intersects(TriggerBoxRockFall) ? Color.White
            : Color.CornflowerBlue);
            BoundingRenderer.RenderBox(door.collisionbox, gamecam.ViewMatrix(),
            gamecam.ProjectionMatrix(), player.hitBox.Intersects(TriggerBoxRockFall) ? Color.White
            : Color.CornflowerBlue);

            base.Draw(gameTime);
        }

        // For the door movement we need to build an easing function
        private Vector3 QuadEaseOut(float time, float duration, Vector3 startPoint, Vector3 endPoint)
        {
            // We need to calculate the independant variable time as a proportion of the time passed to the total duration
            float t = time / duration;

            // Calculate the position akaa distance traveled from start
            // Using a derived quadratic equation
            // Producing a fraction of total distance which will be our scaling factor
            float p = -1f * t * t + 2f * t;

            // Determine total distance 
            Vector3 totalDistance = endPoint - startPoint;

            // Determine duration traveled by scaling total distance to scale factor (p)
            Vector3 distanceTraveled = totalDistance * p;

            // Determine new pos by adding distance traveled to start point
            Vector3 newPosition = startPoint + distanceTraveled;

            return newPosition;
        }
    }
}
