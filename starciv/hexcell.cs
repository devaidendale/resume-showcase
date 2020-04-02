using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StarCiv
{
    public class HexCell
    {
        public enum HexTypes
        {
            Selection = 0,
            Mouseover = 1,
            Standard = 2,
            Owned = 3,

            Total = 4,
        }

        public int typeOfHex;
        public bool hasShip;
        public Vector2 coordinates;
        public List<VertexPositionColor> vertices;
        public int[] indices;
        public BoundingBox bBox;
        public float lenSide;
        public float h;
        public float r;
        public float x;
        public float y;
        public float z;
        public bool selectionHexColour;
        public Color hexColour;
        public float hexAlpha;
        public float alphaIndex;

        public HexCell(Vector3 pos, float a_lenSide, Vector2 coordinates)
        {
            hexAlpha = 0.33f;
            alphaIndex = 0.73f;     // Because the sin of 0.73 is equal to about double of the initial hexAlpha.
            
            // Effective hack of detecting the type of hex this is by judging the length of its side.
            switch((int)a_lenSide)
            {
                case 80:
                    typeOfHex = (int)HexTypes.Selection;
                    hexColour = new Color(Color.White, hexAlpha);
                    break;
                case 99:
                    typeOfHex = (int)HexTypes.Mouseover;
                    hexColour = new Color(Color.Red, hexAlpha);
                    break;
                case 100:
                    typeOfHex = (int)HexTypes.Standard;
                    hexColour = new Color(Color.Purple, hexAlpha);
                    break;
                case 101:
                    typeOfHex = (int)HexTypes.Owned;
                    hexColour = new Color(Color.Blue, 0.0f);
                    break;
                
                default:
                    typeOfHex = (int)HexTypes.Standard;
                    break;
            }

            AssignHexCell(pos, a_lenSide, coordinates);
            
        }

        public void AssignHexCell(Vector3 pos, float a_lenSide, Vector2 coordinates)
        {
            this.hasShip = false;
            this.coordinates = coordinates;
            this.x = pos.X;
            this.y = pos.Y;
            this.z = pos.Z;
            this.lenSide = a_lenSide;
            this.h = Global.Instance.CalculateH(this.lenSide);
            this.r = Global.Instance.CalculateR(this.lenSide);
            this.vertices = new List<VertexPositionColor>();

            // Bounding box is for selection.
            if(typeOfHex == (int)HexTypes.Standard)
                bBox = new BoundingBox(GetMinimum(), GetMaximum());

            /* LineList vertices works in pairs; to get around this and not have retarded borders there have to be odd line vertex pairs too.
             *
             *        1
             *       .o. 
             *     .     . 
             *  6o         o2
             *   .         .
             *   .         .
             *  5o         o3
             *     .     . 
             *       .o.
             *        4
             */

            if (typeOfHex == (int)HexTypes.Standard)
            {
                // 1-2:
                vertices.Add(new VertexPositionColor(new Vector3(x, y, z), hexColour));
                vertices.Add(new VertexPositionColor(new Vector3(x + r, y + h, z), hexColour));

                // 3-4:
                vertices.Add(new VertexPositionColor(new Vector3(x + r, y + lenSide + h, z), hexColour));
                vertices.Add(new VertexPositionColor(new Vector3(x, y + lenSide + h + h, z), hexColour));

                // 5-6:
                vertices.Add(new VertexPositionColor(new Vector3(x - r, y + lenSide + h, z), hexColour));
                vertices.Add(new VertexPositionColor(new Vector3(x - r, y + h, z), hexColour));

                // 2-3:
                vertices.Add(new VertexPositionColor(new Vector3(x + r, y + h, z), hexColour));
                vertices.Add(new VertexPositionColor(new Vector3(x + r, y + lenSide + h, z), hexColour));

                // 4-5:
                vertices.Add(new VertexPositionColor(new Vector3(x, y + lenSide + h + h, z), hexColour));
                vertices.Add(new VertexPositionColor(new Vector3(x - r, y + lenSide + h, z), hexColour));

                // 6-1:
                vertices.Add(new VertexPositionColor(new Vector3(x - r, y + h, z), hexColour));
                vertices.Add(new VertexPositionColor(new Vector3(x, y, z), hexColour));
            }

            // Other types use triangle strips for filled colour, so they have to have indices defined.
            else
            {
                // To prevent overlay glitching, small changes to the z are made.
                float zFix = z;
                switch (typeOfHex)
                {
                    case (int)HexTypes.Mouseover:
                        zFix += 2.0f;
                        break;
                    case (int)HexTypes.Selection:
                        zFix += 1.0f;
                        break;
                }

                // The six vertices of the hexagon.
                vertices.Add(new VertexPositionColor(new Vector3(x, y, zFix), hexColour));
                vertices.Add(new VertexPositionColor(new Vector3(x + r, y + h, zFix), hexColour));
                vertices.Add(new VertexPositionColor(new Vector3(x + r, y + lenSide + h, zFix), hexColour));
                vertices.Add(new VertexPositionColor(new Vector3(x, y + lenSide + h + h, zFix), hexColour));
                vertices.Add(new VertexPositionColor(new Vector3(x - r, y + lenSide + h, zFix), hexColour));
                vertices.Add(new VertexPositionColor(new Vector3(x - r, y + h, zFix), hexColour));

                // Each index assigned with no overlap.
                indices = new int[12];
                indices[0] = 5;
                indices[1] = 0;
                indices[2] = 1;
                indices[3] = 1;
                indices[4] = 2;
                indices[5] = 4;
                indices[6] = 2;
                indices[7] = 3;
                indices[8] = 4;
                indices[9] = 5;
                indices[10] = 1;
                indices[11] = 4;
            }
        }

        public void Update(float dt, float workers)
        {
            // Called when updating colours for some hex types.

            // Pulsating colour logic for selection and hover hexes.
            if(typeOfHex == (int)HexTypes.Selection || typeOfHex == (int)HexTypes.Mouseover)
            {
                // Change the alpha by the time interval.
                alphaIndex += dt * 4;

                // Assign the amplitude of that part of the sine index to the alpha value.
                hexAlpha = (float)(Math.Sin(alphaIndex));

                // Negative amplitudes need to stay positive.
                if (hexAlpha < 0)
                    hexAlpha = -hexAlpha;

                // Up to a maximum of 33%.
                hexAlpha /= 3;
            }

            if (typeOfHex == (int)HexTypes.Selection)
            {
                hexColour = new Color(Color.White, hexAlpha);
            }

            if (typeOfHex == (int)HexTypes.Mouseover)
            {
                hexColour = new Color(Color.Red, hexAlpha);
            }

            if (typeOfHex == (int)HexTypes.Owned)
            {
                hexColour = new Color(Color.Blue, workers / 1000);
                if (hexColour.A >= 51)
                    hexColour.A = 51;
            }

            // Possible XNA 'feature': must re-assign all vertices otherwise this will not update.
            AssignHexCell(new Vector3(this.x, this.y, this.z), this.lenSide, this.coordinates);
        }

        public Vector2 GetCoordinates()
        {
            return coordinates;
        }
        
        public Vector3 GetMidpoint()
        {
            return new Vector3(x, y + (lenSide + (2 * h)) / 2, z);
        }

        public Vector3 GetMinimum()
        {
            return new Vector3(x - r, y, z);
        }

        public Vector3 GetMaximum()
        {
            return new Vector3(x + r, y + (lenSide + (2 * h)), z - 1);
        }

        public Vector3 GetPos()
        {
            return new Vector3(x, y, z);
        }

        public bool getHasShip()
        {
            return hasShip;
        }

        public void toggleHasShip(bool toggle)
        {
            hasShip = toggle;
        }

    }
}
