using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NetworkUtil;
using Models;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Xml;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using SnakeGame;
using System.Reflection;
using System.Timers;
using System.Numerics;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using System.Drawing;

class Server
{
    // A map of clients that are connected, each with an ID
    private Dictionary<int, SocketState> clients;
    //The server's world
    private World theWorld;
    //Frame timer
    private static System.Timers.Timer aTimer;
    private bool handshakeHappened;

    private Stopwatch snakeWatch = new Stopwatch();
    private Stopwatch powerWatch = new Stopwatch();

    //snakes that are growing.
    private Dictionary<Snake, int> GrowingSnakes;

    public Server()
    {
        clients = new Dictionary<int, SocketState>();
        GrowingSnakes = new Dictionary<Snake, int>();
        theWorld = new World(2000);
        handshakeHappened = false;

        //Taking information from the Settings.xml and setting the world.
        DataContractSerializer ser = new(typeof(World));

        XmlReader reader = XmlReader.Create("settings.xml");

        lock (theWorld)
        {
            //Adding the walls to the World
            theWorld = (World)ser.ReadObject(reader)!;
            if (theWorld != null && theWorld.ListOfWalls != null)
            {
                int wallCount = 0;
                theWorld.Walls = new Dictionary<int, Wall>();

                foreach (Wall w in theWorld.ListOfWalls)
                {
                    theWorld.Walls[wallCount] = w;

                    wallCount++;
                }
            }

            //generating powerups
            theWorld!.Powerups = new Dictionary<int, Powerup>();
            for (int i = 0; theWorld.Powerups.Count < 20; i++)
            {
                theWorld.Powerups[i] = generatePowerups();
            }
        }

    }

    static void Main(string[] args)
    {
        Server server = new Server(); //starting server and accepting clients
        server.StartServer();

        Console.Read();
    }

    /// <summary>
    /// Method that starts the server
    /// </summary>
    private void StartServer()
    {
        Networking.StartServer(NewClientConnected, 11000);
        Console.WriteLine("Server is running. Accepting new clients");
        SetTimer();
    }

    /// <summary>
    /// Method that sets up the timer
    /// </summary>
    private void SetTimer()
    {
        aTimer = new System.Timers.Timer(theWorld.MSPerFrame);

        aTimer.Elapsed += OnTimedEvent;
        aTimer.AutoReset = true;
        aTimer.Enabled = true;

    }

    /// <summary>
    /// Method that runs for one frame and loops
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnTimedEvent(object? sender, ElapsedEventArgs e)
    {
        Console.WriteLine("FPS: " + 1000 / theWorld.MSPerFrame);
        //trying to send back to all the clients

        lock (clients)
        {
            StringBuilder message = new StringBuilder();

            if (theWorld.Powerups.Count != 0)
            {
                foreach (Powerup p in theWorld.Powerups.Values)
                {
                    message.Append(JsonSerializer.Serialize(p) + "\n");
                }
            }

            if (theWorld.Snakes != null && theWorld.Snakes.Count != 0)
            {
                foreach (Snake s in theWorld.Snakes.Values)
                {
                    if (!clients[s.snake].TheSocket.Connected)// does not add snakes that are disconnected.
                    {
                        s.alive = false;
                    }
                    message.Append(JsonSerializer.Serialize(s) + "\n");
                    if (s.died)
                    {
                        s.died = false;
                        GrowingSnakes.Remove(s);
                    }
                }
            }

            foreach (SocketState client in clients.Values)
            {
                Networking.Send(client.TheSocket, message.ToString());
            }
        }

        lock (theWorld)
        {
            GameLogic();

        }

    }

    /// <summary>
    /// Method that calculates collisions and snake position
    /// </summary>
    private void GameLogic()
    {
        List<int> deadPowers = new List<int>();

        if (handshakeHappened)
        {
            foreach (Snake snake in theWorld.Snakes.Values)
            {
                Vector2D snakeHead = snake.body.Last();
                if (WallCollision(snakeHead) || SnakeOwnCollision(snake) || SnakeBodyCollision(snake)) //if the snake collided with a wall
                {
                    snake.died = true;
                    snake.alive = false;
                    string name = theWorld.Snakes[snake.snake].name;
                    int id = snake.snake;

                    while (true)
                    {
                        snakeWatch.Start();

                        // Check the elapsed time in milliseconds
                        while (snakeWatch.ElapsedMilliseconds < theWorld.MSPerFrame) ;
                        snake.alive = false;

                        break;
                    }

                    snakeWatch.Reset();
                    theWorld.Snakes[id] = generateRandomSnake(name, id);

                }
                int powerID = PowerCollision(snakeHead);
                if (powerID != -1) //if the snake collided with a powerup
                {
                    snake.score = snake.score + 1;

                    theWorld.Powerups[powerID].died = true;
                    theWorld.Powerups[powerID] = generatePowerups(powerID);

                    GrowingSnakes[snake] = 0;
                }
                else //moving freely
                {
                    if (snake.dir.Equals(new Vector2D(1.0, 0.0))) //right
                    {

                        snake.body[snake.body.Count - 1].X += 6;
                        UpdateTail(snake);
                    }
                    else if (snake.dir.Equals(new Vector2D(0.0, 1.0))) //up
                    {
                        snake.body[snake.body.Count - 1].Y += 6;
                        UpdateTail(snake);

                    }
                    else if (snake.dir.Equals(new Vector2D(-1.0, 0.0))) //left
                    {
                        snake.body[snake.body.Count - 1].X -= 6;
                        UpdateTail(snake);

                    }
                    else if (snake.dir.Equals(new Vector2D(0.0, -1.0)))//down
                    {
                        snake.body[snake.body.Count - 1].Y -= 6;
                        UpdateTail(snake);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Method that updates where the snake tail should be in relation to its head
    /// while also checking for growth
    /// </summary>
    /// <param name="snake"></param>
    private void UpdateTail(Snake snake)
    {
        if (!GrowingSnakes.ContainsKey(snake))
        {
            GetSnakeTailDir(snake);
        }
        else
        {
            if (GrowingSnakes[snake] > 24)
            {
                GrowingSnakes.Remove(snake);
            }
            else
            {
                GrowingSnakes[snake] += 1;
            }
        }
    }

    /// <summary>
    /// Method that gets where the tail moves in the correct direction
    /// </summary>
    /// <param name="snake"></param>
    private void GetSnakeTailDir(Snake snake)
    {
        Vector2D vector1 = snake.body[0];
        Vector2D vector2 = snake.body[1];
        Vector2D vector = vector1 - vector2;

        if ((-6 < vector.GetX() && vector.GetX() < 6) && ((-6 < vector.GetY() && vector.GetY() < 6)))
        {
            if (snake.body.Count > 2)
            {
                snake.body.RemoveAt(0);
                vector2 = snake.body[1];
                vector1 = snake.body[0];
                vector = vector1 - vector2;
            }

        }

        if (vector.GetX() == 0)
        {
            if (vector1.GetY() < vector2.GetY())
            {
                snake.body[0].Y += 6;
            }
            else if (vector1.GetY() > vector2.GetY())
            {
                snake.body[0].Y -= 6;
            }
        }
        else if (vector.GetY() == 0)
        {
            if (vector1.GetX() < vector2.GetX())
            {
                snake.body[0].X += 6;
            }
            else if (vector1.GetX() > vector2.GetX())
            {
                snake.body[0].X -= 6;
            }
        }

    }

    /// <summary>
    /// Method that handles new clients connecting
    /// </summary>
    /// <param name="state"></param>
    private void NewClientConnected(SocketState state)
    {
        if (state.ErrorOccurred)
        {
            lock (clients)
            {
                clients.Remove((int)state.ID);
                return;
            }
        }

        IPEndPoint? localIpEndPoint = state.TheSocket.LocalEndPoint as IPEndPoint;

        string ipaddress = localIpEndPoint!.Address + ":" + localIpEndPoint.Port;


        if (ipaddress != null)
        {
            Console.WriteLine("Accepted new connection from " + ipaddress);

        }

        // change the state's network action to the 
        // receive handler so we can process data when something
        // happens on the network

        state.OnNetworkAction = Handshake;
        Networking.GetData(state);
    }

    /// <summary>
    /// Method that handles the handshake
    /// </summary>
    /// <param name="state"></param>
    private void Handshake(SocketState state)
    {
        if (state.ErrorOccurred)
        {
            clients[(int)state.ID].TheSocket.Disconnect(true);
            theWorld.Snakes[(int)state.ID].dc = true;
            theWorld.Snakes[(int)state.ID].join = false;
            theWorld.Snakes[(int)state.ID].alive = false;


            return;
        }

        string totalData = state.GetData();

        if (totalData.Length == 0) //if nothing was received
        {
            totalData = state.GetData();
        }
        else
        {
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            lock (theWorld)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].Length == 0)
                    {
                        continue;
                    }

                    if (parts[i][parts[i].Length - 1] != '\n')
                    {
                        break;
                    }

                    int snakeID = (int)state.ID;
                    string snakeName = parts[i].Trim();

                    if (theWorld.Snakes == null)
                    {
                        theWorld.Snakes = new Dictionary<int, Snake>();
                    }

                    theWorld.Snakes[snakeID] = generateRandomSnake(snakeName);


                    Console.WriteLine("Player(" + snakeID + ") " + snakeName + " has joined");

                }
            }

        }

        //trying to send back to the the clients
        lock (clients)
        {
            clients[(int)state.ID] = state;

            StringBuilder message = new StringBuilder();

            message.Append(state.ID + "\n");
            message.Append(theWorld.Size + "\n");


            if (theWorld.Walls.Count != 0)
            {
                for (int j = 0; j < theWorld.Walls.Count; j++)
                {
                    message.Append(JsonSerializer.Serialize(theWorld.Walls[j]) + "\n");
                }

            }

            if (theWorld.Powerups.Count != 0)
            {
                for (int j = 0; j < theWorld.Powerups.Count; j++)
                {

                    message.Append(JsonSerializer.Serialize(theWorld.Powerups[j]) + "\n");
                }

            }

            for (int i = 0; i < clients.Values.Count; i++)
            {
                if (clients.ContainsKey(i))
                {
                    Networking.Send(clients[i].TheSocket, message.ToString());

                }
            }
        }
        handshakeHappened = true;
        state.OnNetworkAction = ReceiveMessage;
        Networking.GetData(state);
    }

    /// <summary>
    /// Method that handles receiving from clients
    /// </summary>
    /// <param name="state"></param>
    private void ReceiveMessage(SocketState state)
    {

        if (state.ErrorOccurred)
        {
            lock (clients)
            {
                clients[(int)state.ID].TheSocket.Disconnect(true);
                theWorld.Snakes[(int)state.ID].dc = true;
                theWorld.Snakes[(int)state.ID].join = false;
                theWorld.Snakes[(int)state.ID].alive = false;

                return;

            }
        }

        string totalData = state.GetData();

        if (totalData.Length == 0) //if nothing was received
        {
            totalData = state.GetData();
        }
        else
        {
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length == 0)
                {
                    continue;
                }

                if (parts[i][parts[i].Length - 1] != '\n')
                {
                    break;
                }

                int clientID = (int)state.ID;
                Snake theSnake = theWorld.Snakes[clientID];

                //moving json docs
                JsonDocument doc = JsonDocument.Parse(parts[i]);
                string json = doc.RootElement.ToString();
                string previousJson = "";

                if (json != "{\"moving\":\"none\"}")
                {
                    if (json == "{\"moving\":\"left\"}" && previousJson != json) //moving left
                    {
                        if (!theSnake.dir.Equals(new Vector2D(1, 0))) //if it is not moving right
                        {
                            previousJson = json;
                            Vector2D previousHead = theSnake.body[theSnake.body.Count - 1];
                            theSnake.body.Add(new Vector2D(previousHead.GetX(), previousHead.GetY()));
                            theSnake.dir = new Vector2D(-1, 0);
                        }
                    }
                    else if (json == "{\"moving\":\"right\"}" && previousJson != json) //moving right
                    {
                        if (!theSnake.dir.Equals(new Vector2D(-1, 0))) //if it is not moving left
                        {
                            previousJson = json;
                            Vector2D previousHead = theSnake.body[theSnake.body.Count - 1];
                            theSnake.body.Add(new Vector2D(previousHead.GetX(), previousHead.GetY()));
                            theSnake.dir = new Vector2D(1, 0);
                        }
                    }
                    else if (json == "{\"moving\":\"up\"}" && previousJson != json) //moving up
                    {
                        if (!theSnake.dir.Equals(new Vector2D(0, 1))) //if it is not moving down
                        {
                            previousJson = json;
                            Vector2D previousHead = theSnake.body[theSnake.body.Count - 1];
                            theSnake.body.Add(new Vector2D(previousHead.GetX(), previousHead.GetY()));
                            theSnake.dir = new Vector2D(0, -1);

                        }
                    }
                    else if (json == "{\"moving\":\"down\"}" && previousJson != json) //moving down
                    {

                        if (!theSnake.dir.Equals(new Vector2D(0, -1))) //if it is not moving up
                        {
                            previousJson = json;
                            Vector2D previousHead = theSnake.body[theSnake.body.Count - 1];
                            theSnake.body.Add(new Vector2D(previousHead.GetX(), previousHead.GetY()));
                            theSnake.dir = new Vector2D(0, 1);

                        }
                    }
                }
                state.RemoveData(0, parts[i].Length);
            }
        }

        // Continue the event loop that receives messages from this client
        try
        {
            Networking.GetData(state);
        }
        catch (Exception) { }

        state.OnNetworkAction = ReceiveMessage;

    }

    /// <summary>
    /// Method that, given a vector, checks if that vector is hitting any walls
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private bool WallCollision(Vector2D obj)
    {
        double _x = obj.GetX();
        double _y = obj.GetY();
        bool collision = false;

        foreach (Wall w in theWorld.Walls.Values)
        {
            Vector2D minRange;
            Vector2D maxRange;

            //checking if the wall is a horizontal wall or vertical wall
            if (w.p1.GetX() - w.p2.GetX() == 0) //if vertical
            {
                if (w.p1.GetY() > w.p2.GetY()) //p1 is on the bottom
                {
                    minRange = w.p2;
                    maxRange = w.p1;
                }
                else //p1 is on the top
                {
                    minRange = w.p1;
                    maxRange = w.p2;
                }
            }
            else //if horizontal
            {
                if (w.p1.GetX() > w.p2.GetX()) //p1 is on the right
                {
                    minRange = w.p2;
                    maxRange = w.p1;
                }
                else //p1 is on the top
                {
                    minRange = w.p1;
                    maxRange = w.p2;
                }
            }
            if ((_x - 5 >= minRange.GetX() - 30) && _x + 5 <= maxRange.GetX() + 30)
            {
                if ((_y - 5 >= minRange.GetY() - 30) && _y + 5 <= maxRange.GetY() + 30)
                {
                    return true;
                }

            }

        }

        return collision;
    }

    /// <summary>
    /// Method that, given a vector, checks if that vector is hitting any powerups
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private int PowerCollision(Vector2D obj)
    {
        lock (theWorld)
        {
            foreach (Powerup pow in theWorld.Powerups.Values)
            {
                double pow_x = pow.loc.GetX();
                double pow_y = pow.loc.GetY();

                if (pow_x - 6 <= obj.GetX() && obj.GetX() <= pow_x + 6 && pow_y - 6 <= obj.GetY() && obj.GetY() <= pow_y + 6)
                {
                    return pow.power;
                }
            }

            return -1;
        }
    }

    /// <summary>
    /// Method that, given a snake, checks if that snake is hitting any other snakes
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private bool SnakeBodyCollision(Snake snake)
    {
        Vector2D head = snake.body.Last();

        foreach (Snake s in theWorld.Snakes.Values)
        {
            if (s.Equals(snake))
            {
                continue;
            }

            for (int i = 0; i < s.body.Count - 1; i++)
            {
                Vector2D minRange = new Vector2D();
                Vector2D maxRange = new Vector2D();

                if (s.body[i].GetX() == s.body[i + 1].GetX()) //vertical segment
                {
                    if (s.body[i].GetY() < s.body[i + 1].GetY()) //tail is at the top
                    {
                        minRange = s.body[i];
                        maxRange = s.body[i + 1];
                    }
                    else //tail is at the bottom
                    {
                        minRange = s.body[i + 1];
                        maxRange = s.body[i];
                    }
                }
                else //horizontal segment
                {
                    if (s.body[i].GetX() < s.body[i + 1].GetX()) //tail is on the left
                    {
                        minRange = s.body[i];
                        maxRange = s.body[i + 1];
                    }
                    else //tail is on the right
                    {
                        minRange = s.body[i + 1];
                        maxRange = s.body[i];
                    }
                }

                if (minRange.GetX() - maxRange.GetX() == 0) //vertical
                {
                    if (minRange.GetX() - 6 < head.GetX() && head.GetX() < maxRange.GetX() + 6)
                    {
                        if (minRange.GetY() < head.GetY() && head.GetY() < maxRange.GetY())
                        {
                            return true;
                        }
                    }
                }
                else //horizontal
                {
                    if (minRange.GetY() - 6 < head.GetY() && head.GetY() < maxRange.GetY() + 6)
                    {
                        if (minRange.GetX() < head.GetX() && head.GetX() < maxRange.GetX())
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Method that, given a snake, checks if that snake is hitting itself
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private bool SnakeOwnCollision(Snake snake)
    {
        Vector2D head = snake.body.Last();

        for (int i = 0; i < snake.body.Count - 2; i++)
        {
            Vector2D minRange = new Vector2D();
            Vector2D maxRange = new Vector2D();

            if (snake.body[i].GetX() == snake.body[i + 1].GetX()) //vertical segment
            {
                if (snake.body[i].GetY() < snake.body[i + 1].GetY()) //tail is at the top
                {
                    minRange = snake.body[i];
                    maxRange = snake.body[i + 1];
                }
                else //tail is at the bottom
                {
                    minRange = snake.body[i + 1];
                    maxRange = snake.body[i];
                }
            }
            else //horizontal segment
            {
                if (snake.body[i].GetX() < snake.body[i + 1].GetX()) //tail is on the left
                {
                    minRange = snake.body[i];
                    maxRange = snake.body[i + 1];
                }
                else //tail is on the right
                {
                    minRange = snake.body[i + 1];
                    maxRange = snake.body[i];
                }
            }

            if (minRange.GetX() - maxRange.GetX() == 0) //vertical
            {
                if (minRange.GetY() < head.GetY() && head.GetY() < maxRange.GetY() && head.GetX() == minRange.GetX())
                {
                    return true;
                }
            }
            else //horizontal
            {
                if (minRange.GetX() < head.GetX() && head.GetX() < maxRange.GetX() && head.GetY() == minRange.GetY())
                {
                    return true;
                }
            }
        }

        return false;

    }


    /// <summary>
    /// Method that generates powerups
    /// </summary>
    /// <returns></returns>
    private Powerup generatePowerups()
    {
        Random random = new Random();
        //getting random coordinates for snake body

        double _x = random.NextInt64(-1000, 1000);
        double _y = random.NextInt64(-1000, 1000);

        Vector2D powLoc = new Vector2D(_x, _y);

        if (WallCollision(powLoc) || PowerCollision(powLoc) != -1) //checking if the powerups are on top of walls
        {
            _x = random.NextInt64(-1000, 1000);
            _y = random.NextInt64(-1000, 1000);

            powLoc = new Vector2D(_x, _y);
        }

        int powerID = theWorld.Powerups.Count;
        bool died = false;

        return new Powerup(powerID, powLoc, died);
    }

    /// <summary>
    /// Method that generates powerups
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private Powerup generatePowerups(int id)
    {
        Random random = new Random();
        //getting random coordinates for snake body

        double _x = random.NextInt64(-1000, 1000);
        double _y = random.NextInt64(-1000, 1000);

        Vector2D powLoc = new Vector2D(_x, _y);

        if (WallCollision(powLoc)) //checking if the powerups are on top of walls
        {
            _x = random.NextInt64(-1000, 1000);
            _y = random.NextInt64(-1000, 1000);

            powLoc = new Vector2D(_x, _y);
        }

        int powerID = id;
        bool died = false;

        return new Powerup(powerID, powLoc, died);
    }

    /// <summary>
    /// Method that generates a new snake given a name and id
    /// </summary>
    /// <param name="name"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    private Snake generateRandomSnake(string name, int id)
    {
        int snakeID = id;
        string snakeName = name;
        int score = 0;
        bool died = false;
        bool alive = true;
        bool dc = false;
        bool join = true;

        //head
        Random random = new Random();
        random.NextInt64(-1000, 1000);
        double _x = random.NextInt64(-1000, 1000);
        double _y = random.NextInt64(-1000, 1000);

        Vector2D snakeHead = new Vector2D(_x, _y);

        while (WallCollision(snakeHead))
        {
            _x = random.NextInt64(-940, 940);
            _y = random.NextInt64(-940, 940);

            snakeHead = new Vector2D(_x, _y);
        }

        List<Vector2D> directionChoices = new List<Vector2D>();
        Vector2D dir = new Vector2D();
        Vector2D tail = new Vector2D();
        Vector2D temp1 = new Vector2D();
        Vector2D temp2 = new Vector2D();

        do
        {
            tail = new Vector2D();
            temp1 = new Vector2D();
            temp2 = new Vector2D();

            //dir
            directionChoices.Add(new Vector2D(0.0, -1.0));
            directionChoices.Add(new Vector2D(1.0, 0.0));
            directionChoices.Add(new Vector2D(0.0, 1.0));
            directionChoices.Add(new Vector2D(-1.0, 0.0));

            dir = directionChoices[random.Next(directionChoices.Count)];


            //body
            if (dir.Equals(new Vector2D(0, -1))) //if snake is pointed up
            {
                tail.X = snakeHead.X;
                tail.Y = snakeHead.Y + 120;

                temp1.X = snakeHead.X;
                temp1.Y = snakeHead.Y + 40;

                temp2.X = snakeHead.X;
                temp2.Y = snakeHead.Y + 80;
            }
            else if (dir.Equals(new Vector2D(1, 0))) //if snake is pointed right
            {
                tail.Y = snakeHead.Y;
                tail.X = snakeHead.X - 120;

                temp1.X = snakeHead.X - 40;
                temp1.Y = snakeHead.Y;

                temp2.X = snakeHead.X - 80;
                temp2.Y = snakeHead.Y;
            }
            else if (dir.Equals(new Vector2D(0, 1))) //if snake is pointed down
            {
                tail.X = snakeHead.X;
                tail.Y = snakeHead.Y - 120;

                temp1.X = snakeHead.X;
                temp1.Y = snakeHead.Y - 40;

                temp2.X = snakeHead.X;
                temp2.Y = snakeHead.Y - 80;
            }
            else if (dir.Equals(new Vector2D(-1, 0))) //if snake is pointed left
            {
                tail.Y = snakeHead.Y;
                tail.X = snakeHead.X + 120;


                temp1.X = snakeHead.X + 40;
                temp1.Y = snakeHead.Y;

                temp2.X = snakeHead.X + 80;
                temp2.Y = snakeHead.Y;
            }
        } while (WallCollision(temp1) || WallCollision(temp2) || WallCollision(tail));

        List<Vector2D> body = new List<Vector2D>();
        body.Add(tail);
        body.Add(snakeHead);

        Snake snake = new Snake(snakeID, snakeName, body, dir, score, died, alive, dc, join);
        return snake;
    }

    /// <summary>
    /// Method that creates a new snake given a name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private Snake generateRandomSnake(string name)
    {
        int id = theWorld.Snakes.Count;
        return generateRandomSnake(name, id);
    }
}