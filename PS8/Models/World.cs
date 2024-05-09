using System;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace Models
{
    [DataContract(Name = "GameSettings", Namespace = "")]
    public class World
    {
        private int playerID;
        private string playerName;
        private int MaxPowerups = 20;
        private int MaxPowerupDelay = 75;
        public Dictionary<int, Wall> Walls;
        public Dictionary<int, Snake> Snakes;
        public Dictionary<int, Powerup> Powerups;

        [DataMember(Name = "MSPerFrame")]
        public int MSPerFrame
        { get; set; }
        [DataMember(Name = "RespawnRate")]
        public int RespawnRate
        { get; set; }
        [DataMember(Name = "UniverseSize")]
        public int Size
        { get; set; }
        [DataMember(Name = "Walls")]
        public List<Wall> ListOfWalls;

        private int snakeLength = 120;

        public World()
        {
            Snakes = new Dictionary<int, Snake>();
            Powerups = new Dictionary<int, Powerup>();
            Walls = new Dictionary<int, Wall>();
            ListOfWalls = new List<Wall>();

        }

        public World(int _size)
        {
            Snakes = new Dictionary<int, Snake>();
            Powerups = new Dictionary<int, Powerup>();
            Walls = new Dictionary<int, Wall>();
            Size = _size;
        }

        public void setPlayerID(int id)
        {
            playerID = id;
        }

        public int getPlayerID()
        {
            return playerID;
        }

        public void setPlayerName(string name)
        {
            playerName = name;
        }

        public string getPlayerName()
        {
            return playerName;
        }

        public int getMaxPowerUps()
        {
            return MaxPowerups;
        }

        public int getMaxPowerupDelay()
        {
            return MaxPowerupDelay;
        }

        public int getSnakeLength()
        {
            return snakeLength; 
        }

        public void setSnakeLength(int s)
        {
            snakeLength = s;
        }
    }
}