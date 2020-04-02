using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace StarCiv
{
    public class HexAstar
    {
        public enum HexDirections
        {
            TopRight = 0,
            Right = 1,
            BottomRight = 2,
            BottomLeft = 3,
            Left = 4,
            TopLeft = 5,

            Total = 6,
        }

        public struct ASNode
        {
            public Vector2 coordinates;
            public Vector2 parentID;
            public bool active;
            public bool path;
            
            public float F;     // Float cost.
            public float G;     // Total cost.
            public float H;     // Heuristic cost.

            public ASNode(Vector2 coordinates, Vector2 parentID, float F, float G, float H)
            {
                this.coordinates = coordinates;
                this.parentID = parentID;
                this.F = F;
                this.G = G;
                this.H = H;
                this.active = true;
                this.path = false;
            }
        }

        public ASNode[,] openList;
        public ASNode[,] closedList;

        public List<Vector2> pathway;
        public Vector2[] selectedCoords;
        public bool oddRow;
        public Vector2 start;
        public Vector2 end;
        public float lowestF;
        public ASNode lowestNode;
        public int lowestSelection;

        HexCell[,] hexMap;

        public HexAstar(Vector2 start, Vector2 end, HexCell[,] hexMap)
        {
            this.hexMap = hexMap;
            this.pathway = new List<Vector2>();

            openList = new ASNode[hexMap.GetLength(0), hexMap.GetLength(1)];
            closedList = new ASNode[hexMap.GetLength(0), hexMap.GetLength(1)];
            selectedCoords = new Vector2[(int)HexDirections.Total];

            this.start = start;
            this.end = end;

            // Assign starting node.
            float startF, startG, startH;
            startG = 0.0f;
            startH = heuristicDerivative(start, end);
            startF = startG + startH;

            ASNode startNode = new ASNode(start, new Vector2(-1, -1), startF, startG, startH);

            openList[(int)start.Y, (int)start.X] = startNode;

            AStarBitch(startNode, (int)startNode.coordinates.Y, (int)startNode.coordinates.X);
        }

        public void AStarBitch(ASNode parentNode, int y, int x)
        {
            // Reset sensitive variables.
            lowestF = 65535.0f;
            lowestNode = new ASNode(new Vector2(-1, -1), new Vector2(-1, -1), 65535.0f, 0.0f, 0.0f);
            lowestSelection = -1;

            if (y % 2 == 0)
            {
                oddRow = false;
            }
            else
            {
                oddRow = true;
            }

            for (int idx = 0; idx < (int)HexDirections.Total; idx++)
            {
                switch (idx)
                {
                    case (int)HexDirections.TopRight:
                        if (oddRow)
                        {
                            selectedCoords[idx] = new Vector2(x + 1, y + 1);
                        }
                        else
                        {
                            selectedCoords[idx] = new Vector2(x, y + 1);
                        }
                        break;

                    case (int)HexDirections.Right:
                        selectedCoords[idx] = new Vector2(x + 1, y);
                        break;

                    case (int)HexDirections.BottomRight:
                        if (oddRow)
                        {
                            selectedCoords[idx] = new Vector2(x + 1, y - 1);
                        }
                        else
                        {
                            selectedCoords[idx] = new Vector2(x, y - 1);
                        }
                        break;

                    case (int)HexDirections.BottomLeft:
                        if (oddRow)
                        {
                            selectedCoords[idx] = new Vector2(x, y - 1);
                        }
                        else
                        {
                            selectedCoords[idx] = new Vector2(x - 1, y - 1);
                        }
                        break;

                    case (int)HexDirections.Left:
                        selectedCoords[idx] = new Vector2(x - 1, y);
                        break;

                    case (int)HexDirections.TopLeft:
                        if (oddRow)
                        {
                            selectedCoords[idx] = new Vector2(x, y + 1);
                        }
                        else
                        {
                            selectedCoords[idx] = new Vector2(x - 1, y + 1);
                        }
                        break;
                }

                // Ensure selected coordinates are in bounds...
                if (selectedCoords[idx].X >= 0 && selectedCoords[idx].X < openList.GetLength(1))
                {
                    if (selectedCoords[idx].Y >= 0 && selectedCoords[idx].Y < openList.GetLength(0))
                    {
                        // ...have no impassables (unless the target is an impassable)...
                        if (((selectedCoords[idx].Y == end.Y && selectedCoords[idx].X == end.X) && hexMap[(int)end.Y, (int)end.X].hasShip) || !hexMap[(int)selectedCoords[idx].Y, (int)selectedCoords[idx].X].hasShip)
                        {
                            // ...and not in the closed list and then add to the open list.
                            if (!closedList[(int)selectedCoords[idx].Y, (int)selectedCoords[idx].X].active)
                            {
                                float G = parentNode.G + 1.0f;
                                float H = heuristicDerivative(hexMap[(int)selectedCoords[idx].Y, (int)selectedCoords[idx].X].GetCoordinates(), end);
                                float F = G + H;
                                ASNode newNode = new ASNode(selectedCoords[idx], parentNode.coordinates, F, G, H);

                                openList[(int)selectedCoords[idx].Y, (int)selectedCoords[idx].X] = newNode;
                            }
                        }
                    }
                }
            }

            // Drop this parent node from open list and transfer to closed list.
            closedList[(int)parentNode.coordinates.Y, (int)parentNode.coordinates.X] = openList[(int)parentNode.coordinates.Y, (int)parentNode.coordinates.X];
            openList[(int)parentNode.coordinates.Y, (int)parentNode.coordinates.X].active = false;

            // Find best cost path.
            for (int idx = 0; idx < (int)HexDirections.Total; idx++)
            {
                // Discard indices out of bounds.
                if (selectedCoords[idx].X < 0 || selectedCoords[idx].Y < 0) continue;
                if (selectedCoords[idx].X >= openList.GetLength(1) || selectedCoords[idx].Y >= openList.GetLength(0)) continue;


                if (openList[(int)selectedCoords[idx].Y, (int)selectedCoords[idx].X].active == true &&
                    openList[(int)selectedCoords[idx].Y, (int)selectedCoords[idx].X].F < lowestF)
                {
                    lowestF = openList[(int)selectedCoords[idx].Y, (int)selectedCoords[idx].X].F;
                    lowestNode = openList[(int)selectedCoords[idx].Y, (int)selectedCoords[idx].X];
                    lowestSelection = idx;
                }
            }

            if (lowestSelection != -1)
            {
                // Mark this best cost as a pathway.
                if (!hexMap[(int)selectedCoords[lowestSelection].Y, (int)selectedCoords[lowestSelection].X].hasShip)
                {
                    openList[(int)lowestNode.coordinates.Y, (int)lowestNode.coordinates.X].path = true;
                    pathway.Add(lowestNode.coordinates);
                }
            }
            else
            {
                // There is no path!
            }

            // Put all other nodes in the closed list.
            for (int idx = 0; idx < (int)HexDirections.Total; idx++)
            {
                // Discard indices out of bounds.
                if (selectedCoords[idx].X < 0 || selectedCoords[idx].Y < 0) continue;
                if (selectedCoords[idx].X >= openList.GetLength(1) || selectedCoords[idx].Y >= openList.GetLength(0)) continue;

                // Skip if we hit the open list winner.
                if (idx == lowestSelection) continue;
                openList[(int)selectedCoords[idx].Y, (int)selectedCoords[idx].X] = closedList[(int)selectedCoords[idx].Y, (int)selectedCoords[idx].X];
                openList[(int)selectedCoords[idx].Y, (int)selectedCoords[idx].X].active = false;
            }

            // If we haven't reached our destination and the next move isn't an impassable, iterate through again.
            if (lowestSelection != -1)
            {
                if (lowestNode.coordinates != end && !hexMap[(int)selectedCoords[lowestSelection].Y, (int)selectedCoords[lowestSelection].X].hasShip)
                {
                    AStarBitch(lowestNode, (int)lowestNode.coordinates.Y, (int)lowestNode.coordinates.X);
                }
            }

        }

        public float heuristicDerivative(Vector2 origin, Vector2 target)
        {
            float dx = (float)Math.Abs(target.X - origin.X);
            float dy = (float)Math.Abs(target.Y - origin.Y);
            return (float)Math.Sqrt((dx * dx) + (dy * dy));
        }

        public int GetTotalHops()
        {
            return pathway.Count;
        }

        public Vector3 StepTarget(int hops)
        {
            return hexMap[(int)pathway[hops].Y, (int)pathway[hops].X].GetMidpoint();
        }

    }
}
