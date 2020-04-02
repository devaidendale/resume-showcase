using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StarCiv
{
    public class MapManager
    {
        
        public HexCell[,] hexes;
        public ResourceObject[,] resources;
        public HexCell selectionHex;
        public HexCell hoverHex;
        public List<HexCell> ownedHexes;
        public int width;
        public int height;
        public int xOffset;
        public int yOffset;
        public int lenSide;
        public float pixelWidth;
        public float pixelHeight;
        BasicEffect effect;
        public List<VertexPositionColor> gridVerts;
        
        // Layer 2 objects.
        public List<BaseShip> ships;

        // Super basic AI
        public BasicAI basicAI;
        
        // Placeholder graphics for resources
        public List<VertexPositionColor> resourceVerts;

        public enum HexVertice
        {
            Top = 0,
            TopRight = 1,
            BottomRight = 2,
            Bottom = 3,
            BottomLeft = 4,
            TopLeft = 5,
        }

        public MapManager(int width, int height, int a_lenSide, int xOffset, int yOffset, GraphicsDevice device)
        {
            this.gridVerts = new List<VertexPositionColor>();

            this.ships = new List<BaseShip>();

            this.ownedHexes = new List<HexCell>();
            
            this.resourceVerts = new List<VertexPositionColor>();

            this.width = width;
            this.height = height;
            this.xOffset = xOffset;
            this.yOffset = yOffset;
            this.lenSide = a_lenSide;
            hexes = new HexCell[height, width];
            resources = new ResourceObject[height, width];
            basicAI = new BasicAI();

            effect = new BasicEffect(device, null);
            effect.VertexColorEnabled = true;

            float h = Global.Instance.CalculateH(lenSide);
            float r = Global.Instance.CalculateR(lenSide);
        
            float hexWidth = 0;
            float hexHeight = 0;
      
            hexWidth = r + r;
            hexHeight = lenSide + h;

            this.pixelWidth = (width * hexWidth) + r;
            this.pixelHeight = (height * hexHeight) + h;

            bool inTopRow = false;
            bool inBottomRow = false;
            bool inLeftColumn = false;
            bool inRightColumn = false;
            bool isTopLeft = false;
            bool isTopRight = false;
            bool isBottomLeft = false;
            bool isBottomRight = false;           
            
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    #region Position Booleans
                    if (i == 0)
                    {
                        inTopRow = true;
                    }
                    else
                    {
                        inTopRow = false;
                    } 
                    if (i == height - 1)
                    {
                        inBottomRow = true;
                    }
                    else
                    {
                        inBottomRow = false;
                    } 
                    if (j == 0)
                    {
                        inLeftColumn = true;
                    }
                    else
                    {
                        inLeftColumn = false;
                    } 
                    if (j == width - 1)
                    {
                        inRightColumn = true;
                    }
                    else
                    {
                        inRightColumn = false;
                    } 
                    if (inTopRow && inLeftColumn)
                    {
                        isTopLeft = true;
                    }
                    else
                    {
                        isTopLeft = false;
                    } 
                        if (inTopRow && inRightColumn)
                    {
                    isTopRight = true;
                    }
                    else
                    {
                        isTopRight = false;
                    } 
                    if (inBottomRow && inLeftColumn)
                    {
                        isBottomLeft = true;
                    }
                    else
                    {
                        isBottomLeft = false;
                    } 
                    if (inBottomRow && inRightColumn)
                    {
                        isBottomRight = true;
                    }
                    else
                    {
                        isBottomRight = false;
                    }
                    #endregion 
             
                    
                    // Calculate hex positions.
                    if (isTopLeft)
                    {
                        // First hex.
                        hexes[0, 0] = new HexCell(new Vector3(0 + r + xOffset, 0 + yOffset, 0), lenSide, new Vector2(0, 0));
                    }
                    else
                    {
                        if (inLeftColumn)
                        {
                            // Calculate from hex above and stagger the rows.
                            if (i % 2 == 0)
                            {
                                hexes[i, j] = new HexCell(hexes[i - 1, j].vertices[(int)HexVertice.BottomLeft].Position, lenSide, new Vector2(j, i));
                            }
                            else
                            {
                                hexes[i, j] = new HexCell(hexes[i - 1, j].vertices[(int)HexVertice.BottomRight].Position, lenSide, new Vector2(j, i));
                            }
                        }
                        else
                        {
                            // Calculate from hex to the left.
                            float x = hexes[i, j - 1].vertices[(int)HexVertice.TopRight].Position.X;
                             
                            float y = hexes[i, j - 1].vertices[(int)HexVertice.TopRight].Position.Y;
                             
                            x += r;
                            y -= h;
                            hexes[i, j] = new HexCell(new Vector3(x, y, 0), lenSide, new Vector2(j, i));
                        }
                    }
                }
            }

            selectionHex = new HexCell(hexes[0,0].GetPos() + new Vector3(0, (int)(lenSide * 0.2), 0), (int)(lenSide * 0.8), new Vector2(0, 0));
            hoverHex = new HexCell(hexes[0,0].GetPos() + new Vector3(0, (int)(lenSide * 0.01), 0), (int)(lenSide * 0.99), new Vector2(0, 0));
            
            // Combine each hex vertex list into grid vertex list.
            for (int i = 0; i < hexes.GetLength(0); i++)
            {
                for (int j = 0; j < hexes.GetLength(1); j++)
                {
                    gridVerts.AddRange(hexes[i, j].vertices.ToArray());
                }
            }

            // Load layer 1 (resources).

            // Assign resources to each hex location.
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    resources[i, j] = new ResourceObject((int)Global.PlayerOwners.None, hexes[i, j], (int)Global.Instance.rand.Next(0, 4));
                }
            }

            // Create some visually linear distributions of black holes to act as 'walls' for A*.
            int blackholeStreaks = (int)Global.Instance.rand.Next(7, 14);
            for (int i = 0; i < blackholeStreaks; i++)
            {
                Vector2 streakCoords = new Vector2(Global.Instance.rand.Next(1, Global.Instance.HexGridSizeX - 2), Global.Instance.rand.Next(1, Global.Instance.HexGridSizeY - 2));
                int streakLength = (int)Global.Instance.rand.Next(9, 15);
                int streakDirection = 0;
                
                for (int j = 0; j < streakLength; j++)
                {
                    resources[(int)streakCoords.Y, (int)streakCoords.X].resourceType = 9;
                    hexes[(int)streakCoords.Y, (int)streakCoords.X].toggleHasShip(true);
                    streakDirection = Global.Instance.rand.Next(0, 6);
                    bool oddRow = streakCoords.Y % 2 > 0 ? true : false;
                    Vector2 nextCoords = streakCoords;

                    if (oddRow)
                    {
                        switch (streakDirection)
                        {
                            case 0:
                                // TopRight exit
                                nextCoords.X += 1.0f;
                                nextCoords.Y += 1.0f;
                                break;
                            case 1:
                                // Right exit
                                nextCoords.X += 1.0f;
                                break;
                            case 2:
                                // BottomRight exit
                                nextCoords.X += 1.0f;
                                nextCoords.Y -= 1.0f;
                                break;
                            case 3:
                                // BottomLeft exit
                                nextCoords.Y -= 1.0f;
                                break;
                            case 4:
                                // Left exit
                                nextCoords.X -= 1.0f;
                                break;
                            case 5:
                                // TopLeft exit
                                nextCoords.Y += 1.0f;
                                break;
                        }
                    }
                    else if (!oddRow)
                    {
                        switch (streakDirection)
                        {
                            case 0:
                                // TopRight exit
                                nextCoords.Y += 1.0f;
                                break;
                            case 1:
                                // Right exit
                                nextCoords.X += 1.0f;
                                break;
                            case 2:
                                // BottomRight exit
                                nextCoords.Y -= 1.0f;
                                break;
                            case 3:
                                // BottomLeft exit
                                nextCoords.Y -= 1.0f;
                                nextCoords.X -= 1.0f;
                                break;
                            case 4:
                                // Left exit
                                nextCoords.X -= 1.0f;
                                break;
                            case 5:
                                // TopLeft exit
                                nextCoords.Y += 1.0f;
                                nextCoords.X -= 1.0f;
                                break;
                        }
                    }

                    // Skip if it's a border tile.
                    if ((nextCoords.X < 1 || nextCoords.X > Global.Instance.HexGridSizeX - 2) || (nextCoords.Y < 1 || nextCoords.Y > Global.Instance.HexGridSizeY - 2))
                    {
                        // streakCoords remain the same.
                    }
                    else
                    {
                        streakCoords = nextCoords;
                    }
                    
                    // This location becomes a black hole.  (hasShip has an unfortunate name.)
                    resources[(int)streakCoords.Y, (int)streakCoords.X].resourceType = 9;
                    hexes[(int)streakCoords.Y, (int)streakCoords.X].toggleHasShip(true);
                }

            }

            // Load layer 2 (ships)

            // Player starts off with one colony ship.
            ColonyShip playerBeginningShip = new ColonyShip((int)Global.PlayerOwners.User, hexes[0, 0]);
            ships.Add(playerBeginningShip);
            hexes[0, 0].toggleHasShip(true);
            ships[0].selected = true;
            
            // Computer starts off with a scout.
            ScoutShip computerBeginningShip = new ScoutShip((int)Global.PlayerOwners.Computer1, hexes[height - 1, width - 1]);
            ships.Add(computerBeginningShip);
            basicAI.AddAI(ships[1]);
            hexes[height - 1, width - 1].toggleHasShip(true);

        }

        public void Hover(Vector2 pos, GraphicsDevice device, ref Camera camera)
        {
            // Using ray picking.
            Vector3 nearsource = new Vector3(pos.X, pos.Y, 0.0f);
            Vector3 farsource = new Vector3(pos.X, pos.Y, 1.0f);
            Matrix world = Matrix.CreateTranslation(0, 0, 0);
            Vector3 nearPoint = device.Viewport.Unproject(nearsource, camera.projectionMatrix, camera.viewMatrix, world);
            Vector3 farPoint = device.Viewport.Unproject(farsource, camera.projectionMatrix, camera.viewMatrix, world);

            // Create a ray from the near clip plane to the far clip plane.
            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();
            Ray pickRay = new Ray(nearPoint, direction);

            // Check for collisions.
            for (int i = 0; i < hexes.GetLength(0); i++)
            {
                for (int j = 0; j < hexes.GetLength(1); j++)
                {
                    if (hexes[i, j].bBox.Intersects(pickRay).HasValue)
                    {
                        hoverHex.AssignHexCell(hexes[i, j].GetPos() + new Vector3(0, (int)(lenSide * 0.01), 0), (int)(lenSide * 0.99), hexes[i, j].GetCoordinates());
                    }
                }
            }
        }

        public void Select(Vector2 pos, GraphicsDevice device, ref Camera camera)
        {         
            // Clear any selected layer 2 ships.
            for (int i = 0; i < ships.Count; i++)
            {
                if (ships[i].selected)
                {
                    ships[i].Select(false);
                }
            }

            // Using ray picking.
            Vector3 nearsource = new Vector3(pos.X, pos.Y, 0.0f);
            Vector3 farsource = new Vector3(pos.X, pos.Y, 1.0f);
            Matrix world = Matrix.CreateTranslation(0, 0, 0);
            Vector3 nearPoint = device.Viewport.Unproject(nearsource, camera.projectionMatrix, camera.viewMatrix, world);
            Vector3 farPoint = device.Viewport.Unproject(farsource, camera.projectionMatrix, camera.viewMatrix, world);

            // Create a ray from the near clip plane to the far clip plane.
            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();
            Ray pickRay = new Ray(nearPoint, direction);

            // Check for collisions.
            for (int i = 0; i < hexes.GetLength(0); i++)
            {
                for (int j = 0; j < hexes.GetLength(1); j++)
                {
                    if (hexes[i, j].bBox.Intersects(pickRay).HasValue)
                    {
                        selectionHex.AssignHexCell(hexes[i, j].GetPos() + new Vector3(0, (int)(lenSide * 0.2), 0), (int)(lenSide * 0.8), hexes[i, j].GetCoordinates());
                        // Trigger selection of layer 2 ships if there's one here.
                        for (int k = 0; k < ships.Count; k++)
                        {
                            if (selectionHex.coordinates == ships[k].hexCell.coordinates && ships[k].owner == (int)Global.PlayerOwners.User)
                            {
                                if (!ships[k].active)
                                {
                                    ships[k].Select(true);
                                }
                            }
                        }
                    }
                }
            }            

        }

        public void Command(Vector2 pos, GraphicsDevice device, ref Camera camera)
        {
            for(int i = 0; i < ships.Count; i++)
            {
                BaseShip selectedShip = ships[i];
                if (selectedShip.selected && !selectedShip.active)   // Check if a layer 2 ship is both selected and not active before proceeding.
                {
                    // Using ray picking.
                    Vector3 nearsource = new Vector3(pos.X, pos.Y, 0.0f);
                    Vector3 farsource = new Vector3(pos.X, pos.Y, 1.0f);
                    Matrix world = Matrix.CreateTranslation(0, 0, 0);
                    Vector3 nearPoint = device.Viewport.Unproject(nearsource, camera.projectionMatrix, camera.viewMatrix, world);
                    Vector3 farPoint = device.Viewport.Unproject(farsource, camera.projectionMatrix, camera.viewMatrix, world);

                    // Create a ray from the near clip plane to the far clip plane.
                    Vector3 direction = farPoint - nearPoint;
                    direction.Normalize();
                    Ray pickRay = new Ray(nearPoint, direction);

                    // Needed to get out of the loop if a match has been found, otherwise it's possible two hex cells are selected, breaking Astar horribly.
                    bool getout = false;

                    // Check for collisions.
                    for (int j = 0; j < hexes.GetLength(0); j++)
                    {
                        for (int k = 0; k < hexes.GetLength(1); k++)
                        {
                            if (hexes[j, k].bBox.Intersects(pickRay).HasValue && getout == false)
                            {
                                if (hexes[j, k] != selectedShip.hexCell)    // Don't initiate travel if trying to move to own cell.
                                {
                                    bool combatKilledMe = false;
                                    // Check for combat (this is done instantly due to gameplay issues).
                                    for (int l = 0; l < ships.Count; l++)
                                    {
                                        if (ships[l].hexCell.coordinates == new Vector2(k, j))
                                        {
                                            // Make sure the target ship is actually an enemy.
                                            if (ships[l].owner != (int)Global.PlayerOwners.User)
                                            {
                                                combatKilledMe = Combat(selectedShip, ships[l]);
                                            }
                                        }
                                    }
                                    
                                    // Don't set a course if SUPER AWESOME INSTANT COMBAT destroyed the player's ship.
                                    if (combatKilledMe)
                                    {
                                        getout = true;
                                    }
                                    else
                                    {
                                        // Set a course, captain.
                                        selectedShip.SetDestination(hexes[j, k], hexes);
                                        getout = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }

        public bool Combat(BaseShip attacker, BaseShip defender)
        {
            // To the death!
            bool attackerWins;
            while (true)
            {
                // Attacker hits first.
                defender.strength -= 1;
                if (defender.strength == 0)
                {
                    attackerWins = true;
                    break;
                }
                // Defender counter-attacks.
                attacker.strength -= 1;
                if (attacker.strength == 0)
                {
                    attackerWins = false;
                    break;
                }
            }
            if (attackerWins)
            {
                // The attacker won.  Return with false (as in, the attacking ship did not die).
                if (attacker.owner == (int)Global.PlayerOwners.User)
                {
                    Global.Instance.AssignUiSpecialMessage("Your ship attacked and it won!");
                    basicAI.RemoveAI(defender);
                }
                else
                    Global.Instance.AssignUiSpecialMessage("Your ship was attacked and it lost!");
                defender.RemoveShip();
                ships.Remove(defender);
                return false;
            }
            else
            {
                // The defender won.  Return with true (as in, the attacking ship died).
                if (defender.owner == (int)Global.PlayerOwners.User)
                {
                    Global.Instance.AssignUiSpecialMessage("Your ship was attacked and it won!");
                    basicAI.RemoveAI(attacker);
                }
                else
                    Global.Instance.AssignUiSpecialMessage("Your ship attacked and it lost!");
                attacker.RemoveShip();
                ships.Remove(attacker);
                return true;
            }
        }

        public void Action()
        {
            bool hitSomething = false;  // Indicates whether an action actually tried to do anything.

            
            for (int i = 0; i < ships.Count; i++)
            {
                // Ships:
                if (ships[i].selected && !ships[i].active)
                {
                    // Colony ships:
                    if (ships[i].GetObjectType() == "Colony Ship")
                    {
                        hitSomething = true;
                        Vector2 buildHere = ships[i].hexCell.GetCoordinates();

                        // Find out what type of tile the colony ship is on.  Can only build on worker-less space or planets.
                        if (resources[(int)buildHere.Y, (int)buildHere.X].GetObjectType() == "Planet")
                        {
                            // Planetary colony.

                            // Destroy the ship.
                            ships[i].RemoveShip();
                            ships.RemoveAt(i);

                            // Create planetary colony here.
                            resources[(int)buildHere.Y, (int)buildHere.X].BuildColony(true, (int)Global.PlayerOwners.User);

                            // Assign all adjacent hex tiles as valid workable tiles (unless it's out of bounds or a planet).
                            ValidateAdjacentTiles(buildHere, true);

                        }
                        else if (resources[(int)buildHere.Y, (int)buildHere.X].GetObjectType() == "Space" && resources[(int)buildHere.Y, (int)buildHere.X].workers == 0)
                        {
                            // Space colony

                            // Destroy the ship.
                            ships[i].RemoveShip();
                            ships.RemoveAt(i);

                            // Create space colony here.
                            resources[(int)buildHere.Y, (int)buildHere.X].BuildColony(false, (int)Global.PlayerOwners.User);

                            // Assign all adjacent hex tiles as valid workable tiles (unless it's out of bounds or a planet).
                            ValidateAdjacentTiles(buildHere, true);

                        }
                        else
                        {
                            // Has something on this cell, can't build.
                            Global.Instance.AssignUiErrorMessage("Can't build here!");
                        }
                    }
                }
            }

            // For colony bases:
            if(resources[(int)selectionHex.GetCoordinates().Y, (int)selectionHex.GetCoordinates().X].isAColony())
            {
                hitSomething = true;

                // If cursor is on colony, scroll through building options.
                if (hoverHex.GetCoordinates() == selectionHex.GetCoordinates())
                {
                    resources[(int)selectionHex.GetCoordinates().Y, (int)selectionHex.GetCoordinates().X].BuildProduction();
                }

                // If cursor is on a valid tile, send workers to it.
                else if (resources[(int)hoverHex.GetCoordinates().Y, (int)hoverHex.GetCoordinates().X].validLocation)
                {
                    if (resources[(int)selectionHex.GetCoordinates().Y, (int)selectionHex.GetCoordinates().X].workers > 0.0f)
                    {
                        float amount = 5.0f;

                        // If destination is nearly full, adjust transfer to not overflow.
                        if (resources[(int)hoverHex.GetCoordinates().Y, (int)hoverHex.GetCoordinates().X].workers > 195.0f)
                        {
                            amount = 200.0f - resources[(int)hoverHex.GetCoordinates().Y, (int)hoverHex.GetCoordinates().X].workers;
                        }

                        // If source has less than the amount, transfer only whatever it has.
                        if (resources[(int)selectionHex.GetCoordinates().Y, (int)selectionHex.GetCoordinates().X].workers < amount)
                        {
                            amount = resources[(int)selectionHex.GetCoordinates().Y, (int)selectionHex.GetCoordinates().X].workers;
                            resources[(int)hoverHex.GetCoordinates().Y, (int)hoverHex.GetCoordinates().X].workers += amount;
                            resources[(int)selectionHex.GetCoordinates().Y, (int)selectionHex.GetCoordinates().X].workers = 0;
                        }

                        // Otherwise, transfer the amount calculated.
                        else
                        {
                            resources[(int)selectionHex.GetCoordinates().Y, (int)selectionHex.GetCoordinates().X].workers -= amount;
                            resources[(int)hoverHex.GetCoordinates().Y, (int)hoverHex.GetCoordinates().X].workers += amount;
                        }

                        if (amount < 1.0f)
                        {
                            if (resources[(int)hoverHex.GetCoordinates().Y, (int)hoverHex.GetCoordinates().X].workers >= 200.0f)
                            {
                                // Target cell is full.
                                Global.Instance.AssignUiErrorMessage("That tile has maximum workers!");
                            }
                            else
                            {
                                // Technically still transfers workers, despite the message, but is pragmatic in regards to gameplay.
                                Global.Instance.AssignUiErrorMessage("No workers to transfer!");
                            }
                        }
                    }
                }

                // Targetted tile is a planet.
                else if (resources[(int)hoverHex.GetCoordinates().Y, (int)hoverHex.GetCoordinates().X].resourceType == 2)
                {
                    Global.Instance.AssignUiErrorMessage("Target tile is a planet!");
                }
                
                // Targetted tile is a black hole.
                else if (resources[(int)hoverHex.GetCoordinates().Y, (int)hoverHex.GetCoordinates().X].resourceType == 9)
                {
                    Global.Instance.AssignUiErrorMessage("Target tile is a black hole!");
                }

                // The only other possibility is the tile is not adjacent to the colony.
                else
                {
                    Global.Instance.AssignUiErrorMessage("Target tile is out of range of colony!");
                }

            }

            if (!hitSomething)
                Global.Instance.AssignUiErrorMessage("Select something first to give it orders!");
        }

        // This function checks adjacent tiles and evaluates if they can be worked on.
        public void ValidateAdjacentTiles(Vector2 adjacentPos, bool colony)
        {
            bool oddRow = adjacentPos.Y % 2 > 0 ? true : false;
            
            // If a colony has just been built, automatically award all valid adjacent tiles.
            // Otherwise, check if there are at least 100 workers on the tile.
            if (colony || resources[(int)adjacentPos.Y, (int)adjacentPos.X].workers >= 100)
            {
                if (oddRow)
                {
                    if (adjacentPos.X - 1 >= 0)
                        resources[(int)adjacentPos.Y, (int)adjacentPos.X - 1].evaluateLocation();
                    if (adjacentPos.X + 1 < Global.Instance.HexGridSizeX)
                        resources[(int)adjacentPos.Y, (int)adjacentPos.X + 1].evaluateLocation();
                    if (adjacentPos.Y - 1 >= 0)
                        resources[(int)adjacentPos.Y - 1, (int)adjacentPos.X].evaluateLocation();
                    if (adjacentPos.Y + 1 < Global.Instance.HexGridSizeY)
                        resources[(int)adjacentPos.Y + 1, (int)adjacentPos.X].evaluateLocation();
                    if (adjacentPos.X + 1 < Global.Instance.HexGridSizeX && adjacentPos.Y - 1 >= 0)
                        resources[(int)adjacentPos.Y - 1, (int)adjacentPos.X + 1].evaluateLocation();
                    if (adjacentPos.X + 1 < Global.Instance.HexGridSizeX && adjacentPos.Y + 1 < Global.Instance.HexGridSizeY)
                        resources[(int)adjacentPos.Y + 1, (int)adjacentPos.X + 1].evaluateLocation();
                }
                else if (!oddRow)
                {
                    if (adjacentPos.X - 1 >= 0)
                        resources[(int)adjacentPos.Y, (int)adjacentPos.X - 1].evaluateLocation();
                    if (adjacentPos.X + 1 < Global.Instance.HexGridSizeX)
                        resources[(int)adjacentPos.Y, (int)adjacentPos.X + 1].evaluateLocation();
                    if (adjacentPos.Y - 1 >= 0)
                        resources[(int)adjacentPos.Y - 1, (int)adjacentPos.X].evaluateLocation();
                    if (adjacentPos.Y + 1 < Global.Instance.HexGridSizeY)
                        resources[(int)adjacentPos.Y + 1, (int)adjacentPos.X].evaluateLocation();
                    if (adjacentPos.X - 1 >= 0 && adjacentPos.Y + 1 < Global.Instance.HexGridSizeY)
                        resources[(int)adjacentPos.Y + 1, (int)adjacentPos.X - 1].evaluateLocation();
                    if (adjacentPos.X - 1 >= 0 && adjacentPos.Y - 1 >= 0)
                        resources[(int)adjacentPos.Y - 1, (int)adjacentPos.X - 1].evaluateLocation();
                }
            }

        }

        public String WhatIsHere(Vector2 coordinate)
        {
            String objectsHere = "";
            // Returns concatenated string of all objects in this cell.
            objectsHere += resources[(int)coordinate.Y, (int)coordinate.X].GetOwnerString();
            objectsHere += resources[(int)coordinate.Y, (int)coordinate.X].GetObjectType();
            for (int i = 0; i < ships.Count; i++)
            {
                if (ships[i].hexCell.coordinates == coordinate)
                {
                    objectsHere += ",\n" + ships[i].GetOwnerString();
                    objectsHere += ships[i].GetObjectType();
                }
            }           

            return objectsHere;
        }

        public void Update(float dt)
        {            
            // Update resources.
            for (int i = 0; i < resources.GetLength(0); i++)
            {
                for (int j = 0; j < resources.GetLength(1); j++)
                {
                    resources[j, i].Update(dt);

                    // Update colony productions.
                    if (resources[j, i].isAColony())
                    {
                        resources[j, i].EvaluateProduction(ships, hexes[j, i]);
                    }

                    // Validate workable tiles.
                    ValidateAdjacentTiles(new Vector2(i, j), false);

                    // Assign influence hexes.
                    if (!resources[j, i].GetHasInfluence())
                    {
                        // Influence not assigned; must assign.
                        ownedHexes.Add(new HexCell(hexes[j, i].GetPos() + new Vector3(0, (int)(lenSide * -0.01), 0), (int)(lenSide * 1.01), hexes[j,i].GetCoordinates()));
                        resources[j, i].ChangeInfluence(true);
                    }
                }
            }          
            
            for(int i = 0; i < ships.Count; i++)
            {
                if (ships[i].active)
                {
                    ships[i].Update(dt);
                }
            }

            // Update influence hexes.
            for (int i = 0; i < ownedHexes.Count; i++)
            {
                ownedHexes[i].Update(dt, resources[(int)ownedHexes[i].GetCoordinates().Y, (int)ownedHexes[i].GetCoordinates().X].workers);
            }

            // Pulsate selection and hover hexes.
            selectionHex.Update(dt, 0.0f);
            hoverHex.Update(dt, 0.0f);

            // Update AI.
            basicAI.Update(dt);
            // See if AI can create a new ship.
            if (basicAI.newShipInterval <= 0.0f)
            {
                // Make sure spawning hex is empty.
                if (!hexes[height - 1, width - 1].hasShip)
                {
                    ScoutShip newAIShip = new ScoutShip((int)Global.PlayerOwners.Computer1, hexes[height - 1, width - 1]);
                    ships.Add(newAIShip);
                    basicAI.AddAI(ships[ships.IndexOf(newAIShip)]);   // Just to not make me dumb from thinking pointers aren't the same in other programming languages.
                }
            }
            // See if AI can give orders to any ships.
            for (int i = 0; i < basicAI.orderIntervals.Count; i++)
            {
                BaseShip AISelectedShip = basicAI.AIShips[i];
                if (basicAI.orderIntervals[i] <= 0.0f)
                {
                    // An order is up.
                    int theOrder = basicAI.OrderShip(i);
                    Vector2 orderDestination = AISelectedShip.hexCell.GetCoordinates();
                    bool oddRow = AISelectedShip.hexCell.GetCoordinates().Y % 2 > 0 ? true : false;
                    if (oddRow)
                    {
                        switch (theOrder)
                        {
                            case 0:
                                // TopRight exit
                                orderDestination.X += 1.0f;
                                orderDestination.Y += 1.0f;
                                break;
                            case 1:
                                // Right exit
                                orderDestination.X += 1.0f;
                                break;
                            case 2:
                                // BottomRight exit
                                orderDestination.X += 1.0f;
                                orderDestination.Y -= 1.0f;
                                break;
                            case 3:
                                // BottomLeft exit
                                orderDestination.Y -= 1.0f;
                                break;
                            case 4:
                                // Left exit
                                orderDestination.X -= 1.0f;
                                break;
                            case 5:
                                // TopLeft exit
                                orderDestination.Y += 1.0f;
                                break;
                        }
                    }
                    if (!oddRow)
                    {
                        switch (theOrder)
                        {
                            case 0:
                                // TopRight exit
                                orderDestination.Y += 1.0f;
                                break;
                            case 1:
                                // Right exit
                                orderDestination.X += 1.0f;
                                break;
                            case 2:
                                // BottomRight exit
                                orderDestination.Y -= 1.0f;
                                break;
                            case 3:
                                // BottomLeft exit
                                orderDestination.Y -= 1.0f;
                                orderDestination.X -= 1.0f;
                                break;
                            case 4:
                                // Left exit
                                orderDestination.X -= 1.0f;
                                break;
                            case 5:
                                // TopLeft exit
                                orderDestination.Y += 1.0f;
                                orderDestination.X -= 1.0f;
                                break;
                        }
                    }

                    // Ensure the destination is valid.
                    if ((orderDestination.X > 0 && orderDestination.X < Global.Instance.HexGridSizeX) && (orderDestination.Y > 0 && orderDestination.Y < Global.Instance.HexGridSizeY))
                    {
                        bool combatKilledMe = false;
                        // Check for combat (instant).
                        for (int j = 0; j < ships.Count; j++)
                        {
                            if (ships[j].owner == (int)Global.PlayerOwners.User)
                            {
                                if (ships[j].hexCell.GetCoordinates() == orderDestination)
                                    combatKilledMe = Combat(AISelectedShip, ships[j]);
                            }
                        }

                        // Don't continue if player defended successfully against attack.
                        if (combatKilledMe)
                        {

                        }
                        else
                        {
                            // Set course.
                           AISelectedShip.SetDestination(hexes[(int)orderDestination.Y, (int)orderDestination.X], hexes);
                        }
                    }
                    else
                    {
                        // Order is ignored and skipped as it is invalid.
                    }
                }
            }

        }


        public void Draw(GraphicsDevice device, float dt, ref Camera camera)
        {
            device.VertexDeclaration = new VertexDeclaration(device, VertexPositionColor.VertexElements);
            
            effect.VertexColorEnabled = true;
            
            effect.Begin();

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                effect.View = camera.View;
                effect.Projection = camera.Projection;

                // Draw hex grid:
                device.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, gridVerts.ToArray(), 0, gridVerts.Count / 2);

                // Draw influence hexes:
                for(int i = 0; i < ownedHexes.Count; i++)
                {
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, ownedHexes[i].vertices.ToArray(), 0, ownedHexes[i].vertices.Count, ownedHexes[i].indices, 0, ownedHexes[i].indices.Length / 3);
                }

                // Draw selection hex:
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, selectionHex.vertices.ToArray(), 0, selectionHex.vertices.Count, selectionHex.indices, 0, selectionHex.indices.Length / 3);

                // Draw hover hex:
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, hoverHex.vertices.ToArray(), 0, hoverHex.vertices.Count, hoverHex.indices, 0, hoverHex.indices.Length / 3);
                
                // Layer 1:
                // Draw resources:
                resourceVerts = new List<VertexPositionColor>();
                for(int i = 0; i < resources.GetLength(0); i++)
                {
                    for (int j = 0; j < resources.GetLength(1); j++ )
                    {
                        resourceVerts.Add(resources[j, i].vert);
                    }
                }
                device.RenderState.PointSize = 15;
                device.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.PointList, resourceVerts.ToArray(), 0, resourceVerts.Count);


                // Layer 2:
                // Draw ships: Handled by game function due to model data.

                pass.End();
            }

            effect.End();

        }
    }
}
