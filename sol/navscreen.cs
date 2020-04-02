using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace DBP_NavScreen
{
    public class NavScreen
    {
        public Texture2D navBorderOverlay;
        public NavStarfield navStarfield;
        public Camera navCamera;

        public List<NavWaypoint> navWaypoints;

        public Texture2D xboxA;
        public Texture2D xboxB;
        public float navSpritesAlpha;
        public float planetViewSpritesAlpha;
        
        public enum NavKeyMoves
        {
            NONE = 0,
            
            UP = 1,
            RIGHT = 2,
            DOWN = 3,
            LEFT = 4,
            SELECT = 5,
            EXIT = 6,
        }
        public int currSelection;

        public struct ViewingPlanetState
        {
            public ViewingPlanetState(bool a_executing, bool a_entering, bool a_exiting)
            {
                executing = a_executing;
                entering = a_entering;
                exiting = a_exiting;
            }

            public bool isActive()
            {
                if (executing || entering || exiting)
                    return true;
                else
                    return false;
            }

            public bool executing;
            public bool entering;
            public bool exiting;
            public void execute() { executing = true; entering = false; exiting = false; }
            public void enter() { executing = false; entering = true; exiting = false; }
            public void exit() { executing = false; entering = false; exiting = true; }
            public void deactivate() { executing = false; entering = false; exiting = false; }
        }

        public ViewingPlanetState viewingPlanetState;

        // Alias (pointer) for the planet database.
        List<NavPlanet> planetDatabase = NavPROTOTYPE_EXAMPLE_DATABASE.Instance.planetDatabase;

        public NavScreen() { }

        public void Initialise(ContentManager content, GraphicsDevice device)
        {
            navBorderOverlay = content.Load<Texture2D>("Bitmaps/NavBorder");
            xboxA = content.Load<Texture2D>("Bitmaps/xboxA");
            xboxB = content.Load<Texture2D>("Bitmaps/xboxB");
            navSpritesAlpha = 1.0f;
            planetViewSpritesAlpha = 0.0f;

            navCamera = new Camera(device.Viewport);

            navStarfield = new NavStarfield();
            navStarfield.CreateNode(device, content);

            navWaypoints = new List<NavWaypoint>();

            currSelection = 0;
            viewingPlanetState = new ViewingPlanetState(false, false, false);

            // Initialise planets.
            for (int i = 0; i < planetDatabase.Count; i++)
            {
                planetDatabase[i].Initialise(device, content);
            }

            // Sort planets' visual hierarchy.
            for (int i = 0; i < planetDatabase.Count; i++)
            {
                if (i > 0)
                {
                    if (planetDatabase[i].level == planetDatabase[i - 1].level)
                    {
                        // Planets are on the same level.
                        planetDatabase[i].planetSector = NavPlanet.PlanetSector.BOTTOM;
                        planetDatabase[i - 1].planetSector = NavPlanet.PlanetSector.TOP;
                    }
                    else
                    {
                        // Planets are not on the same level.
                        planetDatabase[i].planetSector = NavPlanet.PlanetSector.MIDDLE;
                    }
                }
                else
                {
                    // First planet.
                    planetDatabase[i].planetSector = NavPlanet.PlanetSector.MIDDLE;
                }
            }

            // Sort planets' shading, which depends on what the player has completed.
            for (int i = 0; i < planetDatabase.Count; i++)
            {
                // Checks if this planet is completed, and sets the next level as a potential level.
                if (planetDatabase[i].completed == true)
                {
                    planetDatabase[i].InitialiseState(NavPlanet.PlanetState.COMPLETED);

                    // Get all planets in the next level.  Note: it is only possible that the next level is up to 3 indices past the current one.
                    List<NavPlanet> nextLevelPlanets = new List<NavPlanet>();
                    for (int j = 1; j <= 3; j++)
                    {
                        // Make sure index is not out of bounds, which would indicate an end level.
                        if ((i + j) < planetDatabase.Count)
                        {
                            if (planetDatabase[i + j].level == planetDatabase[i].level + 1)
                            {
                                // Found a planet in the next level.
                                nextLevelPlanets.Add(planetDatabase[i + j]);
                            }
                        }
                    }

                    // If the current level is in the top sector...
                    if (planetDatabase[i].planetSector == NavPlanet.PlanetSector.TOP)
                    {
                        // And any planets in the next level are in the top or middle sectors...
                        for (int j = 0; j < nextLevelPlanets.Count; j++)
                        {
                            if (nextLevelPlanets[j].planetSector == NavPlanet.PlanetSector.TOP || nextLevelPlanets[j].planetSector == NavPlanet.PlanetSector.MIDDLE)
                            {
                                // Set next level's planet state to a potential level.
                                nextLevelPlanets[j].InitialiseState(NavPlanet.PlanetState.VALID);
                            }
                        }
                    }

                    // If the current level is in the middle sector...
                    if (planetDatabase[i].planetSector == NavPlanet.PlanetSector.MIDDLE)
                    {
                        // Set next level's planet(s) to a potential level.
                        for (int j = 0; j < nextLevelPlanets.Count; j++)
                        {
                            nextLevelPlanets[j].InitialiseState(NavPlanet.PlanetState.VALID);
                        }
                    }

                    // If the current level is in the bottom sector...
                    if (planetDatabase[i].planetSector == NavPlanet.PlanetSector.BOTTOM)
                    {
                        // And any planets in the next level are in the bottom or middle sectors...
                        for (int j = 0; j < nextLevelPlanets.Count; j++)
                        {
                            if (nextLevelPlanets[j].planetSector == NavPlanet.PlanetSector.BOTTOM || nextLevelPlanets[j].planetSector == NavPlanet.PlanetSector.MIDDLE)
                            {
                                // Set next level's planet state to a potential level.
                                nextLevelPlanets[j].InitialiseState(NavPlanet.PlanetState.VALID);
                            }
                        }
                    }

                }
            }

            // Initialise waypoints.  (Update them all first otherwise their positions would be 0.)
            for (int i = 0; i < planetDatabase.Count; i++)
            {
                planetDatabase[i].Update(0.0f);
            }
            for (int i = 0; i < planetDatabase.Count; i++)
            {   
                int thisPlanetLevel = planetDatabase[i].level;

                if (planetDatabase[i].completed == true)
                {
                    // Valid next level planet(s) in relation to current one.
                    for (int j = 1; j < 3; j++)
                    {
                        if (i + j < planetDatabase.Count)
                        {
                            if (planetDatabase[i + j].level == thisPlanetLevel + 1)
                            {
                                // They must be in the appropriate sector too.
                                if (planetDatabase[i].planetSector == NavPlanet.PlanetSector.MIDDLE || planetDatabase[i].planetSector == planetDatabase[i + j].planetSector)
                                {
                                    Color colour = Color.White;
                                    switch (planetDatabase[i + j].origPlanetState)
                                    {
                                        case NavPlanet.PlanetState.COMPLETED:
                                            colour = Color.LightGreen;
                                            break;
                                        case NavPlanet.PlanetState.VALID:
                                            colour = Color.DarkGreen;
                                            break;
                                    }
                                    navWaypoints.Add(new NavWaypoint(device, planetDatabase[i].Position, planetDatabase[i + j].Position, colour));
                                }
                            }
                        }
                    }
                }
            }

        }

        public void EvaluateInput(NavKeyMoves keyMove)
        {
            // Set some data about the currently selected planet for processing.
            bool inTopSector = planetDatabase[currSelection].planetSector == NavPlanet.PlanetSector.TOP ? true : false;
            bool inMiddleSector = planetDatabase[currSelection].planetSector == NavPlanet.PlanetSector.MIDDLE ? true : false;
            bool inBottomSector = planetDatabase[currSelection].planetSector == NavPlanet.PlanetSector.BOTTOM ? true : false;

            if (!viewingPlanetState.isActive())
            {
                switch (keyMove)
                {

                    case NavKeyMoves.UP:

                        if (currSelection + 1 < planetDatabase.Count)
                        {
                            if (inMiddleSector && planetDatabase[currSelection + 1].planetSector == NavPlanet.PlanetSector.TOP)
                            {
                                if (planetDatabase[currSelection + 1].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection += 1;
                                }
                            }
                        }
                        if (currSelection - 1 >= 0)
                        {
                            if (inBottomSector)
                            {
                                if (planetDatabase[currSelection - 1].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection -= 1;
                                }
                            }
                        }
                        break;

                    case NavKeyMoves.RIGHT:

                        if (currSelection + 2 < planetDatabase.Count)
                        {
                            if (inTopSector && planetDatabase[currSelection + 2].planetSector == NavPlanet.PlanetSector.TOP)
                            {
                                if (planetDatabase[currSelection + 2].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection += 2;
                                }
                            }
                            else if (inTopSector && planetDatabase[currSelection + 2].planetSector == NavPlanet.PlanetSector.MIDDLE)
                            {
                                if (planetDatabase[currSelection + 2].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection += 2;
                                }
                            }
                            else if (inBottomSector && planetDatabase[currSelection + 2].planetSector == NavPlanet.PlanetSector.BOTTOM)
                            {
                                if (planetDatabase[currSelection + 2].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection += 2;
                                }
                            }

                        }
                        if (currSelection + 1 < planetDatabase.Count)
                        {
                            if (inMiddleSector && planetDatabase[currSelection + 1].planetSector == NavPlanet.PlanetSector.TOP)
                            {
                                if (planetDatabase[currSelection + 1].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection += 1;
                                }
                            }
                            else if (inBottomSector && planetDatabase[currSelection + 1].planetSector == NavPlanet.PlanetSector.MIDDLE)
                            {
                                if (planetDatabase[currSelection + 1].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection += 1;
                                }
                            }
                            else if (inMiddleSector && planetDatabase[currSelection + 1].planetSector == NavPlanet.PlanetSector.MIDDLE)
                            {
                                if (planetDatabase[currSelection + 1].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection += 1;
                                }
                            }
                        }
                        break;

                    case NavKeyMoves.DOWN:

                        if (currSelection + 2 < planetDatabase.Count)
                        {
                            if (inMiddleSector && planetDatabase[currSelection + 2].planetSector == NavPlanet.PlanetSector.BOTTOM)
                            {
                                if (planetDatabase[currSelection + 2].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection += 2;
                                }
                            }
                        }
                        if (currSelection + 1 < planetDatabase.Count)
                        {
                            if (inTopSector)
                            {
                                if (planetDatabase[currSelection + 1].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection += 1;
                                }
                            }
                        }
                        break;

                    case NavKeyMoves.LEFT:

                        if (currSelection - 2 >= 0)
                        {
                            if (inTopSector && planetDatabase[currSelection - 2].planetSector == NavPlanet.PlanetSector.TOP)
                            {
                                if (planetDatabase[currSelection - 2].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection -= 2;
                                }
                            }
                            else if (inMiddleSector && planetDatabase[currSelection - 2].planetSector == NavPlanet.PlanetSector.TOP)
                            {
                                if (planetDatabase[currSelection - 2].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection -= 2;
                                }
                            }
                            else if (inBottomSector && planetDatabase[currSelection - 2].planetSector == NavPlanet.PlanetSector.MIDDLE)
                            {
                                if (planetDatabase[currSelection - 2].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection -= 2;
                                }
                            }
                            else if (inBottomSector && planetDatabase[currSelection - 2].planetSector == NavPlanet.PlanetSector.BOTTOM)
                            {
                                if (planetDatabase[currSelection - 2].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection -= 2;
                                }
                            }
                        }
                        if (currSelection - 1 >= 0)
                        {
                            if (inTopSector && planetDatabase[currSelection - 1].planetSector == NavPlanet.PlanetSector.MIDDLE)
                            {
                                if (planetDatabase[currSelection - 1].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection -= 1;
                                }
                            }
                            else if (inMiddleSector && planetDatabase[currSelection - 1].planetSector == NavPlanet.PlanetSector.MIDDLE)
                            {
                                if (planetDatabase[currSelection - 1].currPlanetState != NavPlanet.PlanetState.INVALID)
                                {
                                    planetDatabase[currSelection].ResetOriginalState();
                                    currSelection -= 1;
                                }
                            }
                        }
                        break;

                    case NavKeyMoves.SELECT:

                        viewingPlanetState.enter();

                        break;
                }

            }

            if (viewingPlanetState.executing)
            {
                switch (keyMove)
                {
                    case NavKeyMoves.SELECT:
                        // Enter level here.
                        break;

                    case NavKeyMoves.EXIT:
                        viewingPlanetState.exit();
                        break;
                }
            }

        }

        public void Update(float dt, GraphicsDevice device)
        {
            if (viewingPlanetState.entering)
            {
                navCamera.offsetDistance.Z -= 4000 * dt;
                if (navCamera.offsetDistance.Z <= 1000)
                    viewingPlanetState.execute();
                if (navSpritesAlpha >= 0.0f)
                    navSpritesAlpha -= dt * 2;
                if (planetViewSpritesAlpha <= 1.0f)
                    planetViewSpritesAlpha += dt * 2;
            }

            if (viewingPlanetState.exiting)
            {
                navCamera.offsetDistance.Z += 4000 * dt;
                if (navCamera.offsetDistance.Z >= 4500)
                    viewingPlanetState.deactivate();
                if (navSpritesAlpha <= 1.0f)
                    navSpritesAlpha += dt * 2;
                if (planetViewSpritesAlpha >= 0.0f)
                    planetViewSpritesAlpha -= dt * 2;
            }

            navCamera.Update(planetDatabase[currSelection].GlobalTransform, true, dt);
            
            navStarfield.Position = navCamera.Position;
            navStarfield.UpdateNode(device, dt, ref navCamera);

            planetDatabase[currSelection].currPlanetState = NavPlanet.PlanetState.SELECTED;

            for (int i = 0; i < planetDatabase.Count; i++)
            {
                planetDatabase[i].Update(dt);

                // Update planet name on main nav state if selected and not in planet view execute.
                if (planetDatabase[i].currPlanetState == NavPlanet.PlanetState.SELECTED && !viewingPlanetState.executing)
                {
                    planetDatabase[i].digiTextName.Activate();
                    // Update digital text effect if in appropriate state.
                    if (planetDatabase[i].digiTextName.digiTextState != DigiText.DigiTextState.Inactive)
                        planetDatabase[i].digiTextName.Update(dt);
                }
                // Update planet detail text on planet viewing state if in the state.
                if (planetDatabase[i].currPlanetState == NavPlanet.PlanetState.SELECTED && (viewingPlanetState.entering || viewingPlanetState.executing))
                {
                    // Set planet's viewing parameter, which ceases pulsating selector.
                    planetDatabase[i].viewing = true;

                    planetDatabase[i].digiTextName.Activate();
                    planetDatabase[i].digiTextDescription.Activate();
                    planetDatabase[i].digiTextPlayerStats.Activate();
                    // Update digital text effect if in appropriate state.
                    if (planetDatabase[i].digiTextName.digiTextState != DigiText.DigiTextState.Inactive)
                        planetDatabase[i].digiTextName.Update(dt);
                    if (planetDatabase[i].digiTextDescription.digiTextState != DigiText.DigiTextState.Inactive)
                        planetDatabase[i].digiTextDescription.Update(dt);
                    if (planetDatabase[i].digiTextPlayerStats.digiTextState != DigiText.DigiTextState.Inactive)
                        planetDatabase[i].digiTextPlayerStats.Update(dt);
                }
                // Set planet's viewing parameter to off if exiting from viewing state, so that the selector can pulsate again.
                if (viewingPlanetState.exiting)
                {
                    planetDatabase[i].viewing = false;
                }
            }
        }

        public void Draw(GraphicsDevice device, float dt)
        {          
            navStarfield.DrawNode(device, dt, ref navCamera);

            for (int i = 0; i < planetDatabase.Count; i++)
            {
                planetDatabase[i].Draw(device, ref navCamera, dt);
            }

            for (int i = 0; i < navWaypoints.Count; i++)
            {
                navWaypoints[i].Draw(dt, device, ref navCamera);
            }
        }

        public void DrawSprites(SpriteBatch spriteBatch, SpriteFont primaryFont, SpriteFont secondaryFont, GraphicsDevice device, float dt)
        {
            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied);

            spriteBatch.Draw(navBorderOverlay, new Vector2(0, 0), Color.White);

            for (int i = 0; i < planetDatabase.Count; i++)
            {
                if(planetDatabase[i].currPlanetState == NavPlanet.PlanetState.SELECTED)
                {
                    Vector3 textPos = planetDatabase[i].Position;
                    Matrix viewProj = navCamera.View * navCamera.Projection;
                    Vector4 projResult = Vector4.Transform(textPos, viewProj);
                    float halfScreenY = ((float)device.Viewport.Height / 2.0f);
                    float halfScreenX = ((float)device.Viewport.Width / 2.0f);
                    Vector2 screenPos = new Vector2(((projResult.X / projResult.W) * halfScreenX) + halfScreenX, halfScreenY - ((projResult.Y / projResult.W) * halfScreenY));                    

                    spriteBatch.DrawString(primaryFont, planetDatabase[i].digiTextName.GetText(), screenPos, new Color(0, 128, 0, navSpritesAlpha), 0.0f, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0);

                    break;
                }
            }

            // Draw main nav sprites if not in planet viewing state.
            if (!viewingPlanetState.executing)
            {
                spriteBatch.Draw(xboxA, new Rectangle(50, Global.Instance.ScreenSizeY - (xboxA.Height + 50), xboxA.Width / 2, xboxA.Height / 2), new Color(255, 255, 255, navSpritesAlpha));
                spriteBatch.DrawString(primaryFont, "Accept", new Vector2(95, Global.Instance.ScreenSizeY - (xboxA.Height + 40)), new Color(0, 0.5f, 0, navSpritesAlpha));
            }

            // Draw viewing sprites if planet view state is active.
            if (viewingPlanetState.isActive())
            {
                // Planet text.
                for (int i = 0; i < planetDatabase.Count; i++)
                {
                    if (planetDatabase[i].currPlanetState == NavPlanet.PlanetState.SELECTED)
                    {
                        spriteBatch.DrawString(primaryFont, planetDatabase[i].digiTextName.GetText(), new Vector2(20, 100), new Color(0, 0.75f, 0, planetViewSpritesAlpha));
                        spriteBatch.DrawString(secondaryFont, planetDatabase[i].digiTextDescription.GetText(), new Vector2(50, 150), new Color(0, 0.5f, 0, planetViewSpritesAlpha));
                        spriteBatch.DrawString(secondaryFont, planetDatabase[i].digiTextPlayerStats.GetText(), new Vector2(950, 150), new Color(0, 0.5f, 0, planetViewSpritesAlpha));
                    }
                }

                spriteBatch.Draw(xboxA, new Rectangle(Global.Instance.ScreenSizeX - 270, Global.Instance.ScreenSizeY - (xboxA.Height + 50), xboxA.Width / 2, xboxA.Height / 2), new Color(255, 255, 255, planetViewSpritesAlpha));
                spriteBatch.DrawString(primaryFont, "Accept", new Vector2(Global.Instance.ScreenSizeX - 225, Global.Instance.ScreenSizeY - (xboxA.Height + 40)), new Color(0, 0.5f, 0, planetViewSpritesAlpha));
                spriteBatch.Draw(xboxB, new Rectangle(Global.Instance.ScreenSizeX - 150, Global.Instance.ScreenSizeY - (xboxB.Height + 50), xboxB.Width / 2, xboxB.Height / 2), new Color(255, 255, 255, planetViewSpritesAlpha));
                spriteBatch.DrawString(primaryFont, "Back", new Vector2(Global.Instance.ScreenSizeX - 105, Global.Instance.ScreenSizeY - (xboxB.Height + 40)), new Color(0.5f, 0, 0, planetViewSpritesAlpha));
            }

            spriteBatch.End();

        }
    }
}
