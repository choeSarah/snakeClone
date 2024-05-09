using System;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Models;
using NetworkUtil;

namespace SnakeGame;

public class GameController
{
    private World theWorld = new World(2000);
    private SocketState theServer;
    public int OurPlayerID;

    public bool errorOccurred;

    public string moving;

    // A delegate and event to fire when the controller
    // has received and processed new info from the server
    public delegate void GameUpdateHandler();
    public event GameUpdateHandler UpdateArrived;

    public delegate void ErrorHandler();
    public event ErrorHandler ErrorArrived;


    public GameController()
    {
        errorOccurred = false;

        //theWorld = new World(2000);

        moving = "none";
    }


    public World GetWorld()
    {
        return theWorld;
    }

    public bool Connect(string serverText, string nameText)
    {
        string serverAddress = serverText;
        theWorld.setPlayerName(nameText);

        Networking.ConnectToServer(OnConnect, serverAddress, 11000);

        if (theServer.TheSocket.Connected == false) //if the socket is not connected
        {
            return false; //it was not connected
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// Method to be invoked by the networking library when a connection is made
    /// </summary>
    /// <param name="state"></param>
    private void OnConnect(SocketState state)
    {
        if (state.ErrorOccurred)
        {
            errorOccurred = true;
        }

        theServer = state;

        Networking.Send(theServer.TheSocket, theWorld.getPlayerName());

        // Start an event loop to receive messages from the server
        state.OnNetworkAction = ReceiveMessages;
        
        Networking.GetData(state);
    }

    private void ReceiveMessages(SocketState state)
    {
        if (state.ErrorOccurred)
        {
            errorOccurred = true;
            //ErrorArrived?.Invoke();

        }

        string totalData = state.GetData();

        if (totalData.Length == 0) //if nothing was received
        {
            totalData = state.GetData();
        }
        else //if something was received
        {
            lock (theWorld)
            {
                string[] parts = Regex.Split(totalData, @"(?<=[\n])");

                //getting the walls

                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].Length == 0)
                    {
                        continue;
                    }

                    if (parts[i][parts[i].Length-1] != '\n')
                    {
                        break;
                    }

                    if (i == 0 && int.TryParse(parts[i], out _) == true) 
                    {
                        theWorld.setPlayerID(Convert.ToInt32(parts[i]));

                    } else  if (i == 1 && int.TryParse(parts[i], out _) == true)
                    {
                        theWorld.Size = Convert.ToInt32(parts[i]);

                    } else
                    {
                        JsonDocument doc = JsonDocument.Parse(parts[i]);

                        if (doc.RootElement.TryGetProperty("wall", out _))
                        {
                            int wallID = doc.RootElement.GetProperty("wall").GetInt32();
                            Wall? wall = doc.Deserialize<Wall>();

                            theWorld.Walls[wallID] = wall!;

                        }

                        if (doc.RootElement.TryGetProperty("snake", out _))
                        {
                            int snakeID = doc.RootElement.GetProperty("snake").GetInt32();
                            Snake? snake = doc.Deserialize<Snake>();
                            theWorld.Snakes[snakeID] = snake!;
                        }

                        if (doc.RootElement.TryGetProperty("power", out _))
                        {
                            int powerID = doc.RootElement.GetProperty("power").GetInt32();
                            Powerup? powerup = doc.Deserialize<Powerup>();

                            theWorld.Powerups[powerID] = powerup!;

                        }

                    }

                    state.RemoveData(0, parts[i].Length);
                }

                foreach (Snake snake in theWorld.Snakes.Values)
                {
                    if (snake.died)
                    {
                        theWorld.Snakes.Remove(snake.snake);

                    }
                    else
                    {
                        int snakeID = snake.snake;
                        theWorld.Snakes[snakeID] = snake;
                    }
                }

                foreach (Powerup pow in theWorld.Powerups.Values)
                {
                    if (pow.died)
                    {
                        theWorld.Powerups.Remove(pow.power);
                    }
                    else
                    {
                        theWorld.Powerups[pow.power] = pow;
                    }
                }

            }
            //state.RemoveData(0, totalData.Length);
            // Notify any listeners (the view) that a new game world has arrived from the server
            UpdateArrived?.Invoke();
            //state.RemoveData(0, state.GetData().Length);
            state.OnNetworkAction = ReceiveMessages;
            Networking.GetData(state);


        }
    }

    //SNAKE MOVEMENTS

    public void MoveUp()
    {
        moving = "up";
        Networking.Send(theServer.TheSocket, "{\"moving\":\"up\"}\n");
        //moving = "{\"moving\":\"up\"}";
        //Networking.Send(theServer.TheSocket, "{\"moving\":\"up\"}");
    }

    public void MoveLeft()
    {
        moving = "left";
        Networking.Send(theServer.TheSocket, "{\"moving\":\"left\"}\n");
        //moving = "{\"moving\":\"left\"}\n";
        //Networking.Send(theServer.TheSocket, "{\"moving\":\"left\"}\n");

    }

    public void MoveDown()
    {
        moving = "down";
        Networking.Send(theServer.TheSocket, "{\"moving\":\"down\"}\n");
        //moving = "{\"moving\":\"down\"}\n";
        //Networking.Send(theServer.TheSocket, "{\"moving\":\"down\"}");

    }

    public void MoveRight()
    {
        moving = "right";
        Networking.Send(theServer.TheSocket, "{\"moving\":\"right\"}\n");
        //moving = "{\"moving\":\"down\"}\n";
        //Networking.Send(theServer.TheSocket, "{\"moving\":\"right\"}");
    }
}