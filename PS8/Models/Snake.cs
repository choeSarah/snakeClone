using System;
using SnakeGame;
namespace Models
{
	public class Snake
	{
		public int snake { get; private set; } //an int representing the snake's unique ID
		public string name { get; private set; } //a string representing the player's name
		public List<Vector2D> body { get; private set; } //a List<Vector2> representing the entire body of the snake
			//Each point in this list represents one vertex of the snake's body
			//First point of the list gives the location of the snake's tail and the last is the snake's head
		public Vector2D dir { get; set; } //represents snake's orientation
		public int score { get; set; } //representing the player's score
		public bool died { get; set; } //indicating if the snake died on this frame
		public bool alive { get; set; } //indicating if the snake is alive or dead
		public bool dc { get; set; } //indicates if the player controlling snake disconnected
		public bool join { get; set; } //indicates if the player joined

        public int length { get; set; }

        private int speed = 6;
        private int growth = 24;
        private int respawnCount = 0;


        public Snake (int snake, string name, List<Vector2D> body, Vector2D dir, int score, bool died, bool alive, bool dc, bool join)
		{
            this.snake = snake;
            this.name = name;
            this.body = body;
            this.dir = dir;
            this.score = score;
            this.died = died;
            this.alive = alive;
            this.dc = dc;
            this.join = join;
        }

        public int GetSpeed()
        {
            return speed;
        }

        public int GetGrowth()
        {
            return growth;
        }

        public int getRespawnCount()
        {
            return respawnCount;
        }

        public void setRespawnCount(int a)
        {
            respawnCount = a;
        }
    }
}

