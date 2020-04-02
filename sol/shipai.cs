using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DBP_Framework
{
    public class ShipAI : BaseShip
    {
        public int totalMoves = 0;
        public int currentMove = 0;
        public float[] totalMoveTimers = {0.0f, 0.0f, 0.0f, 0.0f};
        public float currentMoveTimer = 0.0f;
        public bool firstInit = true;
        public float waveSegment = 0;
        public float rotationZ = 0.5f;
        public float rotationX = 0.0f;
        public bool facingSomething = false;

        // For basic movement:
        public Vector2 movingCurrVelocity = new Vector2(0.0f, 0.0f);
        public Vector2 movingTargetVelocity = new Vector2(0.0f, 0.0f);

        // For hovering movement:
        public float hoverTargetTime = 0.0f;
        public bool hoverTargetTimeSwitch = false;

        // For pushback movement:
        public float pushbackForce = 0.0f;

        // For avoidance movement:
        public float avoidanceTargetTimer = 0.0f;
        public float avoidanceCurrTimer = 0.0f;
        public float avoidanceVelocity = 0.0f;

        // For drifting movement:
        public float driftTimer = 0.0f;
        public Vector2 driftDirection = new Vector2(0.0f, 0.0f);
        public Vector2 driftVelocity = new Vector2(0.0f, 0.0f);

        // Used for some behaviours which target objects.
        public Vector3 enterVector = new Vector3(0.0f, 0.0f, 0.0f);
        public Vector3 exitVector = new Vector3(0.0f, 0.0f, 0.0f);

        // Used to 'fake' acceleration and deceleration in FaceTarget.
        public Vector3 faceVelocity = new Vector3(0.0f, 0.0f, 0.0f);
        public float fakeInertia = 0.0f;

        // Used for some behaviours to know whether to roll up or down.
        public bool rollingUp = false;

        //protected WeaponStats weaponStats = new WeaponStats();

        //public ShipAI()
        //{
            //m_Position = Vector3.Zero;
        //}
        
        public override void CreateNode()
        {
            base.CreateNode();

            // Slightly arc the models rotation to the screen, looks more 3D this way.
            m_Rotation *= Quaternion.CreateFromAxisAngle(m_Forward, -0.5f);

            weaponStats = new WeaponStats();

            // Randomise weapon cooldown.
            weaponStats.bullets.cooldown = (float)Global.Instance.rand.NextDouble() + 2.0f;
            weaponStats.bullets.cooldown += (0.2f - (float)Global.Instance.difficulty * 0.1f);   // Cooldown modifier by difficulty.
            weaponStats.bullets.countDown = weaponStats.bullets.cooldown;
            weaponStats.bullets.velocity2D *= (float)Global.Instance.difficulty * 0.5f;          // Speed modifier by difficulty.
            weaponStats.rockets.cooldown = (float)Global.Instance.rand.NextDouble() + 2.0f;
            weaponStats.rockets.cooldown += (0.2f - (float)Global.Instance.difficulty * 0.1f);   // Cooldown modifier by difficulty.
            weaponStats.rockets.countDown = weaponStats.rockets.cooldown;
            weaponStats.rockets.velocity2D *= (float)Global.Instance.difficulty * 0.5f;          // Speed modifier by difficulty.
            weaponStats.rockets.duration2D *= 2;
        }

        public override void UpdateNode(float DT)
        {
            // Keeps the bounding collision sphere always with the object
            bSphere.Center = m_globalTransformation.Translation;

            UpdateAI(DT);

            if (!facingSomething)
            {                 
                m_Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, rotationX) *
                             Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(180)) *
                             Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rotationZ);
            }
            
            base.UpdateNode(DT);
        }

        public override void DrawNode(float DT)
        {

        }

        public override void DrawSprites(float DT)
        {
            // Leave empty.
            base.DrawSprites(DT);
        }

        public override void DrawSpritesAdditive(float DT)
        {
            // Leave empty.
            base.DrawSpritesAdditive(DT);
        }

        public void UpdateAI(float DT)
        {   
            if (firstInit)
            {
                waveSegment = (float)wavePosition / (float)waveSize;
                initSpeed();
                UpdateTransform();
                AI_Functions.Instance.RandomiseHover(this);
            }

            // Set variables for specific wave AI.
            switch (waveBehaviour)
            {
                case SPAWN_BEHAVIOUR.STRAIGHT:
                    UpdateStraight(DT);
                    break;

                case SPAWN_BEHAVIOUR.ZIGZAGGED:
                    updateZigzagWave(DT);
                    break;

                case SPAWN_BEHAVIOUR.FORMATION:
                    if(firstInit)
                        initFormationDelay(1);
                    updateFormationWave(DT);
                    break;

                case SPAWN_BEHAVIOUR.SLOW_THEN_FAST:
                    if (firstInit)
                        initFormationDelay(1);
                    updateSlowThenFastWave(DT);
                    break;

                case SPAWN_BEHAVIOUR.IN_THEN_OUT:
                    if (firstInit)
                        initFormationDelay(1);//this probably isn't necessary now
                    UpdateInThenOut(DT);
                    break;

                case SPAWN_BEHAVIOUR.PATROLLING_EDGE:
                    UpdatePatrollingEdge(DT);
                    break;
                    
                case SPAWN_BEHAVIOUR.ASSAULT:
                    UpdateAssault(DT);
                    break;

                case SPAWN_BEHAVIOUR.COVERT:
                    UpdateCovert(DT);
                    break;

                case SPAWN_BEHAVIOUR.ROLLING_TOPBOTTOM:
                    UpdateRollingTopBottom(DT);
                    break;

                case SPAWN_BEHAVIOUR.TOPBOTTOM:
                    UpdateTopBottom(DT);
                    break;

                case SPAWN_BEHAVIOUR.STRAIGHT_AIMING:
                    UpdateStraightAiming(DT);
                    break;

                case SPAWN_BEHAVIOUR.MIDDLE_TOPBOTTOM:
                    UpdateMiddleTopBottom(DT);
                    break;

                case SPAWN_BEHAVIOUR.KAMIKAZE:
                    UpdateKamikaze(DT);
                    break;

                case SPAWN_BEHAVIOUR.STRAIGHT_SLOW:
                    UpdateStraightSlow(DT);
                    break;
            }

            // Limit Y-axis.
            if (waveFormation != SPAWN_STATE.SINGLE_FROM_SIDE && waveFormation != SPAWN_STATE.SINGLE_FROM_TOPBOTTOM)
            {
                if (m_Position.Y < -110)
                    m_Position.Y = -110;
                if (m_Position.Y > 110)
                    m_Position.Y = 110;
            }

            // Fix bounding error.
            if (waveFormation == SPAWN_STATE.SINGLE_FROM_SIDE || waveFormation == SPAWN_STATE.SINGLE_FROM_TOPBOTTOM)
            {
                if (currentMove != 0)
                {
                    if (waveBehaviour == SPAWN_BEHAVIOUR.ASSAULT || currentMove != (totalMoves - 1))
                    {
                        if (m_Position.Y < -110)
                            Yaxis.pushValue += DT * 2;
                        if (m_Position.Y > 110)
                            Yaxis.pushValue -= DT * 2;

                        // For the rare chance that highly mobile ship ends up too close to the left, just out of range.
                        if(m_Position.Y < -80 && m_Position.Z > -100)
                            Yaxis.pushValue += DT;
                        if (m_Position.Y > 80 && m_Position.Z > -100)
                            Yaxis.pushValue -= DT;
                    }
                }
            }
            
            rotationX = MathHelper.Clamp(rotationX, -1f, 1f);
            rotationX = MathHelper.Lerp(0.0f, rotationX, 0.97f);

            if (waveFormation != SPAWN_STATE.SINGLE_FROM_TOPBOTTOM)
            {
                rotationZ = MathHelper.Lerp(0.5f, rotationZ, 0.97f);
                rotationZ = MathHelper.Clamp(rotationZ, -0.25f, 1.3f);
            }
            
            m_Position.Y += Yaxis.baseSpeed;
            m_Position.Z += Zaxis.baseSpeed;
            Zaxis.Update(DT);
            Yaxis.Update(DT);
            currentMoveTimer += DT;
        }

        private void UpdateStraight(float DT)
        {
            if (firstInit)
            {
                // Warp-in effect.
                movingCurrVelocity.X = 5.0f;
                
                firstInit = false;
            }

            AI_Functions.Instance.HandleMovement(this, new Vector2(1.5f, 0.0f), 4.0f, DT);
        }

        private void updateFormationWave(float DT)
        {
            if (firstInit)
            {
                totalMoves = 4;
                totalMoveTimers = new float[] { 0.5f, 1.2f, 1.0f, 1.2f };

                firstInit = false;
            }

            if (currentMoveTimer >= totalMoveTimers[currentMove])
            {
                currentMove += 1;
                if (currentMove == totalMoves)
                {
                    // Change first timer, as it will only be used on the very first move.
                    totalMoveTimers[0] = 1.0f;
                    currentMove = 0;
                }
                currentMoveTimer = 0.0f;
            }

            switch (currentMove)
            {
                // Y-Move to first leg.
                case 0:
                    movingTargetVelocity.X = 0.5f;
                    movingTargetVelocity.Y = 0.5f;

                    if(movingCurrVelocity.X < movingTargetVelocity.X)
                        movingCurrVelocity.X += DT;
                    if(movingCurrVelocity.Y < movingTargetVelocity.Y)
                        movingCurrVelocity.Y += DT;

                    Yaxis.pushValue += DT * (movingCurrVelocity.Y * 2);
                    rotationZ -= DT * 2 * (movingCurrVelocity.Y * 2);
                    rotationX += DT * 0.5f * (movingCurrVelocity.X * 2);
                    Zaxis.pushValue += DT * (movingCurrVelocity.X * 2);
                    break;

                // Stop Y.
                case 1:
                    if (movingCurrVelocity.Y > 0.0f)
                        movingCurrVelocity.Y -= DT;
                    Yaxis.pushValue += DT * (movingCurrVelocity.Y * 2);
                    Zaxis.pushValue += DT * (movingCurrVelocity.X * 2);
                    break;

                // Y-Move to second leg.
                case 2:
                    if(movingCurrVelocity.X < movingTargetVelocity.X)
                        movingCurrVelocity.X += DT;
                    if(movingCurrVelocity.Y < movingTargetVelocity.Y)
                        movingCurrVelocity.Y += DT;

                    Yaxis.pushValue -= DT * (movingCurrVelocity.Y * 2);
                    rotationZ += DT * 2 * (movingCurrVelocity.Y * 2);
                    rotationX -= DT * 0.5f * (movingCurrVelocity.X * 2);
                    Zaxis.pushValue += DT * (movingCurrVelocity.X * 2);
                    break;

                // Stop Y.
                case 3:
                    if (movingCurrVelocity.Y > 0.0f)
                        movingCurrVelocity.Y -= DT;
                    Yaxis.pushValue -= DT * (movingCurrVelocity.Y * 2);
                    Zaxis.pushValue += DT * (movingCurrVelocity.X * 2);
                    break;
            }
        }

        private void updateZigzagWave(float DT)
        {
            if (firstInit)
            {
                totalMoves = 2;
                totalMoveTimers = new float[] {0.5f, 1.0f};

                firstInit = false;
            }

            if (currentMoveTimer >= totalMoveTimers[currentMove])
            {
                currentMove += 1;
                if (currentMove == totalMoves)
                {
                    // Change first timer, as it will only be used on the very first move.
                    totalMoveTimers[0] = 1.0f;
                    currentMove = 0;
                }
                currentMoveTimer = 0.0f;
            }

            switch (currentMove)
            {
                // Y-Move to first leg.
                case 0:
                    movingTargetVelocity.X = 0.5f;
                    movingTargetVelocity.Y = 0.5f;

                    if(movingCurrVelocity.X < movingTargetVelocity.X)
                        movingCurrVelocity.X += DT;
                    if(movingCurrVelocity.Y < movingTargetVelocity.Y)
                        movingCurrVelocity.Y += DT;
                    
                    Yaxis.pushValue += DT * (movingCurrVelocity.Y * 2);
                    Yaxis.pushValue -= 2 * (waveSegment * DT * (movingCurrVelocity.Y * 2));
                    rotationZ += Yaxis.pushValue * 2;
                    rotationX -= Yaxis.pushValue * 0.5f;
                    Zaxis.pushValue += DT * (movingCurrVelocity.X * 2);
                    break;

                // Y-Move to second leg.
                case 1:
                    movingTargetVelocity.Y = -0.5f;

                    if(movingCurrVelocity.X < movingTargetVelocity.X)
                        movingCurrVelocity.X += DT;
                    if(movingCurrVelocity.Y > movingTargetVelocity.Y)
                        movingCurrVelocity.Y -= DT;

                    Yaxis.pushValue += DT * (movingCurrVelocity.Y * 2);
                    Yaxis.pushValue -= 2 * (waveSegment * DT * (movingCurrVelocity.Y * 2));
                    rotationZ += Yaxis.pushValue * 2;
                    rotationX -= Yaxis.pushValue * 0.5f;
                    Zaxis.pushValue += DT * (movingCurrVelocity.X * 2);
                    break;
            }
        }

        private void updateSlowThenFastWave(float DT)
        {
            if (firstInit)
            {
                totalMoves = 3;
                totalMoveTimers = new float[] { 2.0f, 2.0f, 5.0f };

                firstInit = false;
            }

            if (currentMoveTimer >= totalMoveTimers[currentMove])
            {
                currentMove += 1;
                if (currentMove == totalMoves)
                {
                    // This behaviour doesn't loop.
                    currentMove = 2;
                }
                currentMoveTimer = 0.0f;
            }

            // Always reset prior to using a weapon telling it it can't fire
            weaponStats.bulletsFired = false;

            switch (currentMove)
            {
                // Z-Move to first leg.
                case 0:
                    movingTargetVelocity.X = 1.0f;
                    if (movingCurrVelocity.X < movingTargetVelocity.X)
                        movingCurrVelocity.X += DT;

                    Zaxis.pushValue += DT * movingCurrVelocity.X;
                    break;

                // Stop.
                case 1:
                    if (movingCurrVelocity.X > 0.0f)
                        movingCurrVelocity.X -= DT;
                    Zaxis.pushValue += DT * movingCurrVelocity.X;

                    // If we are in a state that we can fire, then let the system know
                    weaponStats.bulletsFired = true;

                    break;
                
                // Z-Move to second leg.
                case 2:
                    movingTargetVelocity.X = 2.0f;
                    if (movingCurrVelocity.X < movingTargetVelocity.X)
                        movingCurrVelocity.X += DT;

                    Zaxis.pushValue += DT * (movingCurrVelocity.X * 2);
                    break;
            }

            // Shoot simple weapon
            // Always count down the timer so we know when we can shoot,
            // the bool check in the if statement lets us know if we can, and the timer
            // makes it so we don't shoot out 60 bullets a second
            weaponStats.bullets.countDown -= DT;
            if (weaponStats.bullets.countDown < 0.0f && weaponStats.bulletsFired)
            {
                WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.BULLETS, SHOOT_DIRECTION.MIDDLE, weaponStats.bullets, this, null, false, null);
                weaponStats.bullets.countDown = weaponStats.bullets.cooldown;
            }
        }

        private void UpdateInThenOut(float DT)
        {
            if (firstInit)
            {
                totalMoves = 3;
                totalMoveTimers = new float[] { 2.5f, (Global.Instance.rand.Next(400, 800) * 0.01f), 10.0f };
                
                // Warp-in effect.
                movingCurrVelocity.X = 5.0f;

                // Set a random exit vector.
                exitVector = new Vector3(0.0f, (-500.0f + Global.Instance.rand.Next(0, 1000)), (-1000.0f - Global.Instance.rand.Next(0, 1000)));

                AI_Functions.Instance.RandomiseDrift(this);
                firstInit = false;
            }

            AI_Functions.Instance.HandleMoveTimers(this, false);
            AI_Functions.Instance.HandleProximityEvents(this);

            weaponStats.bulletsFired = false;

            switch (currentMove)
            {
                // Enter area, decelerate at 4x.
                case 0:
                    AI_Functions.Instance.HandleMovement(this, new Vector2(1.0f, 0.0f), 4.0f, DT);
                    break;

                // Stop, face target, hover, fire, pushback, drift, avoidance.
                case 1:
                    AI_Functions.Instance.HandleMovement(this, new Vector2(0.0f, 0.0f), 1.0f, DT);
                    AI_Functions.Instance.FaceTarget(this, 0.0f, 1.0f, 2, DT);
                    if (movingCurrVelocity.X > -0.3f && movingCurrVelocity.X < 0.3f)
                    {
                        weaponStats.bulletsFired = true;
                        AI_Functions.Instance.HandlePushback(this, DT);
                        AI_Functions.Instance.HandleAvoidance(this, DT);
                        AI_Functions.Instance.HandleDrift(this, DT);//remove because it looks crap with this behaviour
                        AI_Functions.Instance.HandleHover(this, DT);
                    }        
                    break;
                
                // Rotate backwards and exit area.
                case 2:
                    AI_Functions.Instance.FaceTarget(this, 1.0f, 0.5f, 0, DT);
                    break;
            }

            weaponStats.bullets.countDown -= DT;
            if (weaponStats.bullets.countDown < 0.0f && weaponStats.bulletsFired)
            {
                WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.BULLETS, SHOOT_DIRECTION.MIDDLE, weaponStats.bullets, this, null, false, null);
                weaponStats.bullets.countDown = weaponStats.bullets.cooldown;

                // Set a weapons pushback velocity.
                pushbackForce = 0.2f;
            }
        }

        private void UpdatePatrollingEdge(float DT)
        {
            if (firstInit)
            {
                totalMoves = 4;
                totalMoveTimers = new float[] { 15.0f, 
                                                Global.Instance.rand.Next(3, 6), 
                                                Global.Instance.rand.Next(3, 6), 
                                                Global.Instance.rand.Next(3, 6) };
                
                // This prevents some behaviours where the ship will be too far away.
                float fixZbound = Position.Z;
                if (fixZbound < -850)
                    fixZbound = Global.Instance.rand.Next(-800, -600);
                
                enterVector = new Vector3(Position.X, Global.Instance.rand.Next(-100, 100), fixZbound);
                exitVector = new Vector3(0.0f, -15.0f, 0.0f); // This is not the exit vector, but to make it face left.

                firstInit = false;
            }

            AI_Functions.Instance.HandleMoveTimers(this, true);

            weaponStats.bulletsFired = false;
            weaponStats.rocketsFired = false;

            switch (currentMove)
            {
                // Enter area by facing and accelerating towards enter vector.
                case 0:
                    AI_Functions.Instance.FaceTarget(this, 1.5f, 1.5f, 1, DT);
                    AI_Functions.Instance.EvalEnterVector(this);
                    break;

                // Face 0,0,0 and shoot single weapons while hovering and drifting.
                case 1:
                    weaponStats.bulletsFired = true;
                    AI_Functions.Instance.FaceTarget(this, 0.0f, 1.0f, 3, DT);
                    AI_Functions.Instance.HandleHover(this, DT);
                    AI_Functions.Instance.HandleDrift(this, DT);
                    if (weaponStats.bullets.countDown < 0.0f && weaponStats.bulletsFired)
                    {
                        WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.BULLETS, SHOOT_DIRECTION.BOTTOM, weaponStats.bullets, this, null, false, null);
                        WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.BULLETS, SHOOT_DIRECTION.MIDDLE, weaponStats.bullets, this, null, false, null);
                        WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.BULLETS, SHOOT_DIRECTION.TOP, weaponStats.bullets, this, null, false, null);
                        weaponStats.bullets.countDown = weaponStats.bullets.cooldown;
                    }
                    break;

                // Face 0,0,0 and shoot triple weapons while hovering and drifting.
                case 2:
                    weaponStats.rocketsFired = true;
                    AI_Functions.Instance.FaceTarget(this, 0.0f, 1.0f, 3, DT);
                    AI_Functions.Instance.HandleHover(this, DT);
                    AI_Functions.Instance.HandleDrift(this, DT);
                    if (weaponStats.rockets.countDown < 0.0f && weaponStats.rocketsFired)
                    {
                        WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.ROCKETS, SHOOT_DIRECTION.BOTTOM, weaponStats.rockets, this, null, false, null);
                        WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.ROCKETS, SHOOT_DIRECTION.MIDDLE, weaponStats.rockets, this, null, false, null);
                        WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.ROCKETS, SHOOT_DIRECTION.TOP, weaponStats.rockets, this, null, false, null);
                        weaponStats.rockets.countDown = weaponStats.rockets.cooldown;
                    }
                    break;

                // Face 0,0,0 and shoot rocket weapons while hovering and drifting.
                case 3:
                    weaponStats.rocketsFired = true;
                    AI_Functions.Instance.FaceTarget(this, 0.0f, 1.0f, 3, DT);
                    AI_Functions.Instance.HandleHover(this, DT);
                    AI_Functions.Instance.HandleDrift(this, DT);
                    if (weaponStats.rockets.countDown < 0.0f && weaponStats.rocketsFired)
                    {
                        WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.ROCKETS, SHOOT_DIRECTION.BOTTOM1, weaponStats.rockets, this, null, false, null);
                        WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.ROCKETS, SHOOT_DIRECTION.BOTTOM, weaponStats.rockets, this, null, false, null);
                        WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.ROCKETS, SHOOT_DIRECTION.MIDDLE, weaponStats.rockets, this, null, false, null);
                        WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.ROCKETS, SHOOT_DIRECTION.TOP, weaponStats.rockets, this, null, false, null);
                        WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.ROCKETS, SHOOT_DIRECTION.TOP1, weaponStats.rockets, this, null, false, null);
                        weaponStats.rockets.countDown = weaponStats.rockets.cooldown;
                    }
                    break;
            }

            weaponStats.bullets.countDown -= DT;
            weaponStats.rockets.countDown -= DT;
        }

        private void UpdateAssault(float DT)
        {
            if (firstInit)
            {
                totalMoves = 2;
                totalMoveTimers = new float[] { 15.0f, 15.0f };

                // Set random enter vector.  This is where the ship will travel to.
                enterVector = new Vector3(0.0f, Global.Instance.rand.Next(-100, 100), Global.Instance.rand.Next(-700, -450));

                // Start facing left.
                m_Rotation = Quaternion.CreateFromAxisAngle(m_Up, MathHelper.ToRadians(180));
                UpdateTransform();

                firstInit = false;
            }

            AI_Functions.Instance.HandleMoveTimers(this, true);

            weaponStats.bulletsFired = false;

            switch (currentMove)
            {
                // Enter area.
                case 0:
                    AI_Functions.Instance.FaceTarget(this, 2.5f, 2.5f, 1, DT);
                    AI_Functions.Instance.EvalEnterVector(this);
                    AI_Functions.Instance.HandleProximityEvents(this);
                    break;

                // Stop, rotate towards player and fire.
                case 1:
                    if (AI_Functions.Instance.FaceTarget(this, 0.0f, 2.0f, 2, DT))
                    {
                        weaponStats.bulletsFired = true;
                    }
                    AI_Functions.Instance.HandleHover(this, DT);
                    AI_Functions.Instance.HandlePushback(this, DT);
                    AI_Functions.Instance.HandleDrift(this, DT);
                    AI_Functions.Instance.HandleProximityEvents(this);
                    AI_Functions.Instance.HandleAvoidance(this, DT);
                    break;
            }

            weaponStats.bullets.countDown -= DT;
            if (weaponStats.bullets.countDown < 0.0f && weaponStats.bulletsFired)
            {
                WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.BULLETS, SHOOT_DIRECTION.MIDDLE, weaponStats.bullets, this, null, false, null);
                weaponStats.bullets.countDown = weaponStats.bullets.cooldown;

                // Set a weapons pushback velocity.
                pushbackForce = 0.2f;
            }
        }

        private void UpdateCovert(float DT)
        {
            if (firstInit)
            {
                totalMoves = 4;
                totalMoveTimers = new float[] { 15.0f, 15.0f, (Global.Instance.rand.Next(200, 500) * 0.01f), 15.0f };

                // Set random enter vector.  This is where the ship will travel to.
                enterVector = new Vector3(0.0f, Global.Instance.rand.Next(-100, 100), Global.Instance.rand.Next(-700, -450));

                // Set exit vector as the origin.  This is already set by ObjectSpawn.
                exitVector = Position;
                exitVector.Z *= 1.5f;
                
                firstInit = false;
            }

            AI_Functions.Instance.HandleMoveTimers(this, false);            

            weaponStats.bulletsFired = false;

            switch (currentMove)
            {
                // Enter area.
                case 0:
                    AI_Functions.Instance.FaceTarget(this, 2.0f, 2.0f, 1, DT);
                    AI_Functions.Instance.EvalEnterVector(this);
                    break;

                // Stop, rotate towards player.
                case 1:
                    if (AI_Functions.Instance.FaceTarget(this, 0.0f, 2.0f, 2, DT))
                    {
                        currentMove++;
                        currentMoveTimer = 0.0f;
                    }
                    AI_Functions.Instance.HandleHover(this, DT);
                    break;
                
                // Fire at player.
                case 2:
                    weaponStats.bulletsFired = true;
                    AI_Functions.Instance.FaceTarget(this, 0.0f, 1.0f, 2, DT);
                    AI_Functions.Instance.HandleProximityEvents(this);
                    AI_Functions.Instance.HandlePushback(this, DT);
                    AI_Functions.Instance.HandleHover(this, DT);
                    AI_Functions.Instance.HandleAvoidance(this, DT);
                    AI_Functions.Instance.HandleDrift(this, DT);
                    break;
                
                // Rotate slowly towards exit vector while accelerating, creating an arc.
                case 3:
                    AI_Functions.Instance.FaceTarget(this, 2.0f, 1.0f, 0, DT);
                    break;
            }

            weaponStats.bullets.countDown -= DT;
            if (weaponStats.bullets.countDown < 0.0f && weaponStats.bulletsFired)
            {
                WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.BULLETS, SHOOT_DIRECTION.MIDDLE, weaponStats.bullets, this, null, false, null);
                weaponStats.bullets.countDown = weaponStats.bullets.cooldown;

                // Set a weapons pushback velocity.
                pushbackForce = 0.2f;
            }
        }

        private void UpdateRollingTopBottom(float DT)
        {
            if (firstInit)
            {
                totalMoves = 1;
                totalMoveTimers = new float[] { 15.0f };

                if (GlobalTransform.Translation.Y < 0)
                    rollingUp = false;
                if (GlobalTransform.Translation.Y > 0)
                    rollingUp = true;

                weaponStats.bullets.cooldown = 0.7f;

                firstInit = false;
            }                    

            AI_Functions.Instance.HandleMoveTimers(this, false);

            weaponStats.bulletsFired = false;

            switch (currentMove)
            {
                // Enters, rolls and fires.
                case 0:
                    weaponStats.bulletsFired = true;
                    AI_Functions.Instance.Zspin(this, DT);
                    if(rollingUp)
                        AI_Functions.Instance.HandleMovement(this, new Vector2(0.0f, -1.0f), 1.0f, DT);
                    if(!rollingUp)
                        AI_Functions.Instance.HandleMovement(this, new Vector2(0.0f, 1.0f), 1.0f, DT);
                    break;
            }

            weaponStats.bullets.countDown -= DT;
            if (weaponStats.bullets.countDown < 0.0f && weaponStats.bulletsFired)
            {
                WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.BULLETS, SHOOT_DIRECTION.MIDDLE, weaponStats.bullets, this, null, false, null);
                weaponStats.bullets.countDown = weaponStats.bullets.cooldown;

                // Set a weapons pushback velocity.
                pushbackForce = 0.2f;
            }
        }

        private void UpdateTopBottom(float DT)
        {
            if (firstInit)
            {
                totalMoves = 1;
                totalMoveTimers = new float[] { 15.0f };

                if (GlobalTransform.Translation.Y < 0)
                    rollingUp = false;
                if (GlobalTransform.Translation.Y > 0)
                    rollingUp = true;

                firstInit = false;
            }

            AI_Functions.Instance.HandleMoveTimers(this, false);

            weaponStats.bulletsFired = false;

            switch (currentMove)
            {
                // Enters, aims and fires.
                case 0:
                    // Only fire when roughly within bounds.
                    if(Position.Y > -150 && Position.Y < 150)
                        weaponStats.bulletsFired = true;
                    AI_Functions.Instance.FaceTarget(this, 0.0f, 3.0f, 2, DT);
                    if (rollingUp)
                        AI_Functions.Instance.HandleMovement(this, new Vector2(0.0f, -2.0f), 1.0f, DT);
                    if (!rollingUp)
                        AI_Functions.Instance.HandleMovement(this, new Vector2(0.0f, 2.0f), 1.0f, DT);
                    break;
            }

            weaponStats.bullets.countDown -= DT;
            if (weaponStats.bullets.countDown < 0.0f && weaponStats.bulletsFired)
            {
                WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.BULLETS, SHOOT_DIRECTION.MIDDLE, weaponStats.bullets, this, null, false, null);
                weaponStats.bullets.countDown = weaponStats.bullets.cooldown;

                // Set a weapons pushback velocity.
                pushbackForce = 0.2f;
            }
        }

        private void UpdateStraightAiming(float DT)
        {
            if (firstInit)
            {
                // Warp-in effect.
                movingCurrVelocity.X = 5.0f;

                // Start facing left.
                m_Rotation = Quaternion.CreateFromAxisAngle(m_Up, MathHelper.ToRadians(180));
                UpdateTransform();

                firstInit = false;
            }

            AI_Functions.Instance.HandleMovement(this, new Vector2(1.5f, 0.0f), 4.0f, DT);
            AI_Functions.Instance.FaceTarget(this, 0.0f, 2.0f, 2, DT);
            weaponStats.bulletsFired = true;

            weaponStats.bullets.countDown -= DT;
            if (weaponStats.bullets.countDown < 0.0f && weaponStats.bulletsFired)
            {
                WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.BULLETS, SHOOT_DIRECTION.MIDDLE, weaponStats.bullets, this, null, false, null);
                weaponStats.bullets.countDown = weaponStats.bullets.cooldown;
            }
        }

        private void UpdateMiddleTopBottom(float DT)
        {
            if (firstInit)
            {
                totalMoves = 3;
                totalMoveTimers = new float[] { 15.0f, Global.Instance.rand.Next(4, 7), 15.0f };

                enterVector = new Vector3(Position.X, 0.0f, Position.Z);
                
                if (GlobalTransform.Translation.Y < 0)
                    exitVector = new Vector3(Position.X, 1000.0f, Position.Z);
                if (GlobalTransform.Translation.Y > 0)
                    exitVector = new Vector3(Position.X, -1000.0f, Position.Z);

                firstInit = false;
            }

            AI_Functions.Instance.HandleMoveTimers(this, false);

            weaponStats.bulletsFired = false;

            switch (currentMove)
            {
                // Enters and flies towards Y=0.
                case 0:
                    AI_Functions.Instance.FaceTarget(this, 1.5f, 1.5f, 1, DT);
                    AI_Functions.Instance.EvalEnterVector(this);
                    break;

                    // Aims at player and fires.
                case 1:
                    if(AI_Functions.Instance.FaceTarget(this, 0.0f, 1.5f, 2, DT))
                    {
                        weaponStats.bulletsFired = true;
                    }
                    AI_Functions.Instance.HandleHover(this, DT);
                    AI_Functions.Instance.HandleProximityEvents(this);
                    AI_Functions.Instance.HandlePushback(this, DT);
                    break;

                    // Exits opposite of enter vector.
                case 2:
                    AI_Functions.Instance.FaceTarget(this, 1.5f, 1.5f, 0, DT);
                    break;
            }

            weaponStats.bullets.countDown -= DT;
            if (weaponStats.bullets.countDown < 0.0f && weaponStats.bulletsFired)
            {
                WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.BULLETS, SHOOT_DIRECTION.MIDDLE, weaponStats.bullets, this, null, false, null);
                weaponStats.bullets.countDown = weaponStats.bullets.cooldown;

                // Set a weapons pushback velocity.
                pushbackForce = 0.2f;
            }
        }

        private void UpdateKamikaze(float DT)
        {
            if (firstInit)
            {
                totalMoves = 2;
                totalMoveTimers = new float[] { 1.0f, 15.0f };

                // Warp-in effect.
                movingCurrVelocity.X = 4.0f;

                // Start facing left.
                m_Rotation = Quaternion.CreateFromAxisAngle(m_Up, MathHelper.ToRadians(180));
                UpdateTransform();

                firstInit = false;
            }

            AI_Functions.Instance.HandleMoveTimers(this, true);

            switch (currentMove)
            {
                case 0:
                    AI_Functions.Instance.HandleMovement(this, new Vector2(1.0f, 0.0f), 3.0f, DT);
                    break;

                case 1:
                    AI_Functions.Instance.HandleMovement(this, new Vector2(0.0f, 0.0f), 1.0f, DT); // Fixes velocity bug (FaceTarget isn't aware of movement vectors).
                    if (AI_Functions.Instance.FaceTarget(this, 1.0f, 1.0f, 2, DT))
                    {
                        // Only fire if this ship is within the bounds plus a bit.
                        if(Position.Y > -150 && Position.Y > 150)
                            weaponStats.bulletsFired = true;
                    }
                    break;
            }

            weaponStats.bullets.countDown -= DT;
            if (weaponStats.bullets.countDown < 0.0f && weaponStats.bulletsFired)
            {
                WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.BULLETS, SHOOT_DIRECTION.MIDDLE, weaponStats.bullets, this, null, false, null);
                weaponStats.bullets.countDown = weaponStats.bullets.cooldown;
            }
        }

        private void UpdateStraightSlow(float DT)
        {
            if (firstInit)
            {
                // Warp-in effect.
                movingCurrVelocity.X = 4.0f;

                firstInit = false;
            }

            AI_Functions.Instance.HandleMovement(this, new Vector2(0.5f, 0.0f), 4.0f, DT);
            weaponStats.bulletsFired = true;

            weaponStats.bullets.countDown -= DT;
            if (weaponStats.bullets.countDown < 0.0f && weaponStats.bulletsFired)
            {
                WeaponsManager.Instance.FireWeapon(WEAPON_TYPES.BULLETS, SHOOT_DIRECTION.MIDDLE, weaponStats.bullets, this, null, false, null);
                weaponStats.bullets.countDown = weaponStats.bullets.cooldown;
            }
        }        

        private void initFormationDelay(float factor)
        {
            switch (waveFormation)
            {

                    // FIX these to be aware of how large the flight group is, so it's always centred.

                case SPAWN_STATE.DIAGONAL_LEFT:
                    switch (wavePosition)
                    {
                        case 0: currentMoveTimer = -1.2f * factor; break;
                        case 1: currentMoveTimer = -1.0f * factor; break;
                        case 2: currentMoveTimer = -0.8f * factor; break;
                        case 3: currentMoveTimer = -0.6f * factor; break;
                        case 4: currentMoveTimer = -0.4f * factor; break;
                        case 5: currentMoveTimer = -0.2f * factor; break;
                        case 6: currentMoveTimer = 0.0f * factor; break;
                    }
                    break;

                case SPAWN_STATE.DIAGONAL_RIGHT:
                    switch (wavePosition)
                    {
                        case 0: currentMoveTimer = 0.0f * factor; break;
                        case 1: currentMoveTimer = -0.2f * factor; break;
                        case 2: currentMoveTimer = -0.4f * factor; break;
                        case 3: currentMoveTimer = -0.6f * factor; break;
                        case 4: currentMoveTimer = -0.8f * factor; break;
                        case 5: currentMoveTimer = -1.0f * factor; break;
                        case 6: currentMoveTimer = -1.2f * factor; break;
                    }
                    break;

                case SPAWN_STATE.HORIZONTAL:
                    switch (wavePosition)
                    {
                        case 0: currentMoveTimer = -1.8f * factor; break;
                        case 1: currentMoveTimer = -1.5f * factor; break;
                        case 2: currentMoveTimer = -1.2f * factor; break;
                        case 3: currentMoveTimer = -0.9f * factor; break;
                        case 4: currentMoveTimer = -0.6f * factor; break;
                        case 5: currentMoveTimer = -0.3f * factor; break;
                        case 6: currentMoveTimer = 0.0f * factor; break;
                    }
                    break;

                case SPAWN_STATE.TRIANGLE_DOWN:
                    switch (wavePosition)
                    {
                        case 0: currentMoveTimer = -0.6f * factor; break;
                        case 1: currentMoveTimer = -0.4f * factor; break;
                        case 2: currentMoveTimer = -0.2f * factor; break;
                        case 3: currentMoveTimer = 0.0f * factor; break;
                        case 4: currentMoveTimer = -0.2f * factor; break;
                        case 5: currentMoveTimer = -0.4f * factor; break;
                        case 6: currentMoveTimer = -0.6f * factor; break;
                    }
                    break;

                case SPAWN_STATE.TRIANGLE_UP:
                    switch (wavePosition)
                    {
                        case 0: currentMoveTimer = 0.0f * factor; break;
                        case 1: currentMoveTimer = -0.2f * factor; break;
                        case 2: currentMoveTimer = -0.4f * factor; break;
                        case 3: currentMoveTimer = -0.6f * factor; break;
                        case 4: currentMoveTimer = -0.4f * factor; break;
                        case 5: currentMoveTimer = -0.2f * factor; break;
                        case 6: currentMoveTimer = 0.0f * factor; break;
                    }
                    break;

                case SPAWN_STATE.VERTICAL:
                    break;
            }
        }

        private void initSpeed()
        {
            // 10% randomisation.
            float randY = Global.Instance.rand.Next(700 - (700 / 10), 700 + (700 / 10));
            float randX = Global.Instance.rand.Next(1400 - (1400 / 10), 1400 + (1400 / 10));
            randY *= 0.01f;
            randX *= 0.01f;

            Yaxis = new ShipPhysics(randY, 0.1f, randY);
            Zaxis = new ShipPhysics(randX, 0.1f, randX);
        }

        public override void Collide(Node n)
        {
            health = -1.0f;

            if (health < 0.0f)
            {
                active = false;
            }

            for (int k = 0; k < 25; k++)
            {
                ParticleEventManager.Instance.explosionLarge.AddParticle(GlobalTransform.Translation, Vector3.Zero);
            }
            for (int k = 0; k < 50; k++)
            {
                ParticleEventManager.Instance.debrisSmall.AddParticle(GlobalTransform.Translation, Vector3.Zero);
            }
            for (int k = 0; k < 5; k++)
            {
                ParticleEventManager.Instance.debrisMedium.AddParticle(GlobalTransform.Translation, Vector3.Zero);
            }
            for (int k = 0; k < 2; k++)
            {
                ParticleEventManager.Instance.debrisLarge.AddParticle(GlobalTransform.Translation, Vector3.Zero);
            }
        }

        public override void Hit(BaseWeapon proj)
        {
            health -= proj.damage;            

            for (int k = 0; k < 5; k++)
            {
                ParticleEventManager.Instance.explosionSmall.AddParticle(GlobalTransform.Translation, Vector3.Zero);
            }

            if (health < 0.0f)
            {
                active = false;
                SoundManager.Instance.PlayExplosion();
                //ParticleEventManager.Instance.CreateLargeExplosion(GlobalTransform.Translation);

                int random = Global.Instance.rand.Next(0, 10);
                if (random == 1)
                {
                    ParticleEventManager.Instance.CreateLargeExplosion(GlobalTransform.Translation);
                }
                else
                {
                    for (int k = 0; k < 25; k++)
                    {
                        ParticleEventManager.Instance.explosionLarge.AddParticle(GlobalTransform.Translation, Vector3.Zero);
                    }
                    for (int k = 0; k < 50; k++)
                    {
                        ParticleEventManager.Instance.debrisSmall.AddParticle(GlobalTransform.Translation, Vector3.Zero);
                    }
                }

                if (proj.owner == GlobalGameObjects.Instance.m_player) // if the player was the one that scored this kill
                {
                    Global.Instance.currentScore += (int)(maxHealth 
                                                 *        (float)Global.Instance.difficulty 
                                                 *        (1 + ((float)Global.Instance.currentLevelSelected * 0.125f)));
                    PowerUpsManager.Instance.AddPowerUp(GlobalTransform.Translation);
                }
            }
            else // play the sound when your not killing the enemy
            {
                if (proj.GetWeaponType == WEAPON_TYPES.BULLETS)
                {
                    SoundManager.Instance.PlayCannonImpact();
                }

                if (proj.GetWeaponType != WEAPON_TYPES.BULLETS && proj.GetWeaponType != WEAPON_TYPES.PLASMA)
                {
                    SoundManager.Instance.PlayMissileImpact();
                }
            }
        }
    }
}
