using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using IImage = Microsoft.Maui.Graphics.IImage;
#if MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#else
using Microsoft.Maui.Graphics.Win2D;
#endif
using Color = Microsoft.Maui.Graphics.Color;
using System.Reflection;
using Microsoft.Maui;
using System.Net;
using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;
using Models;
using Microsoft.Maui.Graphics;

namespace SnakeGame;
public class WorldPanel : IDrawable
{
    // A delegate for DrawObjectWithTransform
    // Methods matching this delegate can draw whatever they want onto the canvas
    public delegate void ObjectDrawer(object o, ICanvas canvas);

    private int viewSize = 900;

    public World theWorld;

    private IImage wall;
    private IImage background;
    private IImage explosion;

    private bool initializedForDrawing = false;

    internal void SetWorld(World world)
    {
        wall = loadImage("wallsprite.png");
        background = loadImage("background.png");
        explosion = loadImage("explosion.png");
        theWorld = world;
    }

    public World GetWorld()
    {
        return theWorld;
    }

    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeClient.Resources.Images";
        using (Stream stream = assembly.GetManifestResourceStream($"{path}.{name}"))
        {
#if MACCATALYST
            return PlatformImage.FromStream(stream);
#else
            return new W2DImageLoadingService().FromStream(stream);
#endif
        }
    }

    public WorldPanel()
    {
    }

    /// <summary>
    /// This method performs a translation and rotation to draw an object.
    /// </summary>
    /// <param name="canvas">The canvas object for drawing onto</param>
    /// <param name="o">The object to draw</param>
    /// <param name="worldX">The X component of the object's position in world space</param>
    /// <param name="worldY">The Y component of the object's position in world space</param>
    /// <param name="angle">The orientation of the object, measured in degrees clockwise from "up"</param>
    /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
    private void DrawObjectWithTransform(ICanvas canvas, object o, double worldX, double worldY, double angle, ObjectDrawer drawer)
    {
        // "push" the current transform
        canvas.SaveState();

        canvas.Translate((float)worldX, (float)worldY);
        canvas.Rotate((float)angle);
        drawer(o, canvas);

        // "pop" the transform
        canvas.RestoreState();
    }

    private void SnakeSegmentDrawer(object o, ICanvas canvas)
    {
        float snakeSegmentLength = Convert.ToInt32(o);
        canvas.StrokeSize = 10;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawLine(0, 0, 0, (float)-snakeSegmentLength);

    }

    private void PowerDrawer(object o, ICanvas canvas)
    {
        Powerup p = o as Powerup;
        int width = 16;

        canvas.FillColor = Colors.Orange;
        canvas.FillEllipse(-(width / 2), -(width / 2), width, width);
    }

    private void TextDrawer(object o, ICanvas canvas)
    {
        canvas.FontColor = Colors.Blue;
        canvas.FontSize = 16;
        string displayMessage = theWorld.getPlayerName() + ": " +
            theWorld.Snakes[theWorld.getPlayerID()].score;
        canvas.DrawString(displayMessage, 0, 0, 50, 100, HorizontalAlignment.Left,
            VerticalAlignment.Center, TextFlow.OverflowBounds, lineSpacingAdjustment: 0);
    }

    private void ExplosionDrawer(object o, ICanvas canvas)
    {
        int width = 200;
        canvas.DrawImage(explosion, 0 - width/2, 0 - width/2, width, width);
    }

    private void WallDrawer(object o, ICanvas canvas)
    {
        Wall w = o as Wall;
        int width = 50;

        //check if the wall is horizontal or vertical
        if (w.p1.GetX()==w.p2.GetX()) //vertical wall
        {
            if (w.p1.GetY() < w.p2.GetY()) //p1 is on top
            {
                int startX = (int)w.p1.GetX() - 25;
                int startY = (int)w.p1.GetY() - 25;

                int endY = (int)w.p2.GetY() - 25;

                while (startY <= endY)
                {
                    canvas.DrawImage(wall, startX, startY, width, width);

                    startY = startY + 50;
                }
            } else //p2 is on top
            {
                int startX = (int)w.p2.GetX() - 25;
                int startY = (int)w.p2.GetY() - 25;

                int endY = (int)w.p1.GetY() - 25;

                while (startY <= endY)
                {
                    canvas.DrawImage(wall, startX, startY, width, width);

                    startY = startY + 50;
                }
            }


        } else //horizontal wall
        {

            if (w.p1.GetX() < w.p2.GetX()) //p1 is on the left
            {
                int startX = (int)w.p1.GetX() - 25;
                int startY = (int)w.p1.GetY() - 25;

                int endX = (int)w.p2.GetX() - 25;

                while (startX <= endX)
                {
                    canvas.DrawImage(wall, startX, startY, width, width);

                    startX = startX + 50;
                }
            } else //p1 is on the right
            {
                int startX = (int)w.p2.GetX() - 25;
                int startY = (int)w.p2.GetY() - 25;

                int endX = (int)w.p1.GetX() - 25;

                while (startX <= endX)
                {
                    canvas.DrawImage(wall, startX, startY, width, width);

                    startX = startX + 50;
                }
            }
        }

    }

    private void InitializeDrawing()
    {
        wall = loadImage("wallsprite.png");
        background = loadImage("background.png");
        initializedForDrawing = true;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (!initializedForDrawing)
            InitializeDrawing();

        // undo previous transformations from last frame
        canvas.ResetState();

        if (theWorld.Snakes.Count != 0)
        {
            int ourSnakeID = theWorld.getPlayerID();
            Snake ourSnake = theWorld.Snakes[ourSnakeID];

            //Translation so that the camera always follows the player
            float playerX = (float)ourSnake.body[ourSnake.body.Count - 1].GetX();
            float playerY = (float)ourSnake.body[ourSnake.body.Count - 1].GetY();

            canvas.Translate(-playerX + (viewSize / 2), -playerY + (viewSize / 2));
        }

        //drawing the objects in the world
        canvas.DrawImage(background, -(float)(theWorld.Size / 2), -(float)(theWorld.Size/ 2), 2000, 2000);

        lock (theWorld)
        {
            //drawing the powerups
            foreach (var p in theWorld.Powerups.Values)
            {
                DrawObjectWithTransform(canvas, p,
                  p.loc.GetX(), p.loc.GetY(), 0,
                  PowerDrawer);
            }

            //drawing the walls
            foreach (var w in theWorld.Walls.Values)
            {
                //check if the wall is horizontal or vertical
                if (w.p1.GetX() == w.p2.GetX()) //vertical wall
                {
                    if (w.p1.GetY() < w.p2.GetY()) //p1 is on top
                    {
                        DrawObjectWithTransform(canvas, w,
                          0, 0, 0,
                          WallDrawer);
                    }
                    else //p2 is on top
                    {
                        DrawObjectWithTransform(canvas, w,
                          0, 0, 0,
                          WallDrawer);
                    }


                }
                else //horizontal wall
                {

                    if (w.p1.GetX() < w.p2.GetX()) //p1 is on the left
                    {
                        DrawObjectWithTransform(canvas, w,
                             0, 0, 0,
                             WallDrawer);
                    }
                    else //p1 is on the right
                    {
                        DrawObjectWithTransform(canvas, w,
                          0, 0, 0,
                          WallDrawer);
                    }
                }

            }

            //drawing the snakes

            foreach (Snake s in theWorld.Snakes.Values)
            {
                if (s.alive)
                {
                    for (int i = 0; i < s.body.Count() - 1; i++)
                    {
                        Vector2D vector = s.body[i];
                        Vector2D nextVector = s.body[i + 1];
                        double segmentLength = (nextVector - vector).Length();
                        double segmentX = vector.GetX();
                        double segmentY = vector.GetY();
                        double snakeDirection = s.dir.ToAngle();
                        double segmentDirection = Vector2D.AngleBetweenPoints(nextVector, vector);

                        Color[] colors = {Colors.Red, Colors.Orange, Colors.Yellow, Colors.Green, Colors.Blue,
                          Colors.Purple, Colors.Pink, Colors.Silver, Colors.Teal, Colors.Violet};

                        int numColors = colors.Length;
                        int colorIndex = (s.snake) % numColors;

                        canvas.StrokeColor = colors[colorIndex];

                        DrawObjectWithTransform(canvas, segmentLength, segmentX, segmentY, segmentDirection, SnakeSegmentDrawer);
                    }
                    DrawObjectWithTransform(canvas, s, s.body[s.body.Count-1].GetX(), s.body[s.body.Count - 1].GetY(), 0, TextDrawer);

                }
                else //death animation
                {
                    Vector2D head = s.body[s.body.Count - 1];

                    DrawObjectWithTransform(canvas, s.died, head.GetX(), head.GetY(), 0, ExplosionDrawer);


                }


            }

        }
        
    }
}